using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using Unmute.Core.Models;
using Unmute.Core.Services;

namespace Unmute.TTS
{
    internal class PocketTtsService : ITtsService, IDisposable
    {
        private readonly HttpClient httpClient = new();
        private readonly PlaybackDevice playback = new();
        private readonly CancellationTokenSource cts = new();

        private PythonClient python;
        private Process? process;

        public bool IsRunning
        {
            get
            {
                if (process == null)
                    return false;

                try
                {
                    Process.GetProcessById(process.Id);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        public IEnumerable<Voice> AvailableVoices { get; private set; }

        public Voice Voice { get; set; }

        public PocketTtsService()
        {
           
        }

        public async Task NarrateAsync(string text, Voice? voice = null)
        {
            if (voice is null)
                voice = this.Voice;

            var lines = text.Split(['.', '?', '!' ], StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                using var request = new MultipartFormDataContent();
                request.Add(new StringContent(line), "text");
                request.Add(new StringContent(voice.Id), "voice_url");
                var response = await this.httpClient.PostAsync("tts", request);
                if (!response.IsSuccessStatusCode)
                    continue;

                using var stream = await response.Content.ReadAsStreamAsync();
                await this.playback.PlayAsync(stream, this.cts.Token);
            }
        }

        public async Task InitializeAsync()
        {
            var regex = new Regex(@"\""(?<id>\w+)\"" \((?<lang>\w+)\)", RegexOptions.Compiled);
            this.AvailableVoices = File.ReadAllLines("voices.txt")
                                       .Select(x => regex.Match(x))
                                       .Where(x => x.Success)
                                       .Select(x => new Voice(x.Groups["id"].ToString(),
                                                              this.ToTitleCase(x.Groups["id"].ToString()),
                                                              x.Groups["lang"].ToString()))
                                       .ToImmutableArray();

            this.Voice = this.AvailableVoices.First();

            this.python = await new PythonInstaller()
                   .EnableImports()
                   .WithPip()
                   .WithUV()
                   .Version("3.13.14")
                   .InstallAsync();
        }

        public async Task StartAsync()
        {
            const int localPort = 5000;
            this.httpClient.BaseAddress = new Uri($"http://localhost:{localPort}");
            this.process = this.python.ExecutePython($"-m uv tool run pocket-tts serve --host \"localhost\" --port {localPort}", Console.Out);
        }

        public async Task StopAsync()
        {
            if (this.process is not null)
            {
                this.process.Kill();
                await this.process.WaitForExitAsync();
                this.process.Dispose();
                this.process = null;
            }
        }

        public void Dispose()
        {
            this.cts.Cancel();
            this.StopAsync().Wait();
            this.playback.Dispose();
        }

        private string ToTitleCase(string name)
        {
            return CultureInfo.CurrentCulture.TextInfo
                .ToTitleCase(name.Replace("_", " ").ToLower());
        }
    }
}
