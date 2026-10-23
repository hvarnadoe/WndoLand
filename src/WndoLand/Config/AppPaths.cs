namespace WndoLand
{
    internal static class AppPaths
    {
        public static string DataDir { get; } =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WndoLand");

        public static string DefaultKeybinds => Path.Combine(AppContext.BaseDirectory, "keybinds.json");
        public static string UserKeybinds => Path.Combine(DataDir, "user_keybinds.json");
        public static string SettingsFile => Path.Combine(DataDir, "settings.json");
        public static string RulesFile => Path.Combine(DataDir, "rules.json");
        public static string StateFile => Path.Combine(DataDir, "state.json");
        public static string BorderStateFile => Path.Combine(DataDir, "borders.json");

        static AppPaths() => Directory.CreateDirectory(DataDir);
    }
}
