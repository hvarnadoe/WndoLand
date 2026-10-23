namespace WndoLand
{
    internal static class AppIcon
    {
        private const string ResourceName = "wndoland.ico";

        public static Icon Window { get; } = Load(SystemInformation.IconSize);

        public static Icon Tray { get; } = Load(SystemInformation.SmallIconSize);

        private static Icon Load(Size size)
        {
            using var stream = typeof(AppIcon).Assembly.GetManifestResourceStream(ResourceName);
            return stream == null ? SystemIcons.Application : new Icon(stream, size);
        }
    }
}
