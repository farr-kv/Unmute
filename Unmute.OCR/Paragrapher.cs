using Unmute.Core.Models;

namespace Unmute.OCR
{
    internal static class Paragrapher
    {
        internal static IEnumerable<OCRResult> MergeParagraphs(IEnumerable<OCRResult> regions)
        {
            var results = new List<InternalOCRResult>();
            foreach (var next in regions.Select(ToInternalResult).OrderBy(x => x.Bounds.Top))
            {
                var current = results.LastOrDefault();
                if (current is not null &&
                    AreSameFontSize(current, next) &&
                    AreLeftAligned(current, next) &&
                    AreBelow(current, next))
                {
                    current.Text += Environment.NewLine + next.Text;
                    current.Bounds.Left = Math.Min(current.Bounds.Left, next.Bounds.Left);
                    current.Bounds.Top = Math.Min(current.Bounds.Top, next.Bounds.Top);
                    current.Bounds.Right = Math.Max(current.Bounds.Right, next.Bounds.Right);
                    current.Bounds.Bottom = Math.Max(current.Bounds.Bottom, next.Bounds.Bottom);
                }
                else
                {
                    results.Add(next);
                }
            }
            return results;
        }

        private static bool AreSameFontSize(InternalOCRResult current, InternalOCRResult next)
        {
            // Lines shouldn't be more than 10% larger than the next
            var threshold = Math.Max(current.FontSize, next.FontSize) * 0.2f;
            return Math.Abs(current.FontSize - next.FontSize) < threshold;
        }

        private static bool AreLeftAligned(InternalOCRResult current, InternalOCRResult next)
        {
            // Lines should follow the same indentation
            var threshold = Math.Max(current.FontSize, next.FontSize) * 0.5f;
            return Math.Abs(current.Bounds.Left - next.Bounds.Left) < threshold;
        }

        private static bool AreBelow(InternalOCRResult current, InternalOCRResult next)
        {
            // There should not be a character's height of space between lines
            var threshold = Math.Max(current.FontSize, next.FontSize) * 0.6f;
            return Math.Abs(current.Bounds.Bottom - next.Bounds.Top) < threshold;
        }

        private static InternalOCRResult ToInternalResult(OCRResult result)
        {
            return new InternalOCRResult
            {
                Text = result.Text,
                Bounds = result.Bounds,
                FontSize = result.Bounds.Bottom - result.Bounds.Top
            };
        }

        private class InternalOCRResult : OCRResult
        {
            public int FontSize { get; set; }
        }
    }
}
