namespace Unmute.Core.Services
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