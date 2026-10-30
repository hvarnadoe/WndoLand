using Velopack;

namespace WndoLand
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // Turns on per-monitor DPI awareness, so it must run before anything reads monitor sizes. WinForms caches
            // Screen.AllScreens on first use, and a read while still DPI-unaware keeps scaled-down sizes for good
            // (1600x1067 for a 2400x1600 screen at 150%), which left tiled windows filling only part of the screen.
            ApplicationConfiguration.Initialize();

            VelopackApp.Build()
                .OnFirstRun(_ => StartupRegistration.Set(true))
                .OnBeforeUninstallFastCallback(_ => StartupRegistration.Set(false))
                .Run();

            using var singleInstance = new Mutex(true, @"Local\WndoLand.SingleInstance", out var isFirstInstance);
            if (!isFirstInstance)
                return;

            Log.Init();

            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (_, e) => Log.Error(e.Exception, "Unhandled UI-thread exception");
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                Log.Fatal(e.ExceptionObject as Exception ?? new Exception(e.ExceptionObject?.ToString()),
                          $"Unhandled exception (terminating={e.IsTerminating})");
                Borders.RestoreAll();
                Log.Shutdown();
            };
            AppDomain.CurrentDomain.ProcessExit += (_, _) => Borders.RestoreAll();
            TaskScheduler.UnobservedTaskException += (_, e) => Log.Error(e.Exception, "Unobserved task exception");

            try
            {
                Log.LogStartup();
                StartupRegistration.MigrateToTask();
                Application.Run(new MainForm());
                Log.Info("Application exited normally");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Fatal error in Main");
                throw;
            }
            finally
            {
                Log.Shutdown();
            }
        }
    }
}
