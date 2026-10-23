using System.Diagnostics;

namespace WndoLand
{
    internal static class Watchdog
    {
        private const double StallSeconds = 3;

        private static long _lastBeat = Stopwatch.GetTimestamp();
        private static volatile string _operation = "idle";
        private static bool _reported;
        private static System.Threading.Timer? _timer;

        public static void Start() => _timer = new System.Threading.Timer(_ => Check(), null, 1000, 1000);

        public static void Stop() => _timer?.Dispose();

        public static void Beat() => Interlocked.Exchange(ref _lastBeat, Stopwatch.GetTimestamp());

        public static Scope Enter(string operation)
        {
            var previous = _operation;
            _operation = operation;
            return new Scope(previous);
        }

        public readonly struct Scope(string previous) : IDisposable
        {
            public void Dispose()
            {
                _operation = previous;
                Beat();
            }
        }

        private static void Check()
        {
            double quiet = (Stopwatch.GetTimestamp() - Interlocked.Read(ref _lastBeat)) / (double)Stopwatch.Frequency;
            if (quiet > StallSeconds && !_reported)
            {
                _reported = true;
                Log.Warn($"UI thread has not responded for {quiet:F1}s; it is busy in: {_operation}");
            }
            else if (quiet <= StallSeconds && _reported)
            {
                _reported = false;
                Log.Info("UI thread is responding again");
            }
        }
    }
}
