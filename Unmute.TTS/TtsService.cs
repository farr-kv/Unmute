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

        public async Task NarrateAsync(string text)
        {
            using var request = new MultipartFormDataContent();
            request.Add(new StringContent(text), "text");
            var response = await this.httpClient.PostAsync("tts", request);
            if (!response.IsSuccessStatusCode)
                return;
            
            using var stream = await response.Content.ReadAsStreamAsync();
            using var wav = await this.FixWavStream(stream);
            await this.playback.PlayAsync(wav);
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

        // PocketTTS does not set the length as it streams the audio. This needs to be corrected before initiating playback
        private async Task<Stream> FixWavStream(Stream stream)
        {
            var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            var buffer = ms.GetBuffer();
            var totalLength = (int)ms.Length;

            var riffSize = totalLength - 8;
            BitConverter.GetBytes(riffSize).CopyTo(buffer, 4);
            var dataSize = totalLength - 44;
            BitConverter.GetBytes(dataSize).CopyTo(buffer, 40);

            ms.Position = 0;
            return ms;
        }
    }
}
