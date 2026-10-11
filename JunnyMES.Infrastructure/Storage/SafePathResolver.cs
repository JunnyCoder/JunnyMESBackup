using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace JunnyMES.Infrastructure.Storage
{
    public static class SafePathResolver
    {
        public const string InternalDirectory = ".junny-mes";
        public static string ResolveFile(string root, string relativePath)
        {
            return Resolve(root, relativePath, false);
        }

        internal static string ResolveInternal(string root, string relativePath)
        {
            return Resolve(root, relativePath, true);
        }

        private static string Resolve(string root, string relativePath, bool internalPath)
        {
            if (string.IsNullOrWhiteSpace(root) || !Path.IsPathRooted(root))
                throw new InvalidDataException("An absolute root is required.");
            if (string.IsNullOrWhiteSpace(relativePath) || relativePath.Any(char.IsControl) ||
                relativePath.IndexOfAny(Path.GetInvalidPathChars()) >= 0 || Path.IsPathRooted(relativePath))
                throw new InvalidDataException("A relative file path is required.");
            string[] segments = relativePath.Replace('/', '\\').Split('\\');
            foreach (string segment in segments)
            {
                if (segment.Length == 0 || segment == "." || segment == ".." ||
                    segment.Any(char.IsControl) || segment.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
                    segment.EndsWith(".", StringComparison.Ordinal) || char.IsWhiteSpace(segment[segment.Length - 1]) ||
                    Regex.IsMatch(segment.Split('.')[0].TrimEnd(' '), @"^(CON|PRN|AUX|NUL|CLOCK\$|CONIN\$|CONOUT\$|COM[1-9¹²³]|LPT[1-9¹²³])$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant) ||
                    (!internalPath && segment.Equals(InternalDirectory, StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidDataException("Invalid or reserved path segment.");
            }
            string basePath = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string target = Path.GetFullPath(Path.Combine(basePath, Path.Combine(segments)));
            if (!target.StartsWith(basePath, StringComparison.OrdinalIgnoreCase) || target.Length <= basePath.Length)
                throw new InvalidDataException("Path leaves the configured root.");
            CheckAncestors(target);
            return target;
        }

        // Checks existing file/directory segments, including the configured root's ancestors.
        // A hostile local process racing path replacement is outside the demo threat model.
        private static void CheckAncestors(string path)
        {
            for (string current = path; !string.IsNullOrEmpty(current); current = Path.GetDirectoryName(current))
            {
                try
                {
                    if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                        throw new InvalidDataException("Reparse points are not supported.");
                }
                catch (FileNotFoundException) { }
                catch (DirectoryNotFoundException) { }
            }
        }

        public static string RelativeFile(string root, string absolutePath)
        {
            if (string.IsNullOrWhiteSpace(root) || string.IsNullOrWhiteSpace(absolutePath) || !Path.IsPathRooted(absolutePath))
                throw new InvalidDataException("An absolute source path is required.");
            string basePath = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string full = Path.GetFullPath(absolutePath);
            if (!full.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Source path leaves the configured root.");
            string relative = full.Substring(basePath.Length);
            ResolveFile(root, relative);
            return relative.Replace('\\', '/');
        }
    }
}
