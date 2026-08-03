using System.Drawing;
using Unmute.Core.Models;

namespace Unmute.OCR.Services
{
    public interface IOcrService
    {
        OcrEngineType SelectedEngineType { get; }
        Task InitializeAsync(OcrEngineType engineType);
        Task<IEnumerable<OCRResult>> ReadTextAsync(Bitmap bitmap);
    }
}