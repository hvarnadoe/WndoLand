using System.Text;
using System.Text.Json;
using static WndoLand.Win32;

namespace WndoLand
{
    internal class WindowEntry
    {
        public long Hwnd { get; set; }
        public uint Pid { get; set; }
        public string Class { get; set; } = "";
        public int Workspace { get; set; }

        public static WindowEntry From(nint hwnd, int workspace)
        {
            GetWindowThreadProcessId(hwnd, out uint pid);
            var cls = new StringBuilder(256);
            GetClassName(hwnd, cls, cls.Capacity);
            return new WindowEntry { Hwnd = hwnd, Pid = pid, Class = cls.ToString(), Workspace = workspace };
        }

        public bool StillValid()
        {
            nint hwnd = (nint)Hwnd;
            if (!IsWindow(hwnd)) return false;
            GetWindowThreadProcessId(hwnd, out uint pid);
            var cls = new StringBuilder(256);
            GetClassName(hwnd, cls, cls.Capacity);
            return pid == Pid && cls.ToString() == Class;
        }
    }

    internal class WorkspaceState
    {
        public int Active { get; set; } = 1;
        public List<WindowEntry> Windows { get; set; } = new();
        public List<WindowEntry> Floating { get; set; } = new();
    }

    internal static class StateStore
    {
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        public static WorkspaceState? Load()
        {
            try
            {
                if (!File.Exists(AppPaths.StateFile)) return null;
                return JsonSerializer.Deserialize<WorkspaceState>(File.ReadAllText(AppPaths.StateFile));
            }
            catch (Exception ex)
            {
                Log.Warn(ex, "Couldn't read saved workspace layout");
                return null;
            }
        }

        public static void Save(WorkspaceState state)
        {
            try { File.WriteAllText(AppPaths.StateFile, JsonSerializer.Serialize(state, JsonOptions)); }
            catch (Exception ex) { Log.Warn(ex, "Couldn't save workspace layout"); }
        }
    }
}
