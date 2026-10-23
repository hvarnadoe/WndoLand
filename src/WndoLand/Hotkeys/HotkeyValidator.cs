using static WndoLand.Win32;

namespace WndoLand
{
    internal static class HotkeyValidator
    {
        private const int TestId = 0x7FFF;

        public static string? Check(uint modifiers, Keys key, int editingUserIndex, nint hwnd)
        {
            var user = KeybindStore.LoadUser();

            var clash = KeybindStore.LoadDefault().FirstOrDefault(k => Same(k, modifiers, key));
            for (int i = 0; i < user.Count && clash == null; i++)
                if (i != editingUserIndex && Same(user[i], modifiers, key)) clash = user[i];
            if (clash != null) return $"Already used by \"{clash.Name}\".";

            // We hold the edited hotkey's registration ourselves, so the Windows test below would always fail
            if (editingUserIndex >= 0 && editingUserIndex < user.Count && Same(user[editingUserIndex], modifiers, key))
                return null;

            if (!RegisterHotKey(hwnd, TestId, modifiers, (uint)key))
                return "That combination is already in use by Windows or another program.";
            UnregisterHotKey(hwnd, TestId);
            return null;
        }

        private static bool Same(Keybinds k, uint modifiers, Keys key) => k.Modifiers == modifiers && k.Key == key;
    }
}
