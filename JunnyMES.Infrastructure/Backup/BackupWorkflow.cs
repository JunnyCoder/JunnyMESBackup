using System.Threading;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using JunnyMES.Infrastructure.Configuration;
using JunnyMES.Infrastructure.Storage;
using NetworkManager;

namespace JunnyMES.Infrastructure.Backup
{
    // A captured policy cannot change under an in-flight plan.
    public sealed class BackupPolicy
    {
        public string Root { get; private set; }
        public string Identity { get; private set; }
        public int Depth { get; private set; }
        private readonly string[] selected, folders, files;
        public BackupPolicy(string root, string destination, int depth, IEnumerable<string> selected,
            IEnumerable<string> excludedFolders, IEnumerable<string> excludedFiles)
        {
            Root = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar);
            Identity = Root.ToUpperInvariant() + "|" + destination;
            if (depth < 1 || depth > 32) throw new ArgumentOutOfRangeException(nameof(depth));
            Depth = depth;
            this.selected = selected.Select(x => x.Replace('/', '\\')).ToArray(); folders = excludedFolders.ToArray(); files = excludedFiles.ToArray();
            foreach (var path in this.selected.Where(x => x != ".")) SafePathResolver.ResolveFile(Root, path);
        }
        public static string ProgramName(string relative)
        {
            var parts = relative.Replace('/', '\\').Split('\\');
            return parts.Length == 1 ? "." : parts[0];
        }
        internal bool ExcludedFolder(string name) { return Matches(name, folders); }
        internal bool Visits(string relative)
        {
            relative = relative.Replace('/', '\\');
            return selected.Any(x => x != "." && (relative.Equals(x, StringComparison.OrdinalIgnoreCase) ||
                relative.StartsWith(x + "\\", StringComparison.OrdinalIgnoreCase) || x.StartsWith(relative + "\\", StringComparison.OrdinalIgnoreCase)));
        }
        internal bool Includes(string relative)
        {
            relative = relative.Replace('/', '\\');
            var parts = relative.Replace('/', '\\').Split('\\');
            if (parts.Length > Depth || Matches(parts.Last(), files)) return false;
            if (parts.Take(parts.Length - 1).Any(ExcludedFolder)) return false;
            return selected.Any(x => x == "." ? parts.Length == 1 :
                relative.StartsWith(x.TrimEnd('\\', '/') + "\\", StringComparison.OrdinalIgnoreCase));
        }
        private static bool Matches(string name, IEnumerable<string> patterns)
        {
            foreach (string pattern in patterns)
            {
                string term = pattern.Trim('%');
                if (term.Length == 0) continue;
                if (pattern.StartsWith("%") && !pattern.EndsWith("%"))
                { if (name.EndsWith(term, StringComparison.OrdinalIgnoreCase)) return true; }
                else if (pattern.EndsWith("%") && !pattern.StartsWith("%"))
                { if (name.StartsWith(term, StringComparison.OrdinalIgnoreCase)) return true; }
                else if (name.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            }
            return false;
        }
    }

    public interface IFileSnapshotScanner { Dictionary<string, string> Scan(BackupPolicy policy, CancellationToken token = default(CancellationToken)); }
    public sealed class FileSnapshotScanner : IFileSnapshotScanner
    {
        public Dictionary<string, string> Scan(BackupPolicy policy, CancellationToken token = default(CancellationToken))
        {
            if (!Directory.Exists(policy.Root)) throw new DirectoryNotFoundException("Source root is unavailable.");
            SafePathResolver.ResolveFile(policy.Root, "validation-probe");
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            token.ThrowIfCancellationRequested();
            Visit(policy, policy.Root, 1, result, token);
            return result;
        }
        private static void Visit(BackupPolicy policy, string directory, int level, Dictionary<string, string> result, CancellationToken token)
        {
            // Enumeration/read failures abort the complete scan, never becoming inferred deletions.
            foreach (string path in Directory.GetFiles(directory).OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                token.ThrowIfCancellationRequested();
                string relative = SafePathResolver.RelativeFile(policy.Root, path);
                if (!policy.Includes(relative)) continue;
                using (var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                    result.Add(relative, Digest(input, token));
                if (result.Count > TransferOptions.MaxEntries) throw new InvalidDataException("Too many selected files.");
            }
            if (level >= policy.Depth) return;
            foreach (string child in Directory.GetDirectories(directory))
            {
                token.ThrowIfCancellationRequested();
                if (policy.ExcludedFolder(Path.GetFileName(child))) continue;
                string relative = child.Substring(policy.Root.Length + 1);
                if (!policy.Visits(relative)) continue;
                SafePathResolver.RelativeFile(policy.Root, child);
                Visit(policy, child, level + 1, result, token);
            }
        }
        internal static string Digest(Stream input, CancellationToken token = default(CancellationToken))
        {
            if (input.Length > TransferOptions.MaxFileBytes) throw new InvalidDataException("File exceeds 100 MiB.");
            using (var sha = SHA256.Create())
            {
                byte[] buffer = new byte[TransferOptions.BufferBytes];
                int read;
                do
                {
                    token.ThrowIfCancellationRequested();
                    read = input.Read(buffer, 0, buffer.Length);
                    if (read > 0) sha.TransformBlock(buffer, 0, read, buffer, 0);
                } while (read > 0);
                sha.TransformFinalBlock(buffer, 0, 0);
                return BitConverter.ToString(sha.Hash).Replace("-", "");
            }
        }
    }

    [DataContract]
    public sealed class SuccessfulSnapshot
    {
        [DataMember] public int Version = 1;
        [DataMember] public string Identity;
        [DataMember] public string Revision;
        [DataMember] public string CompletedUtc;
        [DataMember] public Dictionary<string, string> Files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }
    public interface ISuccessSnapshotStore
    {
        IDisposable AcquireWriteLease();
        SuccessfulSnapshot Load();
        void Save(SuccessfulSnapshot snapshot);
    }
    public sealed class JsonSuccessSnapshotStore : ISuccessSnapshotStore
    {
        private readonly string path;
        private readonly IJsonSerializer json = new DataContractJsonCodec();
        public JsonSuccessSnapshotStore(string stateDirectory) { path = Path.Combine(stateDirectory, "backup-success-v1.json"); }
        public IDisposable AcquireWriteLease()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            return new FileStream(path + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        }
        public SuccessfulSnapshot Load()
        {
            if (!File.Exists(path)) return null;
            if (new FileInfo(path).Length > 4 * 1024 * 1024) throw new InvalidDataException("Snapshot too large.");
            var value = json.Deserialize<SuccessfulSnapshot>(File.ReadAllText(path));
            DateTime timestamp;
            if (value == null || value.Version != 1 || string.IsNullOrEmpty(value.Identity) ||
                string.IsNullOrEmpty(value.Revision) || !DateTime.TryParse(value.CompletedUtc, out timestamp) ||
                value.Files == null || value.Files.Count > TransferOptions.MaxEntries ||
                value.Files.Any(x => string.IsNullOrEmpty(x.Key) || x.Value == null || x.Value.Length != 64 || x.Value.Any(c => !Uri.IsHexDigit(c))))
                throw new InvalidDataException("Invalid successful snapshot; restore a known good state.");
            value.Files = new Dictionary<string, string>(value.Files, StringComparer.OrdinalIgnoreCase);
            return value;
        }
        public void Save(SuccessfulSnapshot snapshot)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    byte[] body = System.Text.Encoding.UTF8.GetBytes(json.Serialize(snapshot));
                    if (body.Length > 4 * 1024 * 1024) throw new InvalidDataException("Snapshot too large.");
                    stream.Write(body, 0, body.Length); stream.Flush(true);
                }
                if (File.Exists(path)) File.Replace(temp, path, null); else File.Move(temp, path);
            }
            finally { if (File.Exists(temp)) File.Delete(temp); }
        }
    }
    public sealed class ChangeSet
    {
        internal readonly BackupPolicy Policy;
        internal readonly Dictionary<string, string> Current;
        internal readonly string Revision;
        public ReadOnlyCollection<string> Added { get; private set; }
        public ReadOnlyCollection<string> Modified { get; private set; }
        public ReadOnlyCollection<string> Deleted { get; private set; }
        public ReadOnlyCollection<string> Programs { get; private set; }
        public bool IsInitial { get; private set; }
        public bool HasChanges { get { return Added.Count + Modified.Count + Deleted.Count > 0; } }
        internal ChangeSet(BackupPolicy policy, Dictionary<string, string> current, SuccessfulSnapshot previous)
        {
            Policy = policy; Current = new Dictionary<string, string>(current, StringComparer.OrdinalIgnoreCase);
            Revision = previous == null ? null : previous.Revision;
            IsInitial = previous == null || previous.Identity != policy.Identity;
            var old = IsInitial ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) : previous.Files;
            foreach (string path in old.Keys) SafePathResolver.ResolveFile(policy.Root, path);
            Added = Current.Keys.Where(x => !old.ContainsKey(x)).ToList().AsReadOnly();
            Modified = Current.Keys.Where(x => old.ContainsKey(x) && old[x] != Current[x]).ToList().AsReadOnly();
            Deleted = old.Keys.Where(x => policy.Includes(x) && !Current.ContainsKey(x)).ToList().AsReadOnly();
            Programs = Current.Keys.Concat(old.Keys).Select(BackupPolicy.ProgramName).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList().AsReadOnly();
        }
    }
    public sealed class BackupResult
    {
        public bool Success { get; private set; }
        public string Error { get; private set; }
        public string CompletedUtc { get; private set; }
        internal BackupResult(bool success, string error = null, string completedUtc = null)
        { Success = success; Error = error; CompletedUtc = completedUtc; }
    }
    public interface IChangeSetTransport
    {
        // Streams are already verified and locked; implementation must await the final session ACK.
        void Send(IReadOnlyDictionary<string, Stream> uploads, IReadOnlyCollection<string> deletions);
    }
    public sealed class LoopbackChangeSetTransport : IChangeSetTransport
    {
        private readonly Func<Client> connect;
        public LoopbackChangeSetTransport(Func<Client> connect) { this.connect = connect; }
        public void Send(IReadOnlyDictionary<string, Stream> uploads, IReadOnlyCollection<string> deletions)
        {
            using (var client = connect())
                if (!client.BackupChanges(uploads, deletions)) throw new IOException(client.LastError);
        }
    }
    public sealed class BackupWorkflow
    {
        // Serializes check/commit within this process, including multiple UI callbacks.
        private static readonly SemaphoreSlim Gate = new SemaphoreSlim(1, 1);
        private readonly IFileSnapshotScanner scanner;
        private readonly ISuccessSnapshotStore store;
        public BackupWorkflow(IFileSnapshotScanner scanner, ISuccessSnapshotStore store)
        { this.scanner = scanner; this.store = store; }
        public ChangeSet Check(BackupPolicy policy, CancellationToken token = default(CancellationToken))
        {
            Gate.Wait(token);
            try { var current = scanner.Scan(policy, token); token.ThrowIfCancellationRequested(); return new ChangeSet(policy, current, store.Load()); }
            finally { Gate.Release(); }
        }
        public BackupResult Execute(ChangeSet plan, IChangeSetTransport transport, CancellationToken token = default(CancellationToken))
        {
            Gate.Wait(token);
            try
            {
                var opened = new Dictionary<string, Stream>(StringComparer.OrdinalIgnoreCase);
                IDisposable lease = null;
                try
                {
                    token.ThrowIfCancellationRequested();
                    if (plan == null) throw new InvalidOperationException("Check before backup.");
                    lease = store.AcquireWriteLease();
                    var previous = store.Load();
                    if ((previous == null ? null : previous.Revision) != plan.Revision)
                        throw new InvalidOperationException("Successful baseline changed; check again.");
                    if (!plan.HasChanges) return new BackupResult(true);
                    // Keep all scanned files locked until commit so the saved hashes describe sent bytes.
                    foreach (var file in plan.Current)
                    {
                        token.ThrowIfCancellationRequested();
                        var input = new FileStream(SafePathResolver.ResolveFile(plan.Policy.Root, file.Key), FileMode.Open, FileAccess.Read, FileShare.Read);
                        opened.Add(file.Key, input);
                        if (FileSnapshotScanner.Digest(input, token) != file.Value) throw new IOException("Source changed; check again: " + file.Key);
                        input.Position = 0;
                    }
                    // Also catch new files, newly unavailable paths and reappearing deletions before any network operation.
                    var verified = scanner.Scan(plan.Policy, token);
                    if (verified.Count != plan.Current.Count || verified.Any(x => !plan.Current.ContainsKey(x.Key) || plan.Current[x.Key] != x.Value))
                        throw new IOException("Source changed; check again.");
                    var uploads = plan.Added.Concat(plan.Modified).ToDictionary(x => x, x => opened[x], StringComparer.OrdinalIgnoreCase);
                    if (uploads.Count + plan.Deleted.Count > TransferOptions.MaxEntries) throw new InvalidDataException("Too many changes.");
                    token.ThrowIfCancellationRequested();
                    transport.Send(uploads, plan.Deleted);
                    token.ThrowIfCancellationRequested();
                    var next = new SuccessfulSnapshot { Identity = plan.Policy.Identity, Revision = Guid.NewGuid().ToString("N"), CompletedUtc = DateTime.UtcNow.ToString("o") };
                    if (!plan.IsInitial) foreach (var file in previous.Files) next.Files.Add(file.Key, file.Value);
                    foreach (string file in plan.Deleted) next.Files.Remove(file);
                    foreach (var file in plan.Current) next.Files[file.Key] = file.Value;
                    if (next.Files.Count > TransferOptions.MaxEntries) throw new InvalidDataException("Successful snapshot limit exceeded.");
                    token.ThrowIfCancellationRequested();
                    store.Save(next); // Commit boundary: after this returns, success is durable even if Stop arrives.
                    return new BackupResult(true, completedUtc: next.CompletedUtc);
                }
                catch (Exception ex) { return new BackupResult(false, ex.Message); }
                finally { foreach (var stream in opened.Values) stream.Dispose(); lease?.Dispose(); }
            }
            finally { Gate.Release(); }
        }
    }
}
