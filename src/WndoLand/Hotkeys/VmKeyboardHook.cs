using System.Diagnostics;
using System.Runtime.InteropServices;
using static WndoLand.Win32;

namespace WndoLand
{
    /// <summary>
    /// VirtualBox installs its own low-level keyboard hook while a VM has focus and swallows keys before the system
    /// processes RegisterHotKey registrations. While a VirtualBox window is in the foreground this hook matches the
    /// registered hotkeys itself and posts the same WM_HOTKEY message RegisterHotKey would have produced.
    /// </summary>
    internal sealed class VmKeyboardHook : IDisposable
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;

        private readonly LowLevelKeyboardProc _proc;
        private readonly nint _target;
        private List<Keybinds> _binds = new();
        private nint _hook;

        private delegate nint LowLevelKeyboardProc(int code, nint wParam, nint lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern nint SetWindowsHookEx(int id, LowLevelKeyboardProc proc, nint module, uint threadId);
        [DllImport("user32.dll")] private static extern bool UnhookWindowsHookEx(nint hook);
        [DllImport("user32.dll")] private static extern nint CallNextHookEx(nint hook, int code, nint wParam, nint lParam);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] private static extern nint GetModuleHandle(string? name);

        public VmKeyboardHook(nint targetWindow)
        {
            _target = targetWindow;
            _proc = OnKey;
        }

        public void SetBindings(List<Keybinds> binds) => _binds = binds.ToList();

        /// <summary>(Re)installs the hook. Hooks run newest-first, so this puts ours ahead of VirtualBox's.</summary>
        public void Install()
        {
            Uninstall();
            _hook = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle(null), 0);
            if (_hook == 0)
                Log.Warn($"Couldn't install the VirtualBox keyboard hook, Win32 error {Marshal.GetLastWin32Error()}");
        }

        public void Uninstall()
        {
            if (_hook == 0) return;
            UnhookWindowsHookEx(_hook);
            _hook = 0;
        }

        public static bool IsVirtualBox(nint hwnd)
        {
            if (hwnd == 0) return false;
            GetWindowThreadProcessId(hwnd, out uint pid);
            try
            {
                using var p = Process.GetProcessById((int)pid);
                return p.ProcessName.StartsWith("VirtualBox", StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        }

        private static bool Down(Keys key) => (GetAsyncKeyState((int)key) & 0x8000) != 0;

        private nint OnKey(int code, nint wParam, nint lParam)
        {
            if (code >= 0 && (wParam == WM_KEYDOWN || wParam == WM_SYSKEYDOWN))
            {
                var vk = (Keys)Marshal.ReadInt32(lParam);

                uint mods = 0;
                if (Down(Keys.Menu)) mods |= MOD_ALT;
                if (Down(Keys.ControlKey)) mods |= MOD_CONTROL;
                if (Down(Keys.ShiftKey)) mods |= MOD_SHIFT;
                if (Down(Keys.LWin) || Down(Keys.RWin)) mods |= MOD_WIN;

                if (mods != 0)
                {
                    for (int i = 0; i < _binds.Count; i++)
                    {
                        if (_binds[i].Key != vk || _binds[i].Modifiers != mods) continue;
                        if (!IsVirtualBox(GetForegroundWindow())) break;

                        Log.Debug($"VirtualBox hook: hotkey #{i} '{_binds[i].Name}'");
                        PostMessage(_target, (uint)WM_HOTKEY, i, 0);
                        return 1;
                    }
                }
            }
            return CallNextHookEx(_hook, code, wParam, lParam);
        }

        public void Dispose() => Uninstall();
    }
}
