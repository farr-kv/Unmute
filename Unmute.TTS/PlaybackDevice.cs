using SoundFlow.Abstracts.Devices;
using SoundFlow.Backends.MiniAudio;
using SoundFlow.Components;
using SoundFlow.Providers;
using SoundFlow.Structs;
using Unmute.Core;

namespace Unmute.TTS
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

        public void Play(Stream stream)
        {
            this.playbackTask?.Dispose();

            var dataProvider = new StreamDataProvider(this.Engine, this.Format, stream);
            var player = new SoundPlayer(this.Engine, this.Format, dataProvider);
            this.AudioPlaybackDevice.MasterMixer.AddComponent(player);

            this.playbackTask = new ActionDisposable(() =>
            {
                this.AudioPlaybackDevice.MasterMixer.RemoveComponent(player);
                player.Dispose();
                dataProvider.Dispose();
                stream.Dispose();
                this.playbackTask = null;
            });
            player.Play();            
        }

        public void Stop()
        {
            this.playbackTask?.Dispose();
        }
    }
}
