using System.Runtime.InteropServices;
using System.Windows.Forms;
using static WndoLand.Win32;

namespace WndoLand
{
    internal class Workspaces()
    {
        Workspace[] workSpaces =
        {
            new Workspace(),
            new Workspace(),
            new Workspace(),
            new Workspace(),
            new Workspace(),
            new Workspace(),
            new Workspace(),
            new Workspace(),
            new Workspace(),
            new Workspace()
        };
        int activeWorkSpace = 1;

        private readonly HashSet<nint> _floating = new();

        private Workspace Active => workSpaces[activeWorkSpace - 1];

        public void SetActiveWorkspace(int workspaceNum)
        {
            Log.Info($"Switching workspace {activeWorkSpace} -> {workspaceNum}");
            var old = workSpaces[activeWorkSpace - 1];
            workSpaces[workspaceNum - 1].Activate();
            if (old != workSpaces[workspaceNum - 1]) old.Deactivate();
            activeWorkSpace = workspaceNum;
            WorkspaceChanged?.Invoke(workspaceNum);
            Changed?.Invoke();
        }

        public event Action<int>? WorkspaceChanged;

        public event Action? Changed;

        public Action<nint>? WindowPruned
        {
            set { foreach (var ws in workSpaces) ws.Pruned = value; }
        }

        private bool IsKnown(nint hwnd) => _floating.Contains(hwnd) || workSpaces.Any(w => w.Contains(hwnd));

        private nint ForegroundTarget()
        {
            nint fg = GetForegroundWindow();
            return IsKnown(fg) ? fg : GetAncestor(fg, GA_ROOTOWNER);
        }

        public void MoveActiveWindowToWorkspace(int workspaceNum)
        {
            nint hwnd = GetForegroundWindow();
            nint owner = GetAncestor(hwnd, GA_ROOTOWNER);
            nint target = workSpaces.Any(w => w.Contains(hwnd)) ? hwnd : owner;

            int from = Array.FindIndex(workSpaces, w => w.Contains(target));
            if (from < 0)
            {
                Log.Warn($"Can't send foreground window to workspace {workspaceNum}: not tracked ({Describe.Window(hwnd)})");
                return;
            }
            if (from == workspaceNum - 1)
            {
                Log.Debug($"hwnd=0x{target:X} is already on workspace {workspaceNum}");
                return;
            }

            Log.Info($"Moving hwnd=0x{target:X} from workspace {from + 1} to {workspaceNum}: {Describe.Window(target)}");
            // Remove from the source first so Deactivate doesn't minimize it; SetActiveWorkspace then shows and tiles it
            workSpaces[from].Remove(target, retile: false);
            workSpaces[workspaceNum - 1].AddWithoutTiling(target);
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
            var ws = Active;
            nint current = ws.Contains(fg) ? fg : GetAncestor(fg, GA_ROOTOWNER);

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
            var ws = Active;
            nint current = ws.Contains(fg) ? fg : GetAncestor(fg, GA_ROOTOWNER);

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
                Active.Add(target);
                Changed?.Invoke();
                return;
            }

            if (!Active.Contains(target))
            {
                Log.Warn($"Can't float foreground window: not tracked on workspace {activeWorkSpace} ({Describe.Window(target)})");
                return;
            }

            Log.Info($"Floating window: {Describe.Window(target)}");
            Active.Remove(target, retile: true);
            _floating.Add(target);
            Borders.Restore(target);
            CentreFloating(target);
            Changed?.Invoke();
        }

        private static void CentreFloating(nint hwnd)
        {
            if (IsZoomed(hwnd)) ShowWindow(hwnd, NCMDSHOW.SW_RESTORE);
            var mi = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
            if (!GetMonitorInfo(MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST), ref mi)) return;

            int w = (mi.rcMonitor.right - mi.rcMonitor.left) * 7 / 10;
            int h = (mi.rcMonitor.bottom - mi.rcMonitor.top) * 7 / 10;
            int x = mi.rcMonitor.left + ((mi.rcMonitor.right - mi.rcMonitor.left) - w) / 2;
            int y = mi.rcMonitor.top + ((mi.rcMonitor.bottom - mi.rcMonitor.top) - h) / 2;
            MoveWindow(hwnd, x, y, w, h, true);
        }

        public void ToggleFullscreen()
        {
            nint target = ForegroundTarget();
            if (!Active.Contains(target))
            {
                Log.Warn($"Can't fullscreen foreground window: not tiled on workspace {activeWorkSpace} ({Describe.Window(target)})");
                return;
            }
            Log.Info($"Toggle fullscreen: hwnd=0x{target:X}");
            Active.ToggleFullscreen(target);
        }

        public void MoveActiveWindowToNextMonitor()
        {
            nint target = ForegroundTarget();
            if (!Active.Contains(target))
            {
                Log.Warn($"Can't move foreground window to another monitor: not tiled on workspace {activeWorkSpace} ({Describe.Window(target)})");
                return;
            }
            Active.MoveToNextMonitor(target);
        }

        public void AddToWorkspace(int workspaceNum, nint hwnd)
        {
            Log.Debug($"Add hwnd=0x{hwnd:X} to workspace {workspaceNum}");
            workSpaces[workspaceNum - 1].Add(hwnd);
            Changed?.Invoke();
        }

        public void AddToActiveWorkspace(nint hwnd)
        {
            var rule = Rules.Match(hwnd);

            if (rule?.Action == "float")
            {
                Log.Debug($"Floating hwnd=0x{hwnd:X} (rule)");
                _floating.Add(hwnd);
                Changed?.Invoke();
                return;
            }

            int target = rule?.Action == "workspace" && rule.Workspace is >= 1 and <= 10 ? rule.Workspace : activeWorkSpace;
            Log.Debug($"Add hwnd=0x{hwnd:X} to workspace {target} (active={activeWorkSpace})");
            if (target == activeWorkSpace)
                workSpaces[target - 1].Add(hwnd);
            else
                workSpaces[target - 1].AddHidden(hwnd);
            Changed?.Invoke();
        }

        public void Remove(nint hwnd)
        {
            Log.Debug($"Remove hwnd=0x{hwnd:X} from all workspaces (active={activeWorkSpace})");
            _floating.Remove(hwnd);
            Borders.Restore(hwnd);
            for (int i = 0; i < workSpaces.Length; i++)
                workSpaces[i].Remove(hwnd, retile: i == activeWorkSpace - 1);
            Changed?.Invoke();
        }

        public int GetNumWindows()
        {
            return Active.GetNumWindows();
        }

        public void ApplySettings()
        {
            if (Settings.Current.StripTitleBars)
                foreach (var ws in workSpaces)
                    foreach (var hwnd in ws.Hwnds)
                        Borders.Strip(hwnd);
            else
                Borders.RestoreAll();

            Active.Tile();
        }

        public WorkspaceState Snapshot()
        {
            var state = new WorkspaceState { Active = activeWorkSpace };
            for (int i = 0; i < workSpaces.Length; i++)
                foreach (var hwnd in workSpaces[i].Hwnds)
                    state.Windows.Add(WindowEntry.From(hwnd, i + 1));
            foreach (var hwnd in _floating)
                state.Floating.Add(WindowEntry.From(hwnd, 0));
            return state;
        }

        public void BeginRestore(int active) => activeWorkSpace = Math.Clamp(active, 1, workSpaces.Length);

        public void RestoreWindow(int workspaceNum, nint hwnd)
        {
            var ws = workSpaces[Math.Clamp(workspaceNum, 1, workSpaces.Length) - 1];
            if (ws == Active) ws.AddWithoutTiling(hwnd);
            else ws.AddHidden(hwnd);
        }

        public void RestoreFloating(nint hwnd) => _floating.Add(hwnd);

        public void EndRestore() => Active.Tile();

        private class Workspace()
        {
            private List<Window> windows = new List<Window>();

            public Action<nint>? Pruned { get; set; }

            private nint _fullscreen;

            public IEnumerable<nint> Hwnds => windows.Select(w => w.Hwnd).ToList();

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
                (windows[a].Monitor, windows[b].Monitor) = (windows[b].Monitor, windows[a].Monitor);
                Tile();
                return true;
            }

            public void ToggleFullscreen(nint hwnd)
            {
                _fullscreen = _fullscreen == hwnd ? 0 : hwnd;
                Tile();
                if (_fullscreen != 0) SetForegroundWindow(hwnd);
            }

            public void MoveToNextMonitor(nint hwnd)
            {
                var monitors = GetMonitorsLeftToRight();
                if (monitors.Count < 2)
                {
                    Log.Debug("MoveToNextMonitor: only one monitor");
                    return;
                }

                var w = windows.First(x => x.Hwnd == hwnd);
                int idx = monitors.IndexOf(ResolveMonitor(w));
                w.Monitor = monitors[(idx + 1) % monitors.Count];
                Log.Info($"Moving hwnd=0x{hwnd:X} to monitor 0x{w.Monitor:X}");
                Tile();
            }

            private static List<nint> GetMonitorsLeftToRight()
            {
                var found = new List<(nint handle, int left)>();
                EnumDisplayMonitors(0, 0, (handle, _, _, _) =>
                {
                    var mi = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
                    if (GetMonitorInfo(handle, ref mi)) found.Add((handle, mi.rcMonitor.left));
                    return true;
                }, 0);
                return found.OrderBy(m => m.left).Select(m => m.handle).ToList();
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
                Log.Debug($"Tile: {windows.Count} window(s) ({before - windows.Count} pruned), gap {gap}");

                foreach (var group in windows.GroupBy(w => ResolveMonitor(w)).ToList())
                {
                    if (!TryGetMonitorRect(group.Key, out var area))
                    {
                        Log.Error($"GetMonitorInfo failed for hMonitor=0x{group.Key:X}, Win32 error {Marshal.GetLastWin32Error()}; skipping its windows");
                        continue;
                    }

                    int totalW = area.right - area.left;
                    int totalH = area.bottom - area.top;
                    var members = group.ToList();
                    var layout = ComputeLayout(members.Count);
                    Log.Debug($"  monitor 0x{group.Key:X} {Describe.Rect(area)}: {members.Count} window(s)");

                    for (int i = 0; i < layout.Count; i++)
                    {
                        var c = layout[i];
                        // Round edges rather than sizes so neighbouring cells never leave a gap
                        int left = area.left + (int)Math.Round(c.X * totalW);
                        int top = area.top + (int)Math.Round(c.Y * totalH);
                        int right = area.left + (int)Math.Round((c.X + c.W) * totalW);
                        int bottom = area.top + (int)Math.Round((c.Y + c.H) * totalH);
                        members[i].Bounds = Rectangle.FromLTRB(left, top, right, bottom);

                        var place = members[i].Hwnd == _fullscreen
                            ? Rectangle.FromLTRB(area.left, area.top, area.right, area.bottom)
                            : Rectangle.FromLTRB(left + gap / 2, top + gap / 2, right - gap / 2, bottom - gap / 2);
                        Place(members[i].Hwnd, place, i);
                    }
                }
            }

            private static void Place(nint hwnd, Rectangle r, int cell)
            {
                // A maximized window ignores MoveWindow's size (and apps like Chrome re-maximize), so un-maximize first
                if (IsZoomed(hwnd))
                {
                    Log.Debug($"  cell {cell}: hwnd=0x{hwnd:X} is maximized, restoring before placing");
                    ShowWindow(hwnd, NCMDSHOW.SW_RESTORE);
                }

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
                    Log.Debug($"  cell {cell}: hwnd=0x{hwnd:X} -> {Describe.Rect(r.Left, r.Top, r.Right, r.Bottom)}");
                else
                    Log.Warn($"MoveWindow failed for hwnd=0x{hwnd:X} Win32 error {Marshal.GetLastWin32Error()}");
            }

            private static nint ResolveMonitor(Window w)
            {
                if (w.Monitor != 0 && TryGetMonitorRect(w.Monitor, out _)) return w.Monitor;
                return w.Monitor = MonitorFromWindow(w.Hwnd, MONITOR_DEFAULTTONEAREST);
            }

            private static bool TryGetMonitorRect(nint monitor, out RECT rect)
            {
                var mi = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
                if (GetMonitorInfo(monitor, ref mi))
                {
                    rect = mi.rcMonitor;
                    return true;
                }
                rect = new RECT();
                return false;
            }

            public int GetNumWindows()
            {
                return windows.Count;
            }

            private class Window(nint HWND)
            {
                public nint Hwnd = HWND;
                public nint Monitor;
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
