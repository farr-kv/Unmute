using Microsoft.Extensions.DependencyInjection;
using Unmute.OCR.Services;
using Unmute.OCR.Services.Implementations;

namespace Unmute.OCR.PaddleOCR.Extensions
{
    public static class DIExtensions
    {
        public static IServiceCollection UseOCR(this IServiceCollection instance)
        {
            instance.AddTransient<TesseractOcrEngine>();
            instance.AddTransient<PaddleOcrEngine>();
            instance.AddSingleton<IOcrService, OcrService>();

            return instance;
        }
    }
}
