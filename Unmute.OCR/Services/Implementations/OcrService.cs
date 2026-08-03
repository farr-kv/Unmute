using Microsoft.Extensions.DependencyInjection;
using System.Drawing;
using Unmute.Core.Models;

namespace Unmute.OCR.Services.Implementations
{
    internal class OcrService : IOcrService, IDisposable
    {
        public OcrEngineType SelectedEngineType { get; private set; }
        private IOcrEngine SelectedEngine { get; set; }

        private readonly IServiceProvider serviceProvider;

        public OcrService(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public Task InitializeAsync(OcrEngineType engineType)
        {
            if(SelectedEngineType == engineType)
                return Task.CompletedTask;

            this.SelectedEngine?.Dispose();
            this.SelectedEngine = engineType switch
            {
                OcrEngineType.Tesseract => serviceProvider.GetRequiredService<TesseractOcrEngine>(),
                OcrEngineType.PaddleOcr => serviceProvider.GetRequiredService<PaddleOcrEngine>(),
                _ => throw new NotSupportedException($"OCR engine type {engineType} is not supported.")
            };
            this.SelectedEngineType = engineType;
            return this.SelectedEngine.InitializeAsync();
        }

        public Task<IEnumerable<OCRResult>> ReadTextAsync(Bitmap bitmap)
        {
            return this.SelectedEngine.ReadTextAsync(bitmap);
        }

        public void Dispose()
        {
            this.SelectedEngine?.Dispose();
        }
    }
}
