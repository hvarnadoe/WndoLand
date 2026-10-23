using Velopack;
using Velopack.Sources;

namespace WndoLand
{
    /// <summary>
    /// Checks GitHub Releases for a newer version at startup and every few hours. With auto-update on it
    /// installs and restarts; otherwise it downloads and raises <see cref="UpdateReady"/> so the user can choose.
    /// Does nothing when running outside an installed copy (e.g. from Visual Studio).
    /// </summary>
    internal sealed class UpdateService : IDisposable
    {
        private const string RepoUrl = "https://github.com/hvarnadoe/WndoLand";
        private static readonly TimeSpan StartupDelay = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan Interval = TimeSpan.FromHours(12);

        // WNDOLAND_UPDATE_SOURCE (a folder of vpk output) replaces GitHub, for testing updates locally.
        private readonly UpdateManager _manager = new(CreateSource());
        private readonly System.Windows.Forms.Timer _timer = new();
        private UpdateInfo? _pending;
        private bool _busy;

        private static IUpdateSource CreateSource()
        {
            var local = Environment.GetEnvironmentVariable("WNDOLAND_UPDATE_SOURCE");
            if (!string.IsNullOrWhiteSpace(local))
            {
                Log.Info($"Using local update source {local}");
                return new SimpleFileSource(new DirectoryInfo(local));
            }
            return new GithubSource(RepoUrl, null, false);
        }

        public bool IsInstalled => _manager.IsInstalled;
        public string? PendingVersion => _pending?.TargetFullRelease.Version.ToString();

        /// <summary>Raised on the UI thread when an update is downloaded and waiting for the user.</summary>
        public event Action<string>? UpdateReady;

        public void Start()
        {
            if (!IsInstalled)
            {
                Log.Info("Not running from an installed copy, update checks disabled");
                return;
            }

            _timer.Interval = (int)StartupDelay.TotalMilliseconds;
            _timer.Tick += async (_, _) =>
            {
                _timer.Interval = (int)Interval.TotalMilliseconds;
                await CheckAsync();
            };
            _timer.Start();
        }

        /// <summary>Checks now. Returns false if nothing newer was found or the check failed.</summary>
        public async Task<bool> CheckAsync()
        {
            if (!IsInstalled || _busy) return false;
            _busy = true;
            try
            {
                var info = await _manager.CheckForUpdatesAsync();
                if (info == null)
                {
                    Log.Info("No update available");
                    return false;
                }

                var version = info.TargetFullRelease.Version.ToString();
                Log.Info($"Update available: {version}");
                await _manager.DownloadUpdatesAsync(info);
                _pending = info;

                if (Settings.Current.AutoUpdate)
                    ApplyPending();
                else
                    UpdateReady?.Invoke(version);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Update check failed");
                return false;
            }
            finally
            {
                _busy = false;
            }
        }

        /// <summary>Installs the downloaded update and restarts the app.</summary>
        public void ApplyPending()
        {
            if (_pending == null) return;
            Log.Info($"Applying update {_pending.TargetFullRelease.Version} and restarting");
            Borders.RestoreAll();
            _manager.ApplyUpdatesAndRestart(_pending.TargetFullRelease);
        }

        public void Dispose() => _timer.Dispose();
    }
}
