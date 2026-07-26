using Unmute.Core.Services;
using System.Diagnostics;

namespace Unmute.TTS
{
    internal class TtsService : ITtsService, IDisposable
    {
        private readonly HttpClient httpClient = new();
        private readonly PlaybackDevice playback = new();

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


        public TtsService()
        {
            
        }

        public async Task Narrate(string text)
        {
            using var request = new MultipartFormDataContent();
            request.Add(new StringContent(text), "text");
            var response = await this.httpClient.PostAsync("tts", request);
            if (!response.IsSuccessStatusCode)
                return;
            
            var wavStream = await response.Content.ReadAsStreamAsync();
            this.playback.Play(wavStream);
            // TODO add await so the stream can be disposed here instead of in playback
        }

        public async Task StartAsync()
        {
            var pythonClient = await new PythonInstaller()
                    .EnableImports()
                    .WithPip()
                    .WithUV()
                    .Version("3.13.14")
                    .InstallAsync();

            const int localPort = 5000;
            httpClient.BaseAddress = new Uri($"http://localhost:{localPort}");
            process = pythonClient.ExecutePython($"-m uv tool run pocket-tts serve --host \"localhost\" --port {localPort}", Console.Out);
        }

        public async Task StopAsync()
        {
            if (process is not null)
            {
                process.Kill();
                await process.WaitForExitAsync();
                process.Dispose();
                process = null;
            }
        }

        public void Dispose()
        {
            this.StopAsync().Wait();
            this.playback.Dispose();
        }
    }
}
