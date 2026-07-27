using SoundFlow.Abstracts.Devices;
using SoundFlow.Backends.MiniAudio;
using SoundFlow.Components;
using SoundFlow.Providers;
using SoundFlow.Structs;
using Unmute.Core;

namespace Unmute.TTS.PocketTTS
{
    internal class PlaybackDevice: IDisposable
    {
        private AudioFormat Format { get; }
        private MiniAudioEngine Engine { get; }
        private AudioPlaybackDevice AudioPlaybackDevice { get; }
        private IDisposable? playbackTask;

        public PlaybackDevice()
        {
            this.Format = AudioFormat.Cd;
            this.Engine = new MiniAudioEngine();
            this.Engine.UpdateAudioDevicesInfo();
            var defaultDevice = this.Engine.PlaybackDevices.FirstOrDefault(x => x.IsDefault);
            this.AudioPlaybackDevice = this.Engine.InitializePlaybackDevice(defaultDevice, this.Format);
            this.AudioPlaybackDevice.Start();
        }

        public void Dispose()
        {
            this.playbackTask?.Dispose();
            this.AudioPlaybackDevice.Dispose();
            this.Engine.Dispose();
        }

        public Task PlayAsync(Stream stream)
        {
            this.Stop();

            var dataProvider = new StreamDataProvider(this.Engine, this.Format, stream);
            var player = new SoundPlayer(this.Engine, this.Format, dataProvider);
            this.AudioPlaybackDevice.MasterMixer.AddComponent(player);

            this.playbackTask = new DisposableAction(() =>
            {
                this.AudioPlaybackDevice.MasterMixer.RemoveComponent(player);
                player.Dispose();
                dataProvider.Dispose();
                this.playbackTask = null;
            });

            var tcs = new TaskCompletionSource<bool>();
            player.PlaybackEnded += (_, _) => tcs.SetResult(true);
            player.Play();
            return tcs.Task;
        }

        public void Stop()
        {
            this.playbackTask?.Dispose();
        }
    }
}
