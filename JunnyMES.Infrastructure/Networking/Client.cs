using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using JunnyMES.Infrastructure.Configuration;
using JunnyMES.Core.Interfaces;
using JunnyMES.Core.Logging;
using JunnyMES.Infrastructure.Storage;

namespace NetworkManager
{
    public sealed class Client : IDisposable
    {
        private readonly TcpClient client;
        private readonly IAppLogger log;
        public string ConnectedLocalIPAddress { get; private set; }
        private readonly TransferOptions options;
        private readonly CancellationToken cancellationToken;
        private readonly CancellationTokenRegistration cancellation;
        public string LastError { get; private set; }

        public Client(string IPAddress, string PortNumber,
            int maxRetryAttempts, int retryInterval, IAppLogger logger = null, TransferOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            this.log = logger;
            this.cancellationToken = cancellationToken;
            this.options = options ?? new TransferOptions();
            DemoPaths.ValidateEndpoint(IPAddress, PortNumber);
            if (maxRetryAttempts < 1 || retryInterval < 0) throw new ArgumentOutOfRangeException(nameof(maxRetryAttempts));
            var budget = Stopwatch.StartNew();
            Exception last = null;
            for (int attempt = 0; attempt < maxRetryAttempts; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var candidate = new TcpClient();
                try
                {
                    int remaining = this.options.SessionTimeoutMs - (int)budget.ElapsedMilliseconds;
                    if (remaining <= 0) throw new TimeoutException("Connection budget exceeded.");
                    var pending = candidate.BeginConnect(IPAddress, int.Parse(PortNumber), null, null);
                    using (var wait = pending.AsyncWaitHandle)
                        {
                        int signaled = WaitHandle.WaitAny(new[] { wait, cancellationToken.WaitHandle }, Math.Min(this.options.ConnectTimeoutMs, remaining));
                        cancellationToken.ThrowIfCancellationRequested();
                        if (signaled == WaitHandle.WaitTimeout) throw new TimeoutException("Connection timeout.");
                    }
                    candidate.EndConnect(pending);
                    candidate.NoDelay = true;
                    client = candidate;
                    ConnectedLocalIPAddress = ((System.Net.IPEndPoint)candidate.Client.LocalEndPoint).Address.ToString();
                    cancellation = cancellationToken.Register(() => candidate.Close());
                    return;
                }
                catch (Exception ex)
                {
                    candidate.Close(); last = ex;
                    cancellationToken.ThrowIfCancellationRequested();
                    int remaining = this.options.SessionTimeoutMs - (int)budget.ElapsedMilliseconds;
                    if (remaining <= 0 || attempt + 1 == maxRetryAttempts) break;
                    if (cancellationToken.WaitHandle.WaitOne((int)Math.Min((long)retryInterval * 1000, remaining))) cancellationToken.ThrowIfCancellationRequested();
                }
            }
            throw new IOException("Unable to connect to the loopback server.", last);
        }

        private WireChannel Open()
        {
            cancellationToken.ThrowIfCancellationRequested();
            var channel = new WireChannel(client.GetStream(), options);
            channel.Expect(WireChannel.Greeting);
            return channel;
        }
        public bool BackupStart(List<string> filePaths, string pathBase = "")
        {
            try
            {
                var channel = Open();
                foreach (var entry in Expand(filePaths, pathBase))
                {
                    if (entry.Value)
                    {
                        channel.WriteLine("CREATE_DIRECTORY"); channel.WriteLine(entry.Key);
                        channel.Expect("DIRECTORY_COMPLETED");
                        continue;
                    }
                    string absolute = SafePathResolver.ResolveFile(pathBase, entry.Key);
                    using (var input = new FileStream(absolute, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        if (input.Length > TransferOptions.MaxFileBytes) throw new InvalidDataException("File exceeds 100 MiB.");
                        channel.WriteLine("PUT_FILE"); channel.WriteLine(entry.Key); channel.WriteLength(input.Length);
                        channel.Expect("READY");
                        channel.SendBody(input, input.Length);
                        string response = channel.ReadLine();
                        if (response != "TRANSFER_COMPLETED" && response != "UNCHANGED")
                            throw new InvalidDataException("File not committed: " + response);
                    }
                }
                channel.WriteLine("END_CONNECTION"); channel.Expect("CLOSE_PERMITTED");
                return true;
            }
            catch (Exception ex) { RecordFailure(ex); return false; }
            finally { Dispose(); }
        }
        public bool BackupDelete(List<string> filePaths, string pathBase = "")
        {
            try
            {
                if (filePaths == null || filePaths.Count > TransferOptions.MaxEntries) throw new InvalidDataException("Invalid delete list.");
                var channel = Open();
                foreach (string file in filePaths.Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    // Relative path calculation does not require the deleted source or its parent to exist.
                    string relative = SafePathResolver.RelativeFile(pathBase, file);
                    channel.WriteLine("DELETE_REQUEST"); channel.WriteLine(relative);
                    string response = channel.ReadLine();
                    if (response != "DELETE_COMPLETED" && response != "ALREADY_APPLIED")
                        throw new InvalidDataException("Delete not committed: " + response);
                }
                channel.WriteLine("END_CONNECTION"); channel.Expect("CLOSE_PERMITTED");
                return true;
            }
            catch (Exception ex) { RecordFailure(ex); return false; }
            finally { Dispose(); }
        }
        public bool BackupChanges(IReadOnlyDictionary<string, Stream> uploads, IReadOnlyCollection<string> deletions)
        {
            try
            {
                if (uploads == null || deletions == null || uploads.Count + deletions.Count > TransferOptions.MaxEntries)
                    throw new InvalidDataException("Invalid change set.");
                var channel = Open();
                foreach (var file in uploads)
                {
                    var input = file.Value;
                    if (input.Position != 0 || input.Length > TransferOptions.MaxFileBytes) throw new InvalidDataException("Invalid upload stream.");
                    channel.WriteLine("PUT_FILE"); channel.WriteLine(file.Key); channel.WriteLength(input.Length);
                    channel.Expect("READY"); channel.SendBody(input, input.Length);
                    string response = channel.ReadLine();
                    if (response != "TRANSFER_COMPLETED" && response != "UNCHANGED") throw new IOException("Upload failed: " + response);
                }
                foreach (string file in deletions)
                {
                    channel.WriteLine("DELETE_REQUEST"); channel.WriteLine(file);
                    string response = channel.ReadLine();
                    if (response != "DELETE_COMPLETED" && response != "ALREADY_APPLIED") throw new IOException("Delete failed: " + response);
                }
                channel.WriteLine("END_CONNECTION"); channel.Expect("CLOSE_PERMITTED");
                return true;
            }
            catch (Exception ex) { RecordFailure(ex); return false; }
            finally { Dispose(); }
        }
        public string GetPresetString()
        {
            try
            {
                var channel = Open(); channel.WriteLine("PRESET_REQUEST"); channel.Expect("PRESET_BYTES");
                long length = channel.ReadLength(TransferOptions.MaxPresetBytes);
                using (var output = new MemoryStream())
                {
                    channel.CopyBodyTo(output, length);
                    return new UTF8Encoding(false, true).GetString(output.ToArray());
                }
            }
            finally { Dispose(); }
        }
        private static Dictionary<string, bool> Expand(List<string> paths, string root)
        {
            if (paths == null || paths.Count > TransferOptions.MaxEntries || !Directory.Exists(root))
                throw new InvalidDataException("Invalid source list/root.");
            var entries = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            var pending = new Stack<string>(paths);
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string rootFull = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar);
            // Validate the root's existing ancestors even when the caller selects the root itself.
            SafePathResolver.ResolveFile(root, "validation-probe");
            while (pending.Count > 0)
            {
                string path = pending.Pop();
                if (string.IsNullOrWhiteSpace(path) || !Path.IsPathRooted(path)) throw new InvalidDataException("Invalid source path.");
                string full = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar);
                if (!visited.Add(full)) continue;
                if (visited.Count > TransferOptions.MaxEntries) throw new InvalidDataException("Too many entries.");
                bool isRoot = full.Equals(rootFull, StringComparison.OrdinalIgnoreCase);
                string relative = isRoot ? null : SafePathResolver.RelativeFile(root, full);
                if (Directory.Exists(full))
                {
                    if (!isRoot) entries[relative] = true;
                    foreach (string child in Directory.GetFileSystemEntries(full).OrderByDescending(x => x, StringComparer.OrdinalIgnoreCase))
                        pending.Push(child);
                }
                else if (File.Exists(full) && !isRoot) entries[relative] = false;
                else throw new FileNotFoundException("A selected source no longer exists.", full);
            }
            return entries;
        }
        private void RecordFailure(Exception ex)
        {
            LastError = ex.Message;
            try { log?.Log(LogLevel.Error, "NetworkClient", "Transfer failed: " + ex.Message); }
            catch (Exception) { /* A logging failure must not change a failed transfer into success. */ }
        }
        public void Dispose() { cancellation.Dispose(); client?.Close(); }
    }
}
