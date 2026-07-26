using System.Diagnostics;
using Unmute.Core.Interops;

namespace Unmute.Core.Services.Implementations
{
    internal class ApplicationMonitor : IApplicationMonitor
    {
        private const float CHANGE_TOLERANCE = 0.1f;
        private const float CONFIDENCE_TOLERANCE = 0.75f;

        private readonly IOCREngine engine;

        public ApplicationMonitor(IOCREngine engine)
        {
            this.engine = engine;
        }

        public async Task<IDisposable> MonitorProcessAsync(Process process, TimeSpan pollFrequency, Models.Delegates.TextChanged onTextChanged)
        {
            var cts = new CancellationTokenSource();
            var task = Task.Run(async () =>
            {
                var timer = new PeriodicTimer(pollFrequency);
                var currentScreenText = string.Empty;

                while (await timer.WaitForNextTickAsync())
                {
                    if (cts.IsCancellationRequested)
                        break;

                    var text = await this.MonitorProcessAsync(process);
                    var distance = await this.GetLevenshteinDistanceAsync(currentScreenText, text);
                    var changePerc = (float)distance / Math.Max(currentScreenText.Length, text.Length);

                    if (changePerc > CHANGE_TOLERANCE)
                    {
                        onTextChanged(text);
                    }
                    currentScreenText = text;
                }
            });

            return new ActionDisposable(() =>
            {
                cts.Cancel();
                task.Wait();
                cts.Dispose();
                task.Dispose();
            });
        }

        public async Task<string> MonitorProcessAsync(Process process)
        {
            var bytes = ScreenCaptureInterop.Capture(process);
            if (bytes is null)
                return string.Empty;

            return await this.ExtractTextFromImageAsync(bytes);
        }

        private async Task<string> ExtractTextFromImageAsync(byte[] bytes)
        {
            var results = (await engine.ReadTextAsync(bytes))
                .Where(x => x.Confidence >= CONFIDENCE_TOLERANCE)
                .Select(x => x.Text);

            return string.Join(Environment.NewLine, results);
        }

        private async Task<int> GetLevenshteinDistanceAsync(string s, string t)
        {
            var n = s.Length;
            var m = t.Length;

            if (n == 0)
                return m;

            if (m == 0)
                return n;

            var d = new int[n + 1, m + 1];
            for (int i = 0; i <= n; i++)
            {
                d[i, 0] = i;
            }

            for (int j = 0; j <= m; j++)
            {
                d[0, j] = j;
            }

            for (var i = 1; i <= n; i++)
            {
                for (var j = 1; j <= m; j++)
                {
                    var cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
                }
            }
            return d[n, m];
        }
    }
}
