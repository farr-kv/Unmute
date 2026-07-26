using Unmute.Core.Models;

namespace Unmute.Core.Services
{
    public interface IOCREngine
    {
        Task<IEnumerable<OCRResult>> ReadTextAsync(byte[] imageBytes);
    }
}