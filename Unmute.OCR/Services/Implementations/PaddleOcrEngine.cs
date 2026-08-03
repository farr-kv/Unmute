using OpenCvSharp;
using OpenCvSharp.Extensions;
using Sdcb.PaddleOCR;
using Sdcb.PaddleOCR.Models.Local;
using System.Drawing;
using Unmute.Core.Models;

namespace Unmute.OCR.Services.Implementations
{
    internal sealed class PaddleOcrEngine : IOcrEngine
    {
        private PaddleOcrAll ocr;

        public PaddleOcrEngine()
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

        public Task<IEnumerable<OCRResult>> ReadTextAsync(Bitmap bitmap)
        {
            using Mat image = BitmapConverter.ToMat(bitmap);
            if (image.Channels() == 4)
                Cv2.CvtColor(image, image, ColorConversionCodes.BGRA2BGR);

            var results = ocr.Run(image).Regions
                .Where(r => r.Score > 0.5f)
                .Select(r =>
            {
                var rect = r.Rect.BoundingRect();
                return new OCRResult
                {
                    Text = r.Text,
                    Bounds = new Bounds
                    {
                        Left = rect.Left,
                        Right = rect.Right,
                        Top = rect.Top,
                        Bottom = rect.Bottom
                    }
                };
            });

            return Task.FromResult(Paragrapher.MergeParagraphs(results));
        }

        public void Dispose()
        {
            ocr.Dispose();
        }
    }
}
