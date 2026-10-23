using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using static WndoLand.Win32;

namespace WndoLand
{
    internal class AppRule
    {
        [JsonPropertyName("process")]
        public string? Process { get; init; }

        [JsonPropertyName("class")]
        public string? WindowClass { get; init; }

        [JsonPropertyName("title_contains")]
        public string? TitleContains { get; init; }

        [JsonPropertyName("action")]
        public string Action { get; init; } = "float";

        [JsonPropertyName("workspace")]
        public int Workspace { get; init; }

        public bool Matches(string process, string cls, string title) =>
            (Process == null || string.Equals(Process.Replace(".exe", "", StringComparison.OrdinalIgnoreCase), process, StringComparison.OrdinalIgnoreCase))
            && (WindowClass == null || string.Equals(WindowClass, cls, StringComparison.Ordinal))
            && (TitleContains == null || title.Contains(TitleContains, StringComparison.OrdinalIgnoreCase));
    }

    internal static class Rules
    {
        private const string ExampleRules = "[\n  {\n    \"process\": \"CalculatorApp\",\n    \"action\": \"float\"\n  }\n]\n";

        private static List<AppRule> _rules = new();
        private static DateTime _loadedWriteTime = DateTime.MinValue;

        public static void EnsureFile()
        {
            if (!File.Exists(AppPaths.RulesFile))
                File.WriteAllText(AppPaths.RulesFile, ExampleRules);
        }

        private static void Refresh()
        {
            try
            {
                EnsureFile();
                var written = File.GetLastWriteTimeUtc(AppPaths.RulesFile);
                if (written == _loadedWriteTime) return;
                _loadedWriteTime = written;
                _rules = JsonSerializer.Deserialize<List<AppRule>>(File.ReadAllText(AppPaths.RulesFile)) ?? new();
                Log.Info($"Loaded {_rules.Count} app rule(s)");
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Couldn't read {AppPaths.RulesFile}");
            }
        }

        public static AppRule? Match(nint hwnd)
        {
            Refresh();
            if (_rules.Count == 0) return null;

            var cls = new StringBuilder(256);
            GetClassName(hwnd, cls, cls.Capacity);
            var title = new StringBuilder(256);
            GetWindowText(hwnd, title, title.Capacity);
            GetWindowThreadProcessId(hwnd, out uint pid);

            string process;
            try { using var p = System.Diagnostics.Process.GetProcessById((int)pid); process = p.ProcessName; }
            catch { return null; }

            var rule = _rules.FirstOrDefault(r => r.Matches(process, cls.ToString(), title.ToString()));
            if (rule != null) Log.Info($"Rule matched {process} \"{title}\": {rule.Action}{(rule.Action == "workspace" ? " " + rule.Workspace : "")}");
            return rule;
        }
    }
}
