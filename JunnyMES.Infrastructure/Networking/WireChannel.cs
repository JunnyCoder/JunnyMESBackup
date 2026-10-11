using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace NetworkManager
{
    /// <summary>One owner for UTF-8/LF controls and exact-length binary bodies. No read-ahead.</summary>
    public sealed class WireChannel
    {
        public const string Greeting = "JUNNY_MES/1";
        private static readonly Encoding Utf8 = new UTF8Encoding(false, true);
        private readonly Stream stream;
        private readonly TransferOptions options;
        private readonly Stopwatch elapsed = Stopwatch.StartNew();
        public WireChannel(Stream stream, TransferOptions options = null)
        {
            this.stream = stream ?? throw new ArgumentNullException(nameof(stream));
            this.options = options ?? new TransferOptions();
        }
        private void SetDeadline()
        {
            long remaining = options.SessionTimeoutMs - elapsed.ElapsedMilliseconds;
            if (remaining <= 0) throw new TimeoutException("Session deadline exceeded.");
            if (stream.CanTimeout)
            {
                int timeout = (int)Math.Min(options.IdleTimeoutMs, remaining);
                stream.ReadTimeout = timeout;
                stream.WriteTimeout = timeout;
            }
        }
        public string ReadLine()
        {
            using (var bytes = new MemoryStream())
            {
                while (true)
                {
                    SetDeadline();
                    int value = stream.ReadByte();
                    if (value < 0) throw new EndOfStreamException("EOF in control message.");
                    if (value == '\n') break;
                    if (value == '\r' || bytes.Length >= TransferOptions.MaxControlBytes)
                        throw new InvalidDataException("Invalid/oversized control message.");
                    bytes.WriteByte((byte)value);
                }
                string line = Utf8.GetString(bytes.ToArray());
                if (line.IndexOf('\uFEFF') >= 0 || line.IndexOf('\0') >= 0)
                    throw new InvalidDataException("BOM/NUL is not allowed in controls.");
                return line;
            }
        }
        public void WriteLine(string line)
        {
            if (line == null || line.IndexOfAny(new[] { '\r', '\n', '\0', '\uFEFF' }) >= 0)
                throw new InvalidDataException("Invalid control message.");
            byte[] bytes = Utf8.GetBytes(line);
            if (bytes.Length > TransferOptions.MaxControlBytes) throw new InvalidDataException("Control message too long.");
            SetDeadline(); stream.Write(bytes, 0, bytes.Length);
            SetDeadline(); stream.WriteByte((byte)'\n'); stream.Flush();
        }
        public void Expect(string expected)
        {
            string actual = ReadLine();
            if (actual != expected) throw new InvalidDataException("Unexpected response: " + actual);
        }
        public long ReadLength(long maximum)
        {
            long value;
            if (!long.TryParse(ReadLine(), NumberStyles.None, CultureInfo.InvariantCulture, out value) || value < 0 || value > maximum)
                throw new InvalidDataException("Invalid or oversized body length.");
            return value;
        }
        public void WriteLength(long length) { WriteLine(length.ToString(CultureInfo.InvariantCulture)); }
        public void CopyBodyTo(Stream output, long length)
        {
            if (length < 0 || length > TransferOptions.MaxFileBytes) throw new InvalidDataException("Body length out of range.");
            byte[] buffer = new byte[TransferOptions.BufferBytes];
            while (length > 0)
            {
                SetDeadline();
                int read = stream.Read(buffer, 0, (int)Math.Min(buffer.Length, length));
                if (read == 0) throw new EndOfStreamException("EOF before body completed.");
                output.Write(buffer, 0, read);
                length -= read;
            }
        }
        public void SendBody(Stream input, long length)
        {
            if (length < 0 || length > TransferOptions.MaxFileBytes) throw new InvalidDataException("Body length out of range.");
            byte[] buffer = new byte[TransferOptions.BufferBytes];
            while (length > 0)
            {
                SetDeadline();
                int read = input.Read(buffer, 0, (int)Math.Min(buffer.Length, length));
                if (read == 0) throw new EndOfStreamException("Source file shortened during transfer.");
                SetDeadline(); stream.Write(buffer, 0, read);
                length -= read;
            }
            SetDeadline(); stream.Flush();
        }
    }
}
