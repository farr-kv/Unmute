using System.Drawing;
using System.Drawing.Drawing2D;

namespace Unmute.App.WPF.Extensions
{
    internal static class BitmapExtension
    {   public static ulong GetPerceptualHash(this Bitmap bitmap)
        {
            const int Size = 32;
            const int SmallerSize = 8;

            // Resize
            using var resized = new Bitmap(Size, Size);
            using (var g = Graphics.FromImage(resized))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(bitmap, 0, 0, Size, Size);
            }

            // Grayscale
            double[,] pixels = new double[Size, Size];

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    Color c = resized.GetPixel(x, y);
                    pixels[x, y] =
                        c.R * 0.299 +
                        c.G * 0.587 +
                        c.B * 0.114;
                }
            }

            // DCT
            double[,] dct = DCT2D(pixels);

            // Collect top-left 8x8 (excluding DC)
            List<double> values = new();

            for (int y = 0; y < SmallerSize; y++)
            {
                for (int x = 0; x < SmallerSize; x++)
                {
                    if (x == 0 && y == 0)
                        continue;

                    values.Add(dct[x, y]);
                }
            }

            values.Sort();
            double median = values[values.Count / 2];

            ulong hash = 0;
            int bit = 0;

            for (int y = 0; y < SmallerSize; y++)
            {
                for (int x = 0; x < SmallerSize; x++)
                {
                    if (x == 0 && y == 0)
                        continue;

                    if (dct[x, y] > median)
                        hash |= 1UL << bit;

                    bit++;
                }
            }

            return hash;
        }

        private static double[,] DCT2D(double[,] input)
        {
            int N = input.GetLength(0);
            double[,] output = new double[N, N];

            double c1 = Math.PI / (2.0 * N);

            for (int u = 0; u < N; u++)
            {
                double cu = u == 0 ? Math.Sqrt(1.0 / N) : Math.Sqrt(2.0 / N);

                for (int v = 0; v < N; v++)
                {
                    double cv = v == 0 ? Math.Sqrt(1.0 / N) : Math.Sqrt(2.0 / N);

                    double sum = 0;

                    for (int x = 0; x < N; x++)
                    {
                        double cos1 = Math.Cos((2 * x + 1) * u * c1);

                        for (int y = 0; y < N; y++)
                        {
                            double cos2 = Math.Cos((2 * y + 1) * v * c1);

                            sum += input[x, y] * cos1 * cos2;
                        }
                    }

                    output[u, v] = cu * cv * sum;
                }
            }

            return output;
        }
    }
}
