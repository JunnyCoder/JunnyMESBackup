using System;
using System.IO;

namespace JunnyMES.Infrastructure.Storage
{
    public interface IBackupFileStore
    {
        bool Receive(string root, string relativePath, Action<Stream> receiveBody);
        bool ArchiveDelete(string root, string relativePath);
        void CreateDirectory(string root, string relativePath);
    }

    public sealed class BackupFileStore : IBackupFileStore
    {
        // true: replaced/created; false: identical bytes already committed.
        public bool Receive(string root, string relativePath, Action<Stream> receiveBody)
        {
            string target = SafePathResolver.ResolveFile(root, relativePath);
            string temporaryRelative = SafePathResolver.InternalDirectory + "/incoming/" + Guid.NewGuid().ToString("N") + ".tmp";
            string temporary = SafePathResolver.ResolveInternal(root, temporaryRelative);
            Directory.CreateDirectory(Path.GetDirectoryName(temporary));
            temporary = SafePathResolver.ResolveInternal(root, temporaryRelative);
            try
            {
                using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    receiveBody(output);
                    output.Flush(true);
                }
                target = SafePathResolver.ResolveFile(root, relativePath);
                if (File.Exists(target) && SameBytes(target, temporary)) return false;
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                target = SafePathResolver.ResolveFile(root, relativePath);
                if (File.Exists(target))
                    File.Replace(temporary, target, ArchivePath(root, relativePath), false);
                else
                    File.Move(temporary, target);
                return true;
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }

        public bool ArchiveDelete(string root, string relativePath)
        {
            string target = SafePathResolver.ResolveFile(root, relativePath);
            if (Directory.Exists(target)) throw new InvalidDataException("Delete requests must name individual files.");
            if (!File.Exists(target)) return false;
            string archive = ArchivePath(root, relativePath);
            target = SafePathResolver.ResolveFile(root, relativePath);
            File.Move(target, archive);
            return true;
        }
        public void CreateDirectory(string root, string relativePath)
        {
            string target = SafePathResolver.ResolveFile(root, relativePath);
            Directory.CreateDirectory(target);
            SafePathResolver.ResolveFile(root, relativePath);
        }
        private static string ArchivePath(string root, string relativePath)
        {
            string relative = SafePathResolver.InternalDirectory + "/versions/" + Guid.NewGuid().ToString("N") + "/" + relativePath;
            string result = SafePathResolver.ResolveInternal(root, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(result));
            return SafePathResolver.ResolveInternal(root, relative);
        }
        private static bool SameBytes(string first, string second)
        {
            using (var a = new FileStream(first, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var b = new FileStream(second, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (a.Length != b.Length) return false;
                byte[] x = new byte[65536], y = new byte[65536];
                int read;
                while ((read = a.Read(x, 0, x.Length)) != 0)
                {
                    int offset = 0;
                    while (offset < read)
                    {
                        int count = b.Read(y, offset, read - offset);
                        if (count == 0) return false;
                        offset += count;
                    }
                    for (int i = 0; i < read; i++) if (x[i] != y[i]) return false;
                }
                return true;
            }
        }
    }
}
