using NAudio.Wave;

namespace Unmute.TTS.PocketTTS
{
    internal class PlaybackDevice: IDisposable
    {
        private readonly BufferedWaveProvider provider;
        private readonly WaveOutEvent player;

        public PlaybackDevice()
        {
            var waveFormat = new WaveFormat(24000, 16, 1);
            provider = new BufferedWaveProvider(waveFormat)
            {
                BufferDuration = TimeSpan.FromSeconds(30)
            };
            player = new WaveOutEvent();

            player.Init(provider);
            player.Play();
        }

        public void Dispose()
        {
            player.Dispose();
        }

        public async Task PlayAsync(Stream stream)
        {
            using var reader = new WaveFileReader(stream);
            var bytesRead = 0;
            var buffer = new byte[4096];
            while ((bytesRead = reader.Read(buffer, 0, buffer.Length)) > 0)
            {
                while (provider.BufferedBytes + bytesRead > provider.BufferLength)
                {
                    await Task.Delay(20);
                }
                provider.AddSamples(buffer, 0, bytesRead);
            }
        }

        public void Stop()
        {
            provider.ClearBuffer();
        }
    }
}
