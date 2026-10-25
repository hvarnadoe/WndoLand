using System.Runtime.InteropServices;
using System.Windows.Forms;
using static WndoLand.Win32;

namespace WndoLand
{
    /// <summary>
    /// Every monitor owns its own set of workspaces and its own active workspace; switching workspaces only
    /// affects the active monitor, and windows tile within the monitor their workspace belongs to.
    /// </summary>
    internal class Workspaces
    {
        private const int WorkspaceCount = 10;

        // A focus change this soon after one of our own actions is a side-effect of hiding/showing windows, not the user
        private const long OwnActionQuietMs = 800;

        private sealed class MonitorSpace
        {
            public MonitorSpace(string id)
            {
                Id = id;
                Spaces = Enumerable.Range(0, WorkspaceCount).Select(_ => new Workspace(() => Bounds)).ToArray();
            }

            public string Id { get; }
            public Workspace[] Spaces { get; }
            public int ActiveNum { get; set; } = 1;
            public Workspace Active => Spaces[ActiveNum - 1];

            // Falls back to the primary screen if this monitor has been unplugged
            public Rectangle Bounds => (Screen.AllScreens.FirstOrDefault(s => s.DeviceName == Id) ?? Screen.PrimaryScreen!).Bounds;
        }

        private readonly List<MonitorSpace> _monitors = new();
        private MonitorSpace _activeMonitor;
        private Action<nint>? _pruned;
        private long _ownActionAt;

        private readonly HashSet<nint> _floating = new();

        public Workspaces()
        {
            SyncMonitors();
            _activeMonitor = Primary;
        }

        private MonitorSpace Primary => _monitors.FirstOrDefault(m => m.Id == Screen.PrimaryScreen?.DeviceName) ?? _monitors[0];

        private Workspace Active => _activeMonitor.Active;

        private int activeWorkSpace => _activeMonitor.ActiveNum;

        private IEnumerable<Workspace> AllWorkspaces => _monitors.SelectMany(m => m.Spaces);

        private (MonitorSpace Monitor, Workspace Workspace)? Locate(nint hwnd)
        {
            foreach (var m in _monitors)
                foreach (var ws in m.Spaces)
                    if (ws.Contains(hwnd)) return (m, ws);
            return null;
        }

        private MonitorSpace MonitorUnder(nint hwnd)
        {
            var screen = Screen.FromHandle(hwnd);
            return _monitors.FirstOrDefault(m => m.Id == screen.DeviceName) ?? _activeMonitor;
        }

        private void NoteOwnAction() => _ownActionAt = Environment.TickCount64;

        /// <summary>Adds monitors that appeared and moves the windows of ones that disappeared onto the primary monitor.</summary>
        private void SyncMonitors()
        {
            var screens = Screen.AllScreens.OrderBy(s => s.Bounds.Left).ThenBy(s => s.Bounds.Top).ToList();

            foreach (var s in screens)
            {
                if (_monitors.Any(m => m.Id == s.DeviceName)) continue;
                Log.Info($"Monitor {s.DeviceName} {s.Bounds} gets its own workspaces");
                var space = new MonitorSpace(s.DeviceName);
                foreach (var ws in space.Spaces) ws.Pruned = _pruned;
                _monitors.Add(space);
            }

            foreach (var gone in _monitors.Where(m => screens.All(s => s.DeviceName != m.Id)).ToList())
            {
                Log.Warn($"Monitor {gone.Id} is gone; moving its windows to the primary monitor");
                _monitors.Remove(gone);
                var primary = Primary;
                for (int i = 0; i < WorkspaceCount; i++)
                    foreach (var hwnd in gone.Spaces[i].Hwnds)
                    {
                        if (primary.Spaces[i] == primary.Active) primary.Spaces[i].AddWithoutTiling(hwnd);
                        else primary.Spaces[i].AddHidden(hwnd);
                    }
                if (_activeMonitor == gone) _activeMonitor = primary;
            }

            // Left to right, so "next monitor" and the saved layout agree with the physical arrangement
            _monitors.Sort((a, b) =>
                screens.FindIndex(s => s.DeviceName == a.Id).CompareTo(screens.FindIndex(s => s.DeviceName == b.Id)));
        }

        public void OnDisplaySettingsChanged()
        {
            SyncMonitors();
            if (!_monitors.Contains(_activeMonitor)) _activeMonitor = Primary;
            foreach (var m in _monitors) m.Active.Tile();
            Changed?.Invoke();
        }

        /// <summary>Follows the user's focus onto another monitor (clicking into a window there makes that monitor active).</summary>
        public void NotifyForegroundChanged(nint hwnd)
        {
            if (Environment.TickCount64 - _ownActionAt < OwnActionQuietMs) return;

            nint target = IsKnown(hwnd) ? hwnd : GetAncestor(hwnd, GA_ROOTOWNER);
            var found = Locate(target);
            if (found == null)
            {
                if (!_floating.Contains(target)) return;
                var onScreen = MonitorUnder(target);
                if (onScreen != _activeMonitor)
                {
                    Log.Debug($"Focus moved to a floating window on {onScreen.Id}; it is now the active monitor");
                    _activeMonitor = onScreen;
                }
                return;
            }

            var (monitor, workspace) = found.Value;
            // A window on a hidden workspace got focus from outside (Alt+Tab, taskbar), so follow it there
            if (workspace != monitor.Active)
            {
                int num = Array.IndexOf(monitor.Spaces, workspace) + 1;
                Log.Info($"Focus went to hwnd=0x{target:X} on {monitor.Id} workspace {num}; switching to it");
                _activeMonitor = monitor;
                SetActiveWorkspace(num);
                workspace.LastFocus = target;
                if (!SetForegroundWindow(target))
                    Log.Warn($"SetForegroundWindow refused for hwnd=0x{target:X} (foreground is now 0x{GetForegroundWindow():X})");
                return;
            }

            workspace.LastFocus = target;
            if (monitor != _activeMonitor)
            {
                Log.Debug($"Focus moved to {monitor.Id}; it is now the active monitor");
                _activeMonitor = monitor;
            }
        }

        public void SetActiveWorkspace(int workspaceNum)
        {
            NoteOwnAction();
            var monitor = _activeMonitor;
            Log.Info($"Switching workspace {monitor.ActiveNum} -> {workspaceNum} on {monitor.Id}");
            var old = monitor.Active;
            var next = monitor.Spaces[workspaceNum - 1];
            next.Activate();
            if (old != next) old.Deactivate();
            monitor.ActiveNum = workspaceNum;
            WorkspaceChanged?.Invoke(workspaceNum, monitor.Bounds);
            Changed?.Invoke();
        }

        public event Action<int, Rectangle>? WorkspaceChanged;

        public event Action? Changed;

        public Action<nint>? WindowPruned
        {
            set
            {
                _pruned = value;
                foreach (var ws in AllWorkspaces) ws.Pruned = value;
            }
        }

        private bool IsKnown(nint hwnd) => _floating.Contains(hwnd) || AllWorkspaces.Any(w => w.Contains(hwnd));

        private nint ForegroundTarget()
        {
            nint fg = GetForegroundWindow();
            return IsKnown(fg) ? fg : GetAncestor(fg, GA_ROOTOWNER);
        }

        // The workspace the window is tiled on, falling back to the active one
        private Workspace WorkspaceFor(nint hwnd) => Locate(hwnd)?.Workspace ?? Active;

        public void MoveActiveWindowToWorkspace(int workspaceNum)
        {
            nint hwnd = GetForegroundWindow();
            nint owner = GetAncestor(hwnd, GA_ROOTOWNER);
            nint target = AllWorkspaces.Any(w => w.Contains(hwnd)) ? hwnd : owner;

            var found = Locate(target);
            if (found == null)
            {
                Log.Warn($"Can't send foreground window to workspace {workspaceNum}: not tracked ({Describe.Window(hwnd)})");
                return;
            }

            var (monitor, source) = found.Value;
            if (monitor.Spaces[workspaceNum - 1] == source)
            {
                Log.Debug($"hwnd=0x{target:X} is already on workspace {workspaceNum}");
                return;
            }

            Log.Info($"Moving hwnd=0x{target:X} to workspace {workspaceNum} on {monitor.Id}: {Describe.Window(target)}");
            // Remove from the source first so Deactivate doesn't minimize it; SetActiveWorkspace then shows and tiles it
            source.Remove(target, retile: false);
            monitor.Spaces[workspaceNum - 1].AddWithoutTiling(target);
            _activeMonitor = monitor;
            SetActiveWorkspace(workspaceNum);
        }

        public void CloseActiveWindow()
        {
            nint? target = ClosableTarget();
            if (target == null) return;

            Log.Info($"Closing {Describe.Window(target.Value)}");
            if (!PostMessage(target.Value, WM_CLOSE, 0, 0))
                Log.Error($"PostMessage(WM_CLOSE) failed for hwnd=0x{target.Value:X}, Win32 error {Marshal.GetLastWin32Error()}");
        }

        private nint? ClosableTarget()
        {
            nint target = ForegroundTarget();
            if (target == 0)
            {
                Log.Debug("Close: no foreground window");
                return null;
            }

            var cls = new System.Text.StringBuilder(256);
            GetClassName(target, cls, cls.Capacity);
            GetWindowThreadProcessId(target, out uint pid);
            if (MainForm.IgnoredClasses.Contains(cls.ToString()) || pid == (uint)Environment.ProcessId)
            {
                Log.Warn($"Won't close a shell or WndoLand window: {Describe.Window(target)}");
                return null;
            }
            return target;
        }

        private static readonly string[] ProtectedProcesses = { "explorer", "ApplicationFrameHost", "dwm", "winlogon", "csrss" };

        public void ForceCloseActiveWindow()
        {
            nint? found = ClosableTarget();
            if (found == null) return;
            nint target = found.Value;

            GetWindowThreadProcessId(target, out uint pid);
            try
            {
                using var process = System.Diagnostics.Process.GetProcessById((int)pid);
                if (pid == (uint)Environment.ProcessId || ProtectedProcesses.Contains(process.ProcessName, StringComparer.OrdinalIgnoreCase))
                {
                    Log.Warn($"Won't end {process.ProcessName} (pid {pid}); asking the window to close normally instead");
                    CloseActiveWindow();
                    return;
                }

                Log.Info($"Force closing {process.ProcessName} (pid {pid}): {Describe.Window(target)}");
                process.Kill(entireProcessTree: true);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Couldn't end the process (pid {pid}) behind hwnd=0x{target:X}");
            }
        }

        public void FocusInDirection(Direction dir)
        {
            nint fg = GetForegroundWindow();
            nint current = AllWorkspaces.Any(w => w.Contains(fg)) ? fg : GetAncestor(fg, GA_ROOTOWNER);
            var ws = WorkspaceFor(current);

            var next = ws.FindNeighbour(current, dir);
            if (next == null)
            {
                Log.Debug($"Focus {dir}: no window {dir} of hwnd=0x{current:X} (workspace {activeWorkSpace})");
                return;
            }

            Log.Info($"Focus {dir}: hwnd=0x{current:X} -> {Describe.Window(next.Value)}");
            if (IsIconic(next.Value)) ShowWindow(next.Value, NCMDSHOW.SW_RESTORE);
            if (!SetForegroundWindow(next.Value))
                Log.Warn($"SetForegroundWindow refused for hwnd=0x{next.Value:X} (foreground is now 0x{GetForegroundWindow():X})");
        }

        public void SwapInDirection(Direction dir)
        {
            nint fg = GetForegroundWindow();
            nint current = AllWorkspaces.Any(w => w.Contains(fg)) ? fg : GetAncestor(fg, GA_ROOTOWNER);
            var ws = WorkspaceFor(current);

            if (ws.SwapWithNeighbour(current, dir))
            {
                Log.Info($"Swap {dir}: hwnd=0x{current:X}");
                Changed?.Invoke();
            }
            else
                Log.Debug($"Swap {dir}: nothing {dir} of hwnd=0x{current:X} (workspace {activeWorkSpace})");
        }

        public void ToggleFloat()
        {
            nint target = ForegroundTarget();

            if (_floating.Remove(target))
            {
                Log.Info($"Tiling floating window again: {Describe.Window(target)}");
                MonitorUnder(target).Active.Add(target);
                Changed?.Invoke();
                return;
            }

            var found = Locate(target);
            if (found == null)
            {
                Log.Warn($"Can't float foreground window: not tracked ({Describe.Window(target)})");
                return;
            }

            Log.Info($"Floating window: {Describe.Window(target)}");
            found.Value.Workspace.Remove(target, retile: true);
            _floating.Add(target);
            Borders.Restore(target);
            CentreFloating(target, found.Value.Monitor.Bounds);
            Changed?.Invoke();
        }

        private static void CentreFloating(nint hwnd, Rectangle monitor)
        {
            if (IsZoomed(hwnd)) ShowWindow(hwnd, NCMDSHOW.SW_RESTORE);

            int w = monitor.Width * 7 / 10;
            int h = monitor.Height * 7 / 10;
            int x = monitor.Left + (monitor.Width - w) / 2;
            int y = monitor.Top + (monitor.Height - h) / 2;
            MoveWindow(hwnd, x, y, w, h, true);
        }

        public void ToggleFullscreen()
        {
            nint target = ForegroundTarget();
            var found = Locate(target);
            if (found == null)
            {
                Log.Warn($"Can't fullscreen foreground window: not tiled ({Describe.Window(target)})");
                return;
            }
            Log.Info($"Toggle fullscreen: hwnd=0x{target:X}");
            found.Value.Workspace.ToggleFullscreen(target);
        }

        private static Point Centre(Rectangle r) => new(r.Left + r.Width / 2, r.Top + r.Height / 2);

        /// <summary>The monitor lying in the given direction, preferring the one most squarely in line with the current monitor.</summary>
        private MonitorSpace? NeighbourMonitor(MonitorSpace from, Direction dir)
        {
            var c = Centre(from.Bounds);
            bool horizontal = dir is Direction.Left or Direction.Right;
            MonitorSpace? best = null;
            long bestScore = long.MaxValue;

            foreach (var m in _monitors)
            {
                if (m == from) continue;
                var o = Centre(m.Bounds);
                int dx = o.X - c.X, dy = o.Y - c.Y;

                bool ahead = dir switch
                {
                    Direction.Right => dx > 0,
                    Direction.Left => dx < 0,
                    Direction.Down => dy > 0,
                    _ => dy < 0
                };
                if (!ahead) continue;

                long score = horizontal ? Math.Abs(dx) + 2L * Math.Abs(dy) : Math.Abs(dy) + 2L * Math.Abs(dx);
                if (score < bestScore) { best = m; bestScore = score; }
            }
            return best;
        }

        /// <summary>Makes the monitor in the given direction active and focuses a window on it.</summary>
        public void FocusMonitor(Direction dir)
        {
            var dest = NeighbourMonitor(_activeMonitor, dir);
            if (dest == null)
            {
                Log.Debug($"Focus monitor {dir}: no monitor {dir} of {_activeMonitor.Id}");
                return;
            }

            NoteOwnAction();
            Log.Info($"Focus monitor {dir}: {_activeMonitor.Id} -> {dest.Id}");
            _activeMonitor = dest;

            var ws = dest.Active;
            nint focus = ws.Contains(ws.LastFocus) ? ws.LastFocus : ws.Hwnds.FirstOrDefault();
            if (focus != 0)
            {
                if (IsIconic(focus)) ShowWindow(focus, NCMDSHOW.SW_RESTORE);
                if (!SetForegroundWindow(focus))
                    Log.Warn($"SetForegroundWindow refused for hwnd=0x{focus:X} (foreground is now 0x{GetForegroundWindow():X})");
            }
            else
            {
                // Nothing to focus; park the cursor there so it's clear where new windows will open
                Cursor.Position = Centre(dest.Bounds);
            }

            WorkspaceChanged?.Invoke(dest.ActiveNum, dest.Bounds);
            Changed?.Invoke();
        }

        /// <summary>Sends the foreground window to whatever workspace is currently active on the monitor in the given direction.</summary>
        public void MoveActiveWindowToMonitor(Direction dir)
        {
            nint target = ForegroundTarget();
            var current = Locate(target)?.Monitor ?? MonitorUnder(target);
            var dest = NeighbourMonitor(current, dir);
            if (dest == null)
            {
                Log.Debug($"Move window {dir}: no monitor {dir} of {current.Id}");
                return;
            }
            MoveWindowToMonitor(target, dest);
        }

        /// <summary>Cycles the foreground window to the next monitor, left to right.</summary>
        public void MoveActiveWindowToNextMonitor()
        {
            if (_monitors.Count < 2)
            {
                Log.Debug("MoveToNextMonitor: only one monitor");
                return;
            }

            nint target = ForegroundTarget();
            var current = Locate(target)?.Monitor ?? MonitorUnder(target);
            MoveWindowToMonitor(target, _monitors[(_monitors.IndexOf(current) + 1) % _monitors.Count]);
        }

        private void MoveWindowToMonitor(nint target, MonitorSpace dest)
        {
            NoteOwnAction();
            var found = Locate(target);
            if (found == null)
            {
                if (!_floating.Contains(target))
                {
                    Log.Warn($"Can't move foreground window to another monitor: not tracked ({Describe.Window(target)})");
                    return;
                }
                Log.Info($"Moving floating hwnd=0x{target:X} to {dest.Id}");
                CentreFloating(target, dest.Bounds);
                _activeMonitor = dest;
                Changed?.Invoke();
                return;
            }

            if (found.Value.Monitor == dest) return;

            Log.Info($"Moving hwnd=0x{target:X} from {found.Value.Monitor.Id} to workspace {dest.ActiveNum} on {dest.Id}: {Describe.Window(target)}");
            found.Value.Workspace.Remove(target, retile: true);
            dest.Active.Add(target);
            dest.Active.LastFocus = target;
            _activeMonitor = dest;
            SetForegroundWindow(target);

            WorkspaceChanged?.Invoke(dest.ActiveNum, dest.Bounds);
            Changed?.Invoke();
        }

        public void AddToWorkspace(int workspaceNum, nint hwnd)
        {
            Log.Debug($"Add hwnd=0x{hwnd:X} to workspace {workspaceNum} on {_activeMonitor.Id}");
            _activeMonitor.Spaces[workspaceNum - 1].Add(hwnd);
            Changed?.Invoke();
        }

        /// <summary>
        /// New windows join the active monitor (so launching onto an empty monitor works); windows that were already
        /// open at startup stay on the monitor they are physically on.
        /// </summary>
        public void AddToActiveWorkspace(nint hwnd, bool byLocation = false)
        {
            var rule = Rules.Match(hwnd);

            if (rule?.Action == "float")
            {
                Log.Debug($"Floating hwnd=0x{hwnd:X} (rule)");
                _floating.Add(hwnd);
                Changed?.Invoke();
                return;
            }

            var monitor = byLocation ? MonitorUnder(hwnd) : _activeMonitor;
            int target = rule?.Action == "workspace" && rule.Workspace is >= 1 and <= WorkspaceCount ? rule.Workspace : monitor.ActiveNum;
            Log.Debug($"Add hwnd=0x{hwnd:X} to workspace {target} on {monitor.Id} (active={monitor.ActiveNum})");
            if (target == monitor.ActiveNum)
                monitor.Spaces[target - 1].Add(hwnd);
            else
                monitor.Spaces[target - 1].AddHidden(hwnd);
            Changed?.Invoke();
        }

        public void Remove(nint hwnd)
        {
            Log.Debug($"Remove hwnd=0x{hwnd:X} from all workspaces");
            _floating.Remove(hwnd);
            Borders.Restore(hwnd);
            foreach (var m in _monitors)
                foreach (var ws in m.Spaces)
                    ws.Remove(hwnd, retile: ws == m.Active);
            Changed?.Invoke();
        }

        public int GetNumWindows()
        {
            return Active.GetNumWindows();
        }

        public void ApplySettings()
        {
            if (Settings.Current.StripTitleBars)
                foreach (var ws in AllWorkspaces)
                    foreach (var hwnd in ws.Hwnds)
                        Borders.Strip(hwnd);
            else
                Borders.RestoreAll();

            foreach (var m in _monitors) m.Active.Tile();
        }

        // Windows on other workspaces are parked minimized; bring them back so nothing is left stranded on exit
        public void ShowInactiveWindows()
        {
            foreach (var m in _monitors)
                foreach (var ws in m.Spaces)
                    if (ws != m.Active) ws.Activate(show: true);
        }

        public WorkspaceState Snapshot()
        {
            var state = new WorkspaceState { Active = Primary.ActiveNum, ActiveMonitor = _activeMonitor.Id };
            foreach (var m in _monitors)
            {
                state.ActiveWorkspaces[m.Id] = m.ActiveNum;
                for (int i = 0; i < m.Spaces.Length; i++)
                    foreach (var hwnd in m.Spaces[i].Hwnds)
                        state.Windows.Add(WindowEntry.From(hwnd, i + 1, m.Id));
            }
            foreach (var hwnd in _floating)
                state.Floating.Add(WindowEntry.From(hwnd, 0));
            return state;
        }

        public void BeginRestore(WorkspaceState saved)
        {
            foreach (var m in _monitors)
            {
                int num = saved.ActiveWorkspaces.TryGetValue(m.Id, out int n) ? n
                        : m == Primary ? saved.Active
                        : 1;
                m.ActiveNum = Math.Clamp(num, 1, WorkspaceCount);
            }
            _activeMonitor = _monitors.FirstOrDefault(m => m.Id == saved.ActiveMonitor) ?? Primary;
        }

        public void RestoreWindow(string? monitorId, int workspaceNum, nint hwnd)
        {
            // Layouts saved before monitors had their own workspaces carry no monitor; they all lived on the primary one
            var monitor = _monitors.FirstOrDefault(m => m.Id == monitorId) ?? Primary;
            var ws = monitor.Spaces[Math.Clamp(workspaceNum, 1, WorkspaceCount) - 1];
            if (ws == monitor.Active) ws.AddWithoutTiling(hwnd);
            else ws.AddHidden(hwnd);
        }

        public void RestoreFloating(nint hwnd) => _floating.Add(hwnd);

        public void EndRestore()
        {
            foreach (var m in _monitors) m.Active.Tile();
        }

        private class Workspace(Func<Rectangle> area)
        {
            private List<Window> windows = new List<Window>();

            public Action<nint>? Pruned { get; set; }

            // The window that last had focus here, so returning to this monitor lands back on it
            public nint LastFocus { get; set; }

            private nint _fullscreen;

            public IEnumerable<nint> Hwnds => windows.Select(w => w.Hwnd).ToList();

            public void Activate(bool show)
            {
                if (show)
                {
                    RunOnAll("show", w => w.Show());
                    return;
                }
                Activate();
            }

            public void Activate()
            {
                Log.Debug($"Activate: showing {windows.Count} window(s)");
                RunOnAll("show", w => w.Show());
                Tile();
            }

            public void Deactivate()
            {
                Log.Debug($"Deactivate: hiding {windows.Count} window(s)");
                RunOnAll("hide", w => w.Hide());
            }

            // ShowWindow waits on each app's message loop; give up on a window that never answers rather than freeze the switch
            private void RunOnAll(string what, Action<Window> action)
            {
                var tasks = windows.Select(w => Task.Run(() => action(w))).ToArray();
                if (Task.WaitAll(tasks, SwitchTimeoutMs)) return;

                for (int i = 0; i < tasks.Length; i++)
                    if (!tasks[i].IsCompleted)
                        Log.Warn($"Window did not {what} within {SwitchTimeoutMs} ms, moving on without it: {Describe.Window(windows[i].Hwnd)}");
            }

            private const int SwitchTimeoutMs = 1500;

            public void Add(nint hwnd)
            {
                Borders.Strip(hwnd);
                Borders.DisableTransitions(hwnd);
                windows.Add(new Window(hwnd));
                Tile();
            }

            public void AddHidden(nint hwnd)
            {
                AddWithoutTiling(hwnd);
                windows[^1].Hide();
            }

            public nint? FindNeighbour(nint from, Direction dir)
            {
                var cur = windows.FirstOrDefault(w => w.Hwnd == from);
                if (cur == null || cur.Bounds.IsEmpty)
                {
                    Log.Debug($"FindNeighbour: hwnd=0x{from:X} is not a placed window on this workspace");
                    return null;
                }

                var c = cur.Bounds;
                bool horizontal = dir is Direction.Left or Direction.Right;
                nint? best = null;
                int bestGap = int.MaxValue, bestOverlap = 0, bestCentreDist = int.MaxValue;

                foreach (var w in windows)
                {
                    if (w == cur || w.Bounds.IsEmpty) continue;
                    var r = w.Bounds;

                    bool onSide = dir switch
                    {
                        Direction.Right => r.Left >= c.Right,
                        Direction.Left => r.Right <= c.Left,
                        Direction.Down => r.Top >= c.Bottom,
                        _ => r.Bottom <= c.Top
                    };
                    if (!onSide) continue;

                    int overlap = horizontal
                        ? Math.Min(c.Bottom, r.Bottom) - Math.Max(c.Top, r.Top)
                        : Math.Min(c.Right, r.Right) - Math.Max(c.Left, r.Left);
                    if (overlap <= 0) continue;

                    int gap = dir switch
                    {
                        Direction.Right => r.Left - c.Right,
                        Direction.Left => c.Left - r.Right,
                        Direction.Down => r.Top - c.Bottom,
                        _ => c.Top - r.Bottom
                    };
                    int centreDist = horizontal
                        ? Math.Abs((r.Top + r.Bottom) / 2 - (c.Top + c.Bottom) / 2)
                        : Math.Abs((r.Left + r.Right) / 2 - (c.Left + c.Right) / 2);

                    if (best == null || gap < bestGap || (gap == bestGap && (overlap > bestOverlap ||
                        (overlap == bestOverlap && centreDist < bestCentreDist))))
                    {
                        best = w.Hwnd; bestGap = gap; bestOverlap = overlap; bestCentreDist = centreDist;
                    }
                }
                return best;
            }

            public bool SwapWithNeighbour(nint from, Direction dir)
            {
                var next = FindNeighbour(from, dir);
                if (next == null) return false;

                int a = windows.FindIndex(w => w.Hwnd == from);
                int b = windows.FindIndex(w => w.Hwnd == next.Value);
                (windows[a], windows[b]) = (windows[b], windows[a]);
                Tile();
                return true;
            }

            public void ToggleFullscreen(nint hwnd)
            {
                _fullscreen = _fullscreen == hwnd ? 0 : hwnd;
                Tile();
                if (_fullscreen != 0) SetForegroundWindow(hwnd);
            }

            public bool Contains(nint hwnd) => windows.Any(w => w.Hwnd == hwnd);

            public void AddWithoutTiling(nint hwnd)
            {
                Borders.Strip(hwnd);
                Borders.DisableTransitions(hwnd);
                windows.Add(new Window(hwnd));
            }

            public void Remove(nint hwnd, bool retile)
            {
                if (_fullscreen == hwnd) _fullscreen = 0;
                if (windows.RemoveAll(w => w.Hwnd == hwnd) > 0 && retile)
                    Tile();
            }

            private readonly record struct Cell(double X, double Y, double W, double H);

            // Halves cells repeatedly, alternating vertical and horizontal splits, so adding a window only splits one cell
            private static List<Cell> ComputeLayout(int count)
            {
                var cells = new List<Cell> { new(0, 0, 1, 1) };
                bool vertical = true;
                while (cells.Count < count)
                {
                    var order = Enumerable.Range(0, cells.Count)
                        .OrderByDescending(i => cells[i].X)
                        .ThenByDescending(i => cells[i].Y)
                        .ToList();

                    foreach (int i in order)
                    {
                        if (cells.Count == count) break;
                        var c = cells[i];
                        if (vertical)
                        {
                            cells[i] = c with { W = c.W / 2 };
                            cells.Add(c with { X = c.X + c.W / 2, W = c.W / 2 });
                        }
                        else
                        {
                            cells[i] = c with { H = c.H / 2 };
                            cells.Add(c with { Y = c.Y + c.H / 2, H = c.H / 2 });
                        }
                    }
                    vertical = !vertical;
                }
                return cells;
            }

            public void Tile()
            {
                // Windows we minimized ourselves stay WS_VISIBLE, so only destroyed or app-hidden windows are pruned
                int before = windows.Count;
                windows.RemoveAll(w =>
                {
                    string? reason = !IsWindow(w.Hwnd) ? "no longer exists" : !IsWindowVisible(w.Hwnd) ? "hidden by its app" : null;
                    if (reason == null) return false;
                    Log.Warn($"Pruned ghost window ({reason}): {Describe.Window(w.Hwnd)}");
                    if (w.Hwnd == _fullscreen) _fullscreen = 0;
                    Pruned?.Invoke(w.Hwnd);
                    return true;
                });

                if (windows.Count == 0)
                {
                    Log.Debug("Tile: no windows left, nothing to place");
                    return;
                }

                int gap = Math.Max(0, Settings.Current.Gap);
                var bounds = area();
                Log.Debug($"Tile: {windows.Count} window(s) ({before - windows.Count} pruned), gap {gap}, monitor {Describe.Rect(bounds.Left, bounds.Top, bounds.Right, bounds.Bottom)}");

                int totalW = bounds.Width;
                int totalH = bounds.Height;
                var layout = ComputeLayout(windows.Count);

                for (int i = 0; i < layout.Count; i++)
                {
                    var c = layout[i];
                    // Round edges rather than sizes so neighbouring cells never leave a gap
                    int left = bounds.Left + (int)Math.Round(c.X * totalW);
                    int top = bounds.Top + (int)Math.Round(c.Y * totalH);
                    int right = bounds.Left + (int)Math.Round((c.X + c.W) * totalW);
                    int bottom = bounds.Top + (int)Math.Round((c.Y + c.H) * totalH);
                    windows[i].Bounds = Rectangle.FromLTRB(left, top, right, bottom);

                    var place = windows[i].Hwnd == _fullscreen
                        ? bounds
                        : Rectangle.FromLTRB(left + gap / 2, top + gap / 2, right - gap / 2, bottom - gap / 2);
                    Place(windows[i].Hwnd, place, i);
                }
            }

            private static readonly System.Collections.Concurrent.ConcurrentDictionary<nint, int> _placeGen = new();

            private static void Place(nint hwnd, Rectangle r, int cell)
            {
                // A maximized window ignores MoveWindow's size (and apps like Chrome re-maximize), so un-maximize first
                if (IsZoomed(hwnd))
                {
                    Log.Debug($"  cell {cell}: hwnd=0x{hwnd:X} is maximized, restoring before placing");
                    ShowWindow(hwnd, NCMDSHOW.SW_RESTORE);
                }

                Rectangle target = r;
                if (!SetOuterFor(hwnd, target, cell, "placed")) return;

                // The invisible-border offsets can change after we measure them (style strip landing late, an app
                // re-laying itself out), leaving the window oversized; re-measure shortly after and correct it
                int gen = _placeGen.AddOrUpdate(hwnd, 1, (_, g) => g + 1);
                _ = Task.Run(async () =>
                {
                    for (int attempt = 1; attempt <= 3; attempt++)
                    {
                        await Task.Delay(150);
                        if (_placeGen.GetValueOrDefault(hwnd) != gen || !IsWindow(hwnd) || IsIconic(hwnd) || !IsWindowVisible(hwnd)) return;
                        if (DwmGetWindowAttribute(hwnd, DWMWA_EXTENDED_FRAME_BOUNDS, out RECT v, Marshal.SizeOf<RECT>()) != 0) return;
                        if (Math.Abs(v.left - target.Left) <= 1 && Math.Abs(v.top - target.Top) <= 1
                            && Math.Abs(v.right - target.Right) <= 1 && Math.Abs(v.bottom - target.Bottom) <= 1) return;

                        Log.Warn($"Window missed its cell (attempt {attempt}): wanted {Describe.Rect(target.Left, target.Top, target.Right, target.Bottom)}, " +
                                 $"visible is {Describe.Rect(v)}; correcting. {Describe.Window(hwnd)}");
                        SetOuterFor(hwnd, target, cell, "corrected");
                    }
                });
            }

            // Converts the wanted visible rectangle to the outer rectangle SetWindowPos needs, using the window's current invisible borders
            private static bool SetOuterFor(nint hwnd, Rectangle visibleTarget, int cell, string what)
            {
                var r = visibleTarget;
                if (GetWindowRect(hwnd, out RECT outer)
                    && DwmGetWindowAttribute(hwnd, DWMWA_EXTENDED_FRAME_BOUNDS, out RECT visible, Marshal.SizeOf<RECT>()) == 0)
                {
                    r = Rectangle.FromLTRB(
                        r.Left - (visible.left - outer.left),
                        r.Top - (visible.top - outer.top),
                        r.Right + (outer.right - visible.right),
                        r.Bottom + (outer.bottom - visible.bottom));
                }

                if (SetWindowPos(hwnd, IntPtr.Zero, r.Left, r.Top, r.Width, r.Height, SWP_NOZORDER | SWP_NOACTIVATE | SWP_ASYNCWINDOWPOS))
                {
                    Log.Debug($"  cell {cell}: hwnd=0x{hwnd:X} {what} -> {Describe.Rect(r.Left, r.Top, r.Right, r.Bottom)}");
                    return true;
                }
                Log.Warn($"MoveWindow failed for hwnd=0x{hwnd:X} Win32 error {Marshal.GetLastWin32Error()}");
                return false;
            }

            public int GetNumWindows()
            {
                return windows.Count;
            }

            private class Window(nint HWND)
            {
                public nint Hwnd = HWND;
                public Rectangle Bounds;

                public void Show()
                {
                    if (Log.IsDebugEnabled) Log.Debug($"Show {Describe.Window(Hwnd)}");
                    ShowWindow(Hwnd, NCMDSHOW.SW_RESTORE);
                }
                public void Hide()
                {
                    if (Log.IsDebugEnabled) Log.Debug($"Hide {Describe.Window(Hwnd)}");
                    ShowWindow(Hwnd, NCMDSHOW.SW_FORCEMINIMIZE);
                }
            }
        }
    }
}
