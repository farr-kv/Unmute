using System.Diagnostics;

namespace Unmute.Core.Services
{
    public interface IApplicationMonitor
    {
        Task<IDisposable> MonitorProcessAsync(Process process, TimeSpan pollFrequency, Models.Delegates.TextChanged onTextChanged);
        Task<string> MonitorProcessAsync(Process process);
        Task<byte[]?> GetProcessScreenshotAsync(Process process);
    }
}