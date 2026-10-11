using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using JunnyMES.Core.Configuration;
using JunnyMES.Infrastructure.Storage;
namespace JunnyMES.Infrastructure.Configuration
{
    public static class DemoPaths
    {
        public static string Root
        {
            get
            {
                string configured = Environment.GetEnvironmentVariable("JUNNY_MES_DEMO_ROOT");
                if (!string.IsNullOrWhiteSpace(configured)) return Path.GetFullPath(configured);
                for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
                    if (File.Exists(Path.Combine(directory.FullName, "JunnyMESBackup.sln"))) return directory.FullName;
                throw new InvalidDataException("Keep the repository layout or set JUNNY_MES_DEMO_ROOT.");
            }
        }
        public static string Config(string role) { return Path.Combine(Root, "data", "config", role + ".json"); }
        public static string State(string role) { return Path.Combine(Root, "data", "state", role); }
        public static void ValidateEndpoint(string address, string port)
        {
            IPAddress ip; int number;
            if (!IPAddress.TryParse(address, out ip) || ip.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork || !IPAddress.IsLoopback(ip))
                throw new InvalidDataException("This portfolio demo accepts IPv4 loopback only.");
            if (!int.TryParse(port, out number) || number < 1 || number > 65535) throw new InvalidDataException("Invalid port.");
        }
        public static string Relative(string absolute)
        {
            string root = Path.GetFullPath(Root).TrimEnd('\\') + "\\";
            string full = Path.GetFullPath(absolute);
            if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Path must stay inside the demo root.");
            return full.Substring(root.Length).Replace('\\', '/');
        }
        private static string Resolve(string path, string area)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new InvalidDataException("Empty data path.");
            string parent = Path.GetFullPath(Path.Combine(Root, "data", area));
            string full = Path.GetFullPath(Path.IsPathRooted(path) ? path : Path.Combine(Root, path));
            if (!full.StartsWith(parent.TrimEnd('\\') + "\\", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Path must stay inside data/" + area + ".");
            SafePathResolver.ResolveFile(full, "validation-probe");
            return full;
        }
        public static Dictionary<string, List<string>> Normalize(Dictionary<string, List<string>> source)
        {
            PresetValidator.Validate(source);
            var result = PresetEditMerger.Clone(source);
            ValidateEndpoint(result["ServerIP"][0], result["ServerPort"][0]);
            foreach (string ip in result["registered_IP"]) ValidateEndpoint(ip, result["ServerPort"][0]);
            for (int i = 0; i < result["registered_IP"].Count; i++)
            {
                result["checkingTargetPath"][i] = Resolve(result["checkingTargetPath"][i], "source");
                result["savePath"][i] = Resolve(result["savePath"][i], "backup");
            }
            return result;
        }
        public static Dictionary<string, List<string>> Defaults()
        {
            var result = new Dictionary<string, List<string>>();
            foreach (var field in PresetSchema.Fields) result[field.Key] = new List<string>();
            for (int i = 1; i <= 15; i++)
            {
                string name = "DemoSystem_" + i.ToString("D2");
                result["registered_IP"].Add("127.0.0." + i);
                result["registered_SystemName"].Add(name);
                result["checkingTargetPath"].Add("data/source/" + name);
                result["savePath"].Add("data/backup/" + name);
                result["folderDepth"].Add("4");
                result["check_interval_type"].Add("1");result["check_interval_hour"].Add("0");result["check_interval_minute"].Add("5");result["check_interval_second"].Add("0");
                result["last_Check"].Add("N/A");result["update_only"].Add("false");
            }
            result["ServerIP"].Add("127.0.0.1"); result["ServerPort"].Add("18080");
            result["skipKeywordsFolder"].AddRange(new[] {"temp", "cache"});result["skipKeywordsFile"].Add(".tmp");
            result["FilesToCheck"] = new List<string> {"ProgramA", "ProgramB"};
            return Normalize(result);
        }
    }
}
