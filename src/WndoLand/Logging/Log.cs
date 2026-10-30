using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using Serilog;
using Serilog.Events;
using static WndoLand.Win32;

namespace WndoLand
{
    internal static class Log
    {
        private const string OutputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}";
        private const string Prefix = "[T{Tid}] {File:l}:{Line} ({Member:l}) | {Msg:l}";

        private static Serilog.ILogger _logger = Serilog.Core.Logger.None;

        public static string LogDirectory { get; } =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WndoLand", "logs");

        public static bool IsDebugEnabled => _logger.IsEnabled(LogEventLevel.Debug);

        public static void Init(LogEventLevel minimumLevel = LogEventLevel.Debug)
        {
            try
            {
                Directory.CreateDirectory(LogDirectory);
                _logger = new Serilog.LoggerConfiguration()
                    .MinimumLevel.Is(minimumLevel)
                    .WriteTo.File(
                        Path.Combine(LogDirectory, "wndoland-.log"),
                        outputTemplate: OutputTemplate,
                        rollingInterval: Serilog.RollingInterval.Day,
                        retainedFileCountLimit: 14,
                        shared: true,
                        flushToDiskInterval: TimeSpan.FromSeconds(1))
                    .CreateLogger();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to initialise file logging: {ex}");
            }
        }

        public static void Shutdown() => (_logger as IDisposable)?.Dispose();

        public static void LogStartup([CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "")
        {
            using var me = Process.GetCurrentProcess();
            var asm = typeof(Log).Assembly.GetName();
            var monitors = Monitors.All();
            Write(LogEventLevel.Information, null,
                $"=== WndoLand {asm.Version} starting === pid={me.Id} elevated={IsElevated()} " +
                $"os=\"{RuntimeInformation.OSDescription}\" runtime=\"{RuntimeInformation.FrameworkDescription}\" " +
                $"arch={RuntimeInformation.ProcessArchitecture} cwd=\"{Environment.CurrentDirectory}\" " +
                $"dpiMode={Application.HighDpiMode} monitors={monitors.Count} logDir=\"{LogDirectory}\"", file, line, member);

            // Without per-monitor awareness, sizes and positions on scaled displays get mixed up between scaled and real pixels
            if (Application.HighDpiMode != HighDpiMode.PerMonitorV2)
                Write(LogEventLevel.Warning, null,
                    $"Not per-monitor DPI aware ({Application.HighDpiMode}); tiling may be wrong on displays scaled above 100%", file, line, member);

            foreach (var m in monitors)
                Write(LogEventLevel.Debug, null,
                    $"Monitor {m.DeviceName} bounds={m.Bounds} work={m.WorkArea} dpi={m.Dpi} ({m.ScalePercent}%) primary={m.Primary}", file, line, member);
        }

        private static bool IsElevated()
        {
            try
            {
                using var id = WindowsIdentity.GetCurrent();
                return new WindowsPrincipal(id).IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch { return false; }
        }

        public static void Debug(string msg, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "")
            => Write(LogEventLevel.Debug, null, msg, file, line, member);

        public static void Info(string msg, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "")
            => Write(LogEventLevel.Information, null, msg, file, line, member);

        public static void Warn(string msg, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "")
            => Write(LogEventLevel.Warning, null, msg, file, line, member);

        public static void Warn(Exception ex, string msg, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "")
            => Write(LogEventLevel.Warning, ex, msg, file, line, member);

        public static void Error(string msg, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "")
            => Write(LogEventLevel.Error, null, msg, file, line, member);

        public static void Error(Exception ex, string msg, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "")
            => Write(LogEventLevel.Error, ex, msg, file, line, member);

        public static void Fatal(Exception ex, string msg, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "")
            => Write(LogEventLevel.Fatal, ex, msg, file, line, member);

        private static void Write(LogEventLevel level, Exception? ex, string msg, string file, int line, string member)
        {
            if (!_logger.IsEnabled(level)) return;
            _logger.Write(level, ex, Prefix, Environment.CurrentManagedThreadId, Path.GetFileName(file), line, member, msg);
        }
    }

    internal static class Describe
    {
        public static string Window(nint hwnd)
        {
            if (!IsWindow(hwnd)) return $"hwnd=0x{hwnd:X} exists=False";

            var cls = new StringBuilder(256);
            GetClassName(hwnd, cls, cls.Capacity);
            var title = new StringBuilder(256);
            GetWindowText(hwnd, title, title.Capacity);
            GetWindowThreadProcessId(hwnd, out uint pid);

            string proc;
            try { using var p = Process.GetProcessById((int)pid); proc = p.ProcessName; }
            catch { proc = "?"; }

            return $"hwnd=0x{hwnd:X} exists=True pid={pid} proc={proc} class={cls} title=\"{title}\" " +
                   $"visible={IsWindowVisible(hwnd)} minimized={IsIconic(hwnd)}";
        }

        public static string Rect(RECT r) => Rect(r.left, r.top, r.right, r.bottom);

        public static string Rect(int left, int top, int right, int bottom) =>
            $"[L{left} T{top} R{right} B{bottom} ({right - left}x{bottom - top})]";
    }
}
