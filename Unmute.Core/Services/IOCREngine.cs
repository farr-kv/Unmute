using System.Drawing;
using Unmute.Core.Models;

namespace Unmute.Core.Services
{
    public interface IOCREngine
    {
        Task InitializeAsync();
        Task<IEnumerable<OCRResult>> ReadTextAsync(Bitmap bmp);
    }
}