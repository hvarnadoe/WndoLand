using System.Diagnostics;
using System.Security;
using Microsoft.Win32;

namespace WndoLand
{
    /// <summary>
    /// Starts WndoLand at sign-in via a logon scheduled task. Run-key apps are held back by Explorer's
    /// startup delay (and queue behind every other startup app); logon tasks start immediately.
    /// </summary>
    internal static class StartupRegistration
    {
        private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ValueName = "WndoLand";
        private const string TaskName = "WndoLand";

        public static bool IsEnabled => TaskExists() || RunKeyExists();

        public static void Set(bool enabled)
        {
            if (enabled)
            {
                if (CreateTask())
                    RemoveRunKey();
                else
                    SetRunKey();
            }
            else
            {
                RunSchtasks($"/Delete /TN {TaskName} /F");
                RemoveRunKey();
            }
            Log.Info($"Start with Windows {(enabled ? "enabled" : "disabled")}");
        }

        /// <summary>Moves installs that still use the Run key onto the logon task.</summary>
        public static void MigrateToTask()
        {
            if (RunKeyExists() && !TaskExists())
                Set(true);
        }

        private static bool CreateTask()
        {
            var user = SecurityElement.Escape($"{Environment.UserDomainName}\\{Environment.UserName}");
            var exe = SecurityElement.Escape(Environment.ProcessPath!);
            var xml = $"""
                <?xml version="1.0" encoding="UTF-16"?>
                <Task version="1.2" xmlns="http://schemas.microsoft.com/windows/2004/02/mit/task">
                  <Triggers><LogonTrigger><Enabled>true</Enabled><UserId>{user}</UserId></LogonTrigger></Triggers>
                  <Principals><Principal id="Author"><UserId>{user}</UserId><LogonType>InteractiveToken</LogonType><RunLevel>LeastPrivilege</RunLevel></Principal></Principals>
                  <Settings>
                    <MultipleInstancesPolicy>IgnoreNew</MultipleInstancesPolicy>
                    <DisallowStartIfOnBatteries>false</DisallowStartIfOnBatteries>
                    <StopIfGoingOnBatteries>false</StopIfGoingOnBatteries>
                    <ExecutionTimeLimit>PT0S</ExecutionTimeLimit>
                    <Priority>4</Priority>
                  </Settings>
                  <Actions Context="Author"><Exec><Command>{exe}</Command></Exec></Actions>
                </Task>
                """;

            var path = Path.Combine(Path.GetTempPath(), "wndoland-task.xml");
            try
            {
                File.WriteAllText(path, xml, System.Text.Encoding.Unicode);
                return RunSchtasks($"/Create /TN {TaskName} /XML \"{path}\" /F");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to create startup task");
                return false;
            }
            finally
            {
                try { File.Delete(path); } catch { }
            }
        }

        private static bool TaskExists() => RunSchtasks($"/Query /TN {TaskName}");

        private static bool RunSchtasks(string args)
        {
            try
            {
                using var p = Process.Start(new ProcessStartInfo("schtasks.exe", args)
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                });
                p!.StandardOutput.ReadToEnd();
                p.StandardError.ReadToEnd();
                p.WaitForExit();
                return p.ExitCode == 0;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"schtasks {args} failed");
                return false;
            }
        }

        private static bool RunKeyExists()
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey);
            return key?.GetValue(ValueName) is string;
        }

        private static void SetRunKey()
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true) ?? Registry.CurrentUser.CreateSubKey(RunKey);
            key.SetValue(ValueName, $"\"{Environment.ProcessPath}\"");
        }

        private static void RemoveRunKey()
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
            key?.DeleteValue(ValueName, throwOnMissingValue: false);
        }
    }
}
