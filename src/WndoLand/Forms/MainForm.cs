using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Text.Json;
using static WndoLand.Win32;

namespace WndoLand
{
    public partial class MainForm : Form
    {
        private readonly NotifyIcon _tray;
        private readonly UpdateService _updates = new();
        private bool _reallyExit;
        private bool _allowVisible = false;
        private Workspaces _workspace;
        private readonly WorkspaceOverlay _overlay = new();
        private readonly ActiveBorder _activeBorder;
        private List<Func<Task>> _keybindActions;

        private readonly VmKeyboardHook _vmHook;
        readonly WinEventDelegate _callback;
        IntPtr _hook;
        IntPtr _foregroundHook;
        readonly HashSet<IntPtr> _seen = new();

        private enum HotkeyId
        {
            MoveWindowUp,
            MoveWindowDown,
            MoveWindowLeft,
            MoveWindowRight,
            FullScreenWindow
        }

        public MainForm()
        {
            InitializeComponent();
            Theme.Apply(this);

            _allowVisible = Environment.GetCommandLineArgs().Contains("--show");

            Borders.RecoverFromCrash();

            _callback = OnWinEvent;
            _vmHook = new VmKeyboardHook(Handle);

            var menu = new ContextMenuStrip();
            menu.Items.Add("Open", null, (_, _) => ShowWindow());
            var installUpdate = new ToolStripMenuItem("Restart to install update") { Visible = false };
            installUpdate.Click += (_, _) => _updates.ApplyPending();
            menu.Items.Add(installUpdate);
            menu.Items.Add("Check for updates", null, async (_, _) =>
            {
                if (!_updates.IsInstalled)
                    _tray!.ShowBalloonTip(3000, "WndoLand", "Updates are only available in the installed version.", ToolTipIcon.Info);
                else if (!await _updates.CheckAsync())
                    _tray!.ShowBalloonTip(3000, "WndoLand", "You're up to date.", ToolTipIcon.Info);
            });
            menu.Items.Add("Exit", null, (_, _) => { _reallyExit = true; Close(); });

            _tray = new NotifyIcon
            {
                Icon = AppIcon.Tray,
                Text = "WndoLand",
                ContextMenuStrip = menu,
                Visible = true
            };
            _tray.DoubleClick += (_, _) => ShowWindow();

            _updates.UpdateReady += version =>
            {
                installUpdate.Text = $"Restart to install update ({version})";
                installUpdate.Visible = true;
                _tray.ShowBalloonTip(5000, "WndoLand", $"Version {version} is ready. Use the tray menu to restart and install it.", ToolTipIcon.Info);
            };
            _updates.Start();

            _workspace = new Workspaces { WindowPruned = hwnd => _seen.Remove(hwnd) };
            _workspace.WorkspaceChanged += (num, monitor) => _overlay.Flash(num, monitor);
            Microsoft.Win32.SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;

            var shortcuts = LoadKeybindsFromFile();
            Log.Info($"Loaded {shortcuts.Count} keybind(s)");

            _keybindActions = new List<Func<Task>>();
            RegisterKeybinds(shortcuts);
            AddExistingWindows();

            _workspace.Changed += SaveLayout;
            SaveLayout();

            _activeBorder = new ActiveBorder(hwnd => _seen.Contains(hwnd));
            _activeBorder.Start();

            Watchdog.Start();
        }

        private void OnDisplaySettingsChanged(object? sender, EventArgs e)
        {
            if (IsHandleCreated)
                BeginInvoke(() =>
                {
                    Log.Info("Display configuration changed");
                    _workspace.OnDisplaySettingsChanged();
                });
        }

        private void SaveLayout() => StateStore.Save(_workspace.Snapshot());

        private List<Keybinds> LoadKeybindsFromFile()
        {
            return KeybindStore.LoadAll();
        }

        private void RegisterKeybinds(List<Keybinds> shortcuts)
        {
            _vmHook.SetBindings(shortcuts);
            for (int i = 0; i < shortcuts.Count; i++)
            {
                var kb = shortcuts[i];

                if (!kb.Register(i, Handle))
                    Log.Warn($"Failed to register hotkey #{i} '{kb.Name}' mods=0x{kb.Modifiers:X} key={kb.Key} (already in use?) Win32 error {Marshal.GetLastWin32Error()}");
                else
                    Log.Debug($"Registered hotkey #{i} '{kb.Name}' mods=0x{kb.Modifiers:X} key={kb.Key} target={kb.Target} action={kb.Action}");
                if (kb.Target is Target.Workspace or Target.Window)
                    _keybindActions.Add(kb.CreateAction(_workspace, kb.WorkspaceNum));
                else if (kb.Target == Target.App)
                    _keybindActions.Add(kb.CreateAction(toggleMainWindow: ToggleMainWindow));
                else
                    _keybindActions.Add(kb.CreateAction());
            }
        }

        private void ReloadKeybinds()
        {
            try
            {
                var shortcuts = LoadKeybindsFromFile();

                for (int i = 0; i < _keybindActions.Count; i++)
                    UnregisterHotKey(Handle, i);
                _keybindActions.Clear();

                RegisterKeybinds(shortcuts);
                Log.Info($"Reloaded keybinds ({shortcuts.Count})");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Couldn't reload keybinds");
                MessageBox.Show(this, $"Couldn't reload keybinds: {ex.Message}", "WndoLand");
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY)
            {
                int id = m.WParam.ToInt32();
                if (id >= 0 && id < _keybindActions.Count)
                {
                    Log.Debug($"Hotkey #{id} fired");
                    using var busy = Watchdog.Enter($"hotkey #{id}");
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    try { _keybindActions[id]().GetAwaiter().GetResult(); }
                    catch (Exception ex) { Log.Error(ex, $"Hotkey #{id} action failed"); }
                    if (sw.ElapsedMilliseconds > 300)
                        Log.Warn($"Hotkey #{id} took {sw.ElapsedMilliseconds} ms");
                }
                else
                    Log.Warn($"Hotkey fired with unknown id {id} (registered: {_keybindActions.Count})");
            }
            base.WndProc(ref m);
        }

        protected override void SetVisibleCore(bool value)
        {
            base.SetVisibleCore(_allowVisible && value);
        }

        private void ShowWindow()
        {
            _allowVisible = true;
            CentreOnScreen();
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
        }

        private void ToggleMainWindow()
        {
            if (Visible && ReferenceEquals(ActiveForm, this))
                Hide();
            else
                ShowWindow();
        }

        private void CentreOnScreen()
        {
            var area = Screen.FromPoint(Cursor.Position).WorkingArea;
            Location = new Point(area.Left + (area.Width - Width) / 2, area.Top + (area.Height - Height) / 2);
        }
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            Log.Info($"Form closing reason={e.CloseReason} reallyExit={_reallyExit}");
            if (!_reallyExit && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
                return;
            }

            Microsoft.Win32.SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
            Watchdog.Stop();
            _vmHook.Dispose();
            Borders.RestoreAll();
            _updates.Dispose();
            _tray.Visible = false;
            _tray.Dispose();
            _overlay.Dispose();
            _activeBorder.Dispose();
            base.OnFormClosing(e);
        }
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            _hook = SetWinEventHook(EVENT_OBJECT_DESTROY, EVENT_OBJECT_HIDE, IntPtr.Zero, _callback,
                                    0, 0, WINEVENT_OUTOFCONTEXT | WINEVENT_SKIPOWNPROCESS);
            if (_hook == IntPtr.Zero)
                Log.Error($"SetWinEventHook failed, Win32 error {Marshal.GetLastWin32Error()}");
            else
                Log.Info($"WinEvent hook installed (form handle 0x{Handle:X})");

            _foregroundHook = SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND, IntPtr.Zero, _callback,
                                              0, 0, WINEVENT_OUTOFCONTEXT | WINEVENT_SKIPOWNPROCESS);
            if (_foregroundHook == IntPtr.Zero)
                Log.Error($"SetWinEventHook (foreground) failed, Win32 error {Marshal.GetLastWin32Error()}");

            if (_keybindActions is { Count: > 0 })
            {
                Log.Warn("Form handle was recreated; re-registering hot keys");
                ReloadKeybinds();
            }
        }
        protected override void OnHandleDestroyed(EventArgs e)
        {
            foreach (HotkeyId id in Enum.GetValues<HotkeyId>())
                UnregisterHotKey(Handle, (int)id);

            if (_hook != IntPtr.Zero)
            {
                UnhookWinEvent(_hook);
                _hook = IntPtr.Zero;
            }
            if (_foregroundHook != IntPtr.Zero)
            {
                UnhookWinEvent(_foregroundHook);
                _foregroundHook = IntPtr.Zero;
            }
            base.OnHandleDestroyed(e);
        }

        // Shell windows that are shown and hidden but never destroyed; XamlExplorerHostIslandWindow is the Windows 11 Alt+Tab switcher
        internal static readonly HashSet<string> IgnoredClasses = new()
        {
            "XamlExplorerHostIslandWindow",
            "MultitaskingViewFrame",
            "TaskSwitcherWnd",
            "Shell_TrayWnd",
            "Shell_SecondaryTrayWnd",
            "Progman",
            "WorkerW",
            "EdgeUiInputWndClass",
            "EdgeUiInputTopWndClass",
            "NotifyIconOverflowWindow",
        };

        private static string GetWindowClass(IntPtr hwnd)
        {
            var cls = new StringBuilder(256);
            GetClassName(hwnd, cls, cls.Capacity);
            return cls.ToString();
        }

        private static readonly bool SelfElevated = new System.Security.Principal.WindowsPrincipal(
            System.Security.Principal.WindowsIdentity.GetCurrent()).IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);

        // UIPI stops a non-elevated process from moving an elevated process's windows (MoveWindow fails with error 0)
        private static bool IsElevatedProcessWeCannotMove(uint pid)
        {
            if (SelfElevated) return false;

            nint proc = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
            if (proc == 0) return true;
            try
            {
                if (!OpenProcessToken(proc, TOKEN_QUERY, out nint token)) return true;
                try
                {
                    return GetTokenInformation(token, TokenElevation, out int elevated, sizeof(int), out _) && elevated != 0;
                }
                finally { CloseHandle(token); }
            }
            finally { CloseHandle(proc); }
        }

        private static bool IsTileable(IntPtr hwnd, bool allowMinimized, out string? rejected)
        {
            rejected = null;
            if (GetAncestor(hwnd, GA_PARENT) != GetDesktopWindow()) { rejected = "not top-level"; return false; }
            if (GetWindow(hwnd, GW_OWNER) != IntPtr.Zero) { rejected = "has an owner"; return false; }
            if (!IsWindowVisible(hwnd)) { rejected = "not visible"; return false; }

            // IsWindowVisible is also true for cloaked, minimized and tool windows
            if (DwmGetWindowAttribute(hwnd, DWMWA_CLOAKED, out int cloaked, sizeof(int)) == 0 && cloaked != 0) { rejected = "cloaked"; return false; }
            if (!allowMinimized && IsIconic(hwnd)) { rejected = "minimized"; return false; }
            if ((GetWindowLong(hwnd, GWL_EXSTYLE) & WS_EX_TOOLWINDOW) != 0) { rejected = "tool window"; return false; }

            string cls = GetWindowClass(hwnd);
            if (IgnoredClasses.Contains(cls)) { rejected = $"ignored class {cls}"; return false; }

            GetWindowThreadProcessId(hwnd, out uint pid);
            if (pid == (uint)Environment.ProcessId) { rejected = "own window"; return false; }
            if (IsElevatedProcessWeCannotMove(pid)) { rejected = "elevated process"; return false; }

            var title = new StringBuilder(256);
            GetWindowText(hwnd, title, title.Capacity);
            if (title.Length == 0) { rejected = "empty title"; return false; }

            return true;
        }

        private void RestoreSavedLayout(WorkspaceState saved)
        {
            _workspace.BeginRestore(saved);

            int restored = 0;
            foreach (var entry in saved.Windows)
            {
                nint hwnd = (nint)entry.Hwnd;
                if (!entry.StillValid() || !IsTileable(hwnd, allowMinimized: true, out _) || !_seen.Add(hwnd)) continue;
                _workspace.RestoreWindow(entry.Monitor, entry.Workspace, hwnd);
                restored++;
            }
            foreach (var entry in saved.Floating)
            {
                nint hwnd = (nint)entry.Hwnd;
                if (!entry.StillValid() || !IsTileable(hwnd, allowMinimized: true, out _) || !_seen.Add(hwnd)) continue;
                _workspace.RestoreFloating(hwnd);
                restored++;
            }

            _workspace.EndRestore();
            Log.Info($"Restored {restored} window(s) from the saved layout (active workspace {saved.Active})");
        }

        private void AddExistingWindows()
        {
            var saved = StateStore.Load();
            if (saved != null) RestoreSavedLayout(saved);

            var found = new List<IntPtr>();

            EnumWindows((hwnd, lParam) =>
            {
                if (IsTileable(hwnd, allowMinimized: false, rejected: out _)) found.Add(hwnd);
                return true;
            }, IntPtr.Zero);

            Log.Info($"AddExistingWindows: {found.Count} candidate window(s)");
            foreach (var hwnd in found)
            {
                if (!_seen.Add(hwnd)) continue;
                Log.Debug($"Adopting existing window {Describe.Window(hwnd)}");
                _workspace.AddToActiveWorkspace(hwnd, byLocation: true);
            }
        }

        /// <summary>Hooked only while VirtualBox is focused; reinstalled after it sets up its own hook so ours runs first.</summary>
        private void UpdateVmHook(IntPtr foreground)
        {
            if (!VmKeyboardHook.IsVirtualBox(foreground))
            {
                _vmHook.Uninstall();
                return;
            }

            _vmHook.Install();
            _ = Task.Delay(300).ContinueWith(_ =>
            {
                if (IsHandleCreated && !IsDisposed)
                    BeginInvoke(() => { if (VmKeyboardHook.IsVirtualBox(GetForegroundWindow())) _vmHook.Install(); });
            });
        }

        void OnWinEvent(IntPtr hook, uint evt, IntPtr hwnd, int idObject, int idChild, uint threadId, uint timeMs)
        {
            if (idObject != OBJID_WINDOW || idChild != CHILDID_SELF || hwnd == IntPtr.Zero) return;

            using var busy = Watchdog.Enter($"window event 0x{evt:X} for hwnd=0x{hwnd:X}");

            if (evt == EVENT_SYSTEM_FOREGROUND)
            {
                UpdateVmHook(hwnd);
                _workspace.NotifyForegroundChanged(hwnd);
                return;
            }

            if (evt == EVENT_OBJECT_DESTROY || evt == EVENT_OBJECT_HIDE)
            {
                if (_seen.Remove(hwnd))
                {
                    Log.Debug($"{(evt == EVENT_OBJECT_DESTROY ? "DESTROY" : "HIDE")} event for tracked hwnd=0x{hwnd:X} (tracked now: {_seen.Count})");
                    _workspace.Remove(hwnd);
                }
                return;
            }

            if (!IsTileable(hwnd, allowMinimized: true, out string? rejected))
            {
                if (rejected is not (null or "not top-level" or "has an owner" or "not visible") && !_seen.Contains(hwnd))
                    Log.Debug($"Ignoring SHOW for hwnd=0x{hwnd:X}: {rejected}");
                return;
            }

            string cls = GetWindowClass(hwnd);
            if (!_seen.Add(hwnd)) return;

            GetWindowThreadProcessId(hwnd, out uint pid);
            string procName;
            try { procName = Process.GetProcessById((int)pid).ProcessName; }
            catch (Exception ex)
            {
                Log.Debug($"Ignoring hwnd=0x{hwnd:X}: process {pid} lookup failed ({ex.GetType().Name})");
                return;
            }

            var title = new StringBuilder(256);
            GetWindowText(hwnd, title, title.Capacity);
            Log.Info($"New window: {procName} (PID {pid}) hwnd=0x{hwnd:X} class={cls} title=\"{title}\" (tracked now: {_seen.Count})");

            _workspace.AddToActiveWorkspace(hwnd);
        }

        private void addProcButton_Click(object sender, EventArgs e)
        {
            using var form = new AddProcHK();
            if (form.ShowDialog(this) == DialogResult.OK)
                ReloadKeybinds();
        }

        private void addCmdButton_Click(object sender, EventArgs e)
        {
            using var form = new AddCmdHK();
            if (form.ShowDialog(this) == DialogResult.OK)
                ReloadKeybinds();
        }

        private void hotkeysButton_Click(object sender, EventArgs e)
        {
            using var form = new HotkeyList();
            form.ShowDialog(this);
            if (form.Changed)
                ReloadKeybinds();
        }

        private void settingsButton_Click(object sender, EventArgs e)
        {
            using var form = new SettingsForm(_updates);
            if (form.ShowDialog(this) == DialogResult.OK)
                _workspace.ApplySettings();

            if (form.RelaunchRequested)
            {
                Log.Info("Run level changed; relaunching through the startup task");
                StartupRegistration.RelaunchViaTask();
                _reallyExit = true;
                Close();
                return;
            }

            if (form.ExitRequested)
            {
                Log.Info("Exit requested from settings; restoring windows");
                _workspace.ShowInactiveWindows();
                _reallyExit = true;
                Close();
            }
        }
    }
}
