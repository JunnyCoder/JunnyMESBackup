using System.Threading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using JunnyMES.Core.Interfaces;
using JunnyMES.Infrastructure.Configuration;
using JunnyMES.Core.Logging;

using JunnyMES.Infrastructure.Storage;

namespace NetworkManager
{
    public sealed class Server
    {
        public event EventHandler<string> MessageReceived;
        private readonly object gate = new object();
        private readonly IPresetStore store;
        private readonly IAppLogger logger;
        private readonly IBackupFileStore files;
        private readonly TransferOptions options;
        private readonly IJsonSerializer serializer = new DataContractJsonCodec();
        private readonly int port;
        private readonly System.Net.IPAddress bindAddress;
        private volatile bool stopRequested;
        private bool running;
        private TcpClient active;
        public TcpListener listener;
        public Dictionary<string, List<string>> serverPreset;

        public Server(IPresetStore store, ref Dictionary<string, List<string>> server_Preset,
            string portNumber = "8080", string IPAddress = null, string basePath = null,
            IBackupFileStore files = null, TransferOptions options = null, IAppLogger logger = null)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            this.logger = logger;
            DemoPaths.ValidateEndpoint(IPAddress ?? "127.0.0.1", portNumber);
            // Legacy optional basePath must not create an alternate, unvalidated write root.
            if (basePath != null) throw new ArgumentException("Use the validated savePath setting.", nameof(basePath));
            DemoPaths.Normalize(server_Preset);
            serverPreset = server_Preset;
            port = int.Parse(portNumber);
            bindAddress=System.Net.IPAddress.Parse(IPAddress ?? "127.0.0.1");
            this.files = files ?? new BackupFileStore();
            this.options = options ?? new TransferOptions();
        }
        public void Start() { Start(CancellationToken.None); }
        public void Start(CancellationToken token)
        {
            using (token.Register(Stop)) Run(token);
        }
        private void Run(CancellationToken token)
        {
            lock (gate)
            {
                if (token.IsCancellationRequested) return;
                if (running) throw new InvalidOperationException("Server is already running.");
                stopRequested = false;
                listener = new TcpListener(bindAddress, port);
                try { listener.Start(); running = true; }
                catch (SocketException ex) { listener.Stop(); Notify("Server start failed: " + ex.Message); return; }
            }
            Notify("Server started");
            try
            {
                while (!stopRequested)
                {
                    TcpClient connection;
                    try { connection = listener.AcceptTcpClient(); }
                    catch (SocketException) { if (stopRequested) break; throw; }
                    catch (ObjectDisposedException) { if (stopRequested) break; throw; }
                    catch (InvalidOperationException) { if (stopRequested) break; throw; }
                    lock (gate)
                    {
                        if (stopRequested) { connection.Close(); break; }
                        active = connection;
                    }
                    using (connection)
                    {
                        WireChannel channel = null;
                        try
                        {
                            connection.NoDelay = true;
                            channel = new WireChannel(connection.GetStream(), options);
                            Handle(connection, channel);
                        }
                        catch (Exception ex)
                        {
                            if (!stopRequested)
                            {
                                try { channel?.WriteLine("ERROR"); } catch (Exception) { }
                                Notify("Connection failed: " + ex.Message);
                            }
                        }
                        finally { lock (gate) { if (active == connection) active = null; } }
                    }
                }
            }
            catch (SocketException ex) { if (!stopRequested) Notify("Listener failed: " + ex.Message); }
            finally
            {
                lock (gate) { active?.Close(); active = null; listener.Stop(); running = false; }
                Notify("Server stopped");
            }
        }
        private void Handle(TcpClient connection, WireChannel channel)
        {
            var preset = DemoPaths.Normalize(serverPreset);
            string peer = ((IPEndPoint)connection.Client.RemoteEndPoint).Address.ToString();
            int index = preset["registered_IP"].IndexOf(peer);
            if (index < 0) { channel.WriteLine("NOT_REGISTERED"); return; }
            string root = preset["savePath"][index];
            channel.WriteLine(WireChannel.Greeting);
            int completed = 0;
            while (!stopRequested)
            {
                string command = channel.ReadLine();
                if (command == "END_CONNECTION")
                {
                    if (completed > 0)
                    {
                        var next = DemoPaths.Normalize(serverPreset);
                        next["last_Check"][index] = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss");
                        store.Save(next); // No successful session ACK before settings persistence succeeds.
                        serverPreset["last_Check"][index] = next["last_Check"][index];
                    }
                    channel.WriteLine("CLOSE_PERMITTED");
                    Notify(completed > 0 ? "Backup session completed" : "No changes requested");
                    return;
                }
                if (command == "PRESET_REQUEST")
                {
                    if (completed != 0) throw new InvalidDataException("Preset requests use a separate session.");
                    byte[] data = Encoding.UTF8.GetBytes(serializer.Serialize(preset));
                    if (data.Length > TransferOptions.MaxPresetBytes) throw new InvalidDataException("Preset exceeds limit.");
                    channel.WriteLine("PRESET_BYTES"); channel.WriteLength(data.Length);
                    using (var input = new MemoryStream(data)) channel.SendBody(input, data.Length);
                    return;
                }
                if (completed >= TransferOptions.MaxEntries) throw new InvalidDataException("Too many operations.");
                if (command == "PUT_FILE")
                {
                    string relative = channel.ReadLine();
                    SafePathResolver.ResolveFile(root, relative);
                    long length = channel.ReadLength(TransferOptions.MaxFileBytes);
                    channel.WriteLine("READY");
                    bool changed = files.Receive(root, relative, output => channel.CopyBodyTo(output, length));
                    channel.WriteLine(changed ? "TRANSFER_COMPLETED" : "UNCHANGED");
                }
                else if (command == "DELETE_REQUEST")
                {
                    string relative = channel.ReadLine();
                    SafePathResolver.ResolveFile(root, relative);
                    channel.WriteLine(files.ArchiveDelete(root, relative) ? "DELETE_COMPLETED" : "ALREADY_APPLIED");
                }
                else if (command == "CREATE_DIRECTORY")
                {
                    string relative = channel.ReadLine();
                    SafePathResolver.ResolveFile(root, relative);
                    files.CreateDirectory(root, relative); channel.WriteLine("DIRECTORY_COMPLETED");
                }
                else throw new InvalidDataException("Unknown command.");
                completed++;
            }
        }
        public void Stop()
        {
            lock (gate)
            {
                stopRequested = true;
                active?.Close();
                listener?.Stop();
            }
        }
        private void Notify(string message)
        {
            try { logger?.Log(LogLevel.Operation, "NetworkServer", message); MessageReceived?.Invoke(this, message); }
            catch (Exception) { /* A detached UI must not turn a committed file into a protocol failure. */ }
        }
    }
}
