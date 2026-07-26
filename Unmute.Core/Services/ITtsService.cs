using Unmute.Core.Models;

namespace Unmute.Core.Services
{
    public interface ITtsService
    {
        bool IsRunning { get; }
        IEnumerable<Voice> AvailableVoices { get; }
        Voice Voice { get; set; }

        void Dispose();
        Task NarrateAsync(string text, Voice? voice = null);
        Task StartAsync();
        Task StopAsync();
    }
}