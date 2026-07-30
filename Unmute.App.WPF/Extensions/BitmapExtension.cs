using System.Drawing;

namespace Unmute.App.WPF.Extensions
{
    internal static class BitmapExtension
    {
        public static ulong GetPerceptualHash(this Bitmap instance)
        {
            const int Size = 32;
            const int SmallerSize = 8;

            // Resize
            using var resized = new Bitmap(instance, new Size(Size, Size));

            // Grayscale
            double[,] pixels = new double[Size, Size];

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    Color c = resized.GetPixel(x, y);

                    pixels[x, y] =
                        0.299 * c.R +
                        0.587 * c.G +
                        0.114 * c.B;
                }
            }

            // DCT
            double[,] dct = DCT2D(pixels);

            // Collect low-frequency coefficients (excluding DC)
            double[] values = new double[63];
            int index = 0;

            for (int y = 0; y < SmallerSize; y++)
            {
                for (int x = 0; x < SmallerSize; x++)
                {
                    if (x == 0 && y == 0)
                        continue;

                    values[index++] = dct[x, y];
                }
            }

            Array.Sort(values);
            double median = values[31];

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

            for (int u = 0; u < N; u++)
            {
                for (int v = 0; v < N; v++)
                {
                    double sum = 0;

                    for (int x = 0; x < N; x++)
                    {
                        for (int y = 0; y < N; y++)
                        {
                            sum +=
                                input[x, y] *
                                Math.Cos((2 * x + 1) * u * Math.PI / (2 * N)) *
                                Math.Cos((2 * y + 1) * v * Math.PI / (2 * N));
                        }
                    }

                    double cu = (u == 0) ? 1 / Math.Sqrt(2) : 1;
                    double cv = (v == 0) ? 1 / Math.Sqrt(2) : 1;

                    output[u, v] =
                        0.25 *
                        cu *
                        cv *
                        sum;
                }
            }

            return output;
        }
    }
}
