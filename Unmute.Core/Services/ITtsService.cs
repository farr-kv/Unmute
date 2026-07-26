namespace Unmute.Core.Services
{
    public interface ITtsService
    {
        bool IsRunning { get; }

        void Dispose();
        Task NarrateAsync(string text);
        Task StartAsync();
        Task StopAsync();
    }
}