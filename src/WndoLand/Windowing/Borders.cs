using System.Diagnostics;
using System.Text.Json;
using static WndoLand.Win32;

namespace WndoLand
{
    internal static class Borders
    {
        private const int Stripped = WS_CAPTION | WS_THICKFRAME | WS_SYSMENU | WS_MINIMIZEBOX | WS_MAXIMIZEBOX;
        private static readonly Dictionary<nint, int> _original = new();

        private static readonly HashSet<nint> _quiet = new();

        public static void DisableTransitions(nint hwnd)
        {
            if (!IsWindow(hwnd) || !_quiet.Add(hwnd)) return;
            int on = 1;
            DwmSetWindowAttribute(hwnd, DWMWA_TRANSITIONS_FORCEDISABLED, ref on, sizeof(int));
        }

        private static void EnableTransitions(nint hwnd)
        {
            if (!_quiet.Remove(hwnd) || !IsWindow(hwnd)) return;
            int off = 0;
            DwmSetWindowAttribute(hwnd, DWMWA_TRANSITIONS_FORCEDISABLED, ref off, sizeof(int));
        }

        private record SavedBorder(long Hwnd, uint Pid, int Style);

        public static void Strip(nint hwnd)
        {
            if (!Settings.Current.StripTitleBars || _original.ContainsKey(hwnd) || !IsWindow(hwnd)) return;

            int style = GetWindowLong(hwnd, GWL_STYLE);
            _original[hwnd] = style;
            var sw = Stopwatch.StartNew();
            SetStyle(hwnd, style & ~Stripped);
            Persist();
            if (sw.ElapsedMilliseconds > SlowMs)
                Log.Warn($"Stripping borders from hwnd=0x{hwnd:X} took {sw.ElapsedMilliseconds} ms (the window was slow to respond)");
            Log.Debug($"Stripped borders from hwnd=0x{hwnd:X} (style 0x{style:X})");
        }

        // SetWindowLong waits on the window's own thread, so a busy app would freeze us; run it off the UI thread
        public static void Restore(nint hwnd)
        {
            RestoreCore(hwnd);
            Persist();
        }

        public static void RestoreAll()
        {
            foreach (var hwnd in _quiet.ToList()) EnableTransitions(hwnd);

            var pending = _original.Keys.ToList().Select(RestoreCore).OfType<Task>().ToArray();
            Persist();
            if (!Task.WaitAll(pending, ExitWaitMs))
                Log.Warn($"{pending.Count(t => !t.IsCompleted)} window(s) did not accept their border restore within {ExitWaitMs} ms");
        }

        private const int ExitWaitMs = 2000;

        private static Task? RestoreCore(nint hwnd)
        {
            EnableTransitions(hwnd);
            if (!_original.Remove(hwnd, out int style) || !IsWindow(hwnd)) return null;

            return Task.Run(() =>
            {
                var sw = Stopwatch.StartNew();
                SetStyle(hwnd, style);
                if (sw.ElapsedMilliseconds > SlowMs)
                    Log.Warn($"Restoring borders on hwnd=0x{hwnd:X} took {sw.ElapsedMilliseconds} ms (the window was slow to respond)");
                else
                    Log.Debug($"Restored borders on hwnd=0x{hwnd:X}");
            });
        }

        private const int SlowMs = 250;

        public static void RecoverFromCrash()
        {
            try
            {
                if (!File.Exists(AppPaths.BorderStateFile)) return;
                var saved = JsonSerializer.Deserialize<List<SavedBorder>>(File.ReadAllText(AppPaths.BorderStateFile)) ?? new();
                foreach (var s in saved)
                {
                    nint hwnd = (nint)s.Hwnd;
                    if (!IsWindow(hwnd)) continue;
                    GetWindowThreadProcessId(hwnd, out uint pid);
                    if (pid != s.Pid || (GetWindowLong(hwnd, GWL_STYLE) & WS_CAPTION) == WS_CAPTION) continue;
                    SetStyle(hwnd, s.Style);
                    Log.Warn($"Recovered borders left stripped by a previous run: hwnd=0x{hwnd:X}");
                }
                File.Delete(AppPaths.BorderStateFile);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Couldn't recover window borders from the previous run");
            }
        }

        private static void Persist()
        {
            try
            {
                var saved = _original.Select(kv =>
                {
                    GetWindowThreadProcessId(kv.Key, out uint pid);
                    return new SavedBorder(kv.Key, pid, kv.Value);
                }).ToList();
                File.WriteAllText(AppPaths.BorderStateFile, JsonSerializer.Serialize(saved));
            }
            catch (Exception ex)
            {
                Log.Warn(ex, "Couldn't write border state");
            }
        }

        private static void SetStyle(nint hwnd, int style)
        {
            SetWindowLong(hwnd, GWL_STYLE, style);
            SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE | SWP_FRAMECHANGED);
        }
    }
}
