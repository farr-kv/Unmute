using System.Drawing;
using Unmute.Core.Models;

namespace Unmute.OCR.Services
{
    internal interface IOcrEngine: IDisposable
    {
        Task InitializeAsync();
        Task<IEnumerable<OCRResult>> ReadTextAsync(Bitmap bitmap);
    }
}