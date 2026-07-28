using OpenCvSharp;
using Sdcb.PaddleOCR;
using Sdcb.PaddleOCR.Models.Local;
using Unmute.Core.Models;
using Unmute.Core.Services;

namespace Unmute.OCR.PaddleOCR
{
    internal sealed class PaddleOCREngine : IOCREngine, IDisposable
    {
        private PaddleOcrAll ocr;

        public PaddleOCREngine()
        {
        }

        public Task InitializeAsync()
        {
            ocr = new PaddleOcrAll(LocalFullModels.EnglishV5, config =>
            {
                // TODO detect from cpu/gpu architecture
                config.OneDnnEnabled = true;
                config.CpuMathThreadCount = 8;
                config.UseGpu = true;
            })
            {
                AllowRotateDetection = false,
                Enable180Classification = false,
            };
            return Task.CompletedTask;
        }

        public Task<IEnumerable<OCRResult>> ReadTextAsync(byte[] imageBytes)
        {
            using Mat image = Cv2.ImDecode(imageBytes, ImreadModes.Color);
            var results = ocr.Run(image).Regions.Select(r => new OCRResult(r.Text, r.Score));
            return Task.FromResult(results);
        }

        public void Dispose()
        {
            ocr.Dispose();
        }
    }
}
