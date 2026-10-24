using System.Diagnostics;
using System.Security;
using System.Security.Principal;
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

        public static readonly bool SelfElevated = new WindowsPrincipal(WindowsIdentity.GetCurrent())
            .IsInRole(WindowsBuiltInRole.Administrator);

        public static bool IsEnabled => TaskExists() || RunKeyExists();

        /// <summary>True when the logon task is set to run with highest privileges.</summary>
        public static bool IsElevated => TaskRunsElevated();

        /// <summary>
        /// Registers or removes startup. An elevated task can only be created or deleted by an elevated
        /// process, so those steps re-run schtasks through a UAC prompt; returns false if that was declined.
        /// </summary>
        public static bool Set(bool enabled, bool elevated = false)
        {
            if (enabled)
            {
                if (CreateTask(elevated))
                    RemoveRunKey();
                else if (elevated)
                    return false;
                else
                    SetRunKey();
            }
            else
            {
                if (TaskExists() && !RunSchtasks($"/Delete /TN {TaskName} /F", elevate: TaskRunsElevated()) && TaskExists())
                    return false;
                RemoveRunKey();
            }
            Log.Info($"Start with Windows {(enabled ? "enabled" : "disabled")} (run as administrator: {enabled && elevated})");
            return true;
        }

        /// <summary>
        /// Starts the logon task once this process has exited (the single-instance mutex is held until then),
        /// so the new instance comes up with whatever run level the task now has.
        /// </summary>
        public static void RelaunchViaTask()
        {
            try
            {
                Process.Start(new ProcessStartInfo("cmd.exe", $"/c ping -n 3 127.0.0.1 >nul & schtasks /Run /TN {TaskName}")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Couldn't schedule the relaunch");
            }
        }

        /// <summary>Moves installs that still use the Run key onto the logon task.</summary>
        public static void MigrateToTask()
        {
            if (RunKeyExists() && !TaskExists())
                Set(true);
        }

        private static bool CreateTask(bool elevated)
        {
            var user = SecurityElement.Escape($"{Environment.UserDomainName}\\{Environment.UserName}");
            var exe = SecurityElement.Escape(Environment.ProcessPath!);
            var xml = $"""
                <?xml version="1.0" encoding="UTF-16"?>
                <Task version="1.2" xmlns="http://schemas.microsoft.com/windows/2004/02/mit/task">
                  <Triggers><LogonTrigger><Enabled>true</Enabled><UserId>{user}</UserId></LogonTrigger></Triggers>
                  <Principals><Principal id="Author"><UserId>{user}</UserId><LogonType>InteractiveToken</LogonType><RunLevel>{(elevated ? "HighestAvailable" : "LeastPrivilege")}</RunLevel></Principal></Principals>
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
                return RunSchtasks($"/Create /TN {TaskName} /XML \"{path}\" /F", elevate: elevated);
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

        private static bool TaskRunsElevated()
        {
            var xml = QuerySchtasks($"/Query /TN {TaskName} /XML");
            return xml != null && xml.Contains("<RunLevel>HighestAvailable</RunLevel>", StringComparison.OrdinalIgnoreCase);
        }

        private static string? QuerySchtasks(string args)
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
                var output = p!.StandardOutput.ReadToEnd();
                p.StandardError.ReadToEnd();
                p.WaitForExit();
                return p.ExitCode == 0 ? output : null;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"schtasks {args} failed");
                return null;
            }
        }

        private static bool RunSchtasks(string args, bool elevate = false)
        {
            try
            {
                if (elevate && !SelfElevated)
                {
                    using var admin = Process.Start(new ProcessStartInfo("schtasks.exe", args)
                    {
                        UseShellExecute = true,
                        Verb = "runas",
                        WindowStyle = ProcessWindowStyle.Hidden,
                    });
                    admin!.WaitForExit();
                    return admin.ExitCode == 0;
                }

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
            catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
            {
                Log.Warn("Administrator prompt declined");
                return false;
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
