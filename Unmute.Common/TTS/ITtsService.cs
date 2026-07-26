namespace Flute.Common.TTS
{
    public interface ITtsService
    {
        bool IsRunning { get; }

        void Dispose();
        Task Narrate(string text);
        Task StartAsync();
        Task StopAsync();
    }
}