using System;

namespace NetworkManager
{
    public sealed class TransferOptions
    {
        public const long MaxFileBytes = 100L * 1024 * 1024;
        public const int MaxPresetBytes = 1024 * 1024;
        public const int MaxControlBytes = 8192;
        public const int BufferBytes = 65536;
        public const int MaxEntries = 10000;
        public int ConnectTimeoutMs { get; private set; }
        public int IdleTimeoutMs { get; private set; }
        public int SessionTimeoutMs { get; private set; }
        public TransferOptions(int connectTimeoutMs = 5000, int idleTimeoutMs = 15000, int sessionTimeoutMs = 120000)
        {
            if (connectTimeoutMs <= 0 || idleTimeoutMs <= 0 || sessionTimeoutMs <= 0)
                throw new ArgumentOutOfRangeException(nameof(connectTimeoutMs), "Timeouts must be positive.");
            ConnectTimeoutMs = connectTimeoutMs;
            IdleTimeoutMs = idleTimeoutMs;
            SessionTimeoutMs = sessionTimeoutMs;
        }
    }
}
