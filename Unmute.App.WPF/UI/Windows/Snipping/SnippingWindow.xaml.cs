using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Unmute.App.WPF.UI.Windows.Snipping
{
    public partial class SnippingWindow : Window
    {
        public static Task<System.Drawing.Bitmap?> TakeScreenshotAsync()
        {
            var window = new SnippingWindow();
            window.Show();

            var tcs = new TaskCompletionSource<System.Drawing.Bitmap?>();
            window.Closed += (_, _) =>
            {
                tcs.SetResult(window.Result);
            };
            return tcs.Task;
        }

        private System.Drawing.Bitmap? Result { get; set; }
        private Point startPoint;
        private bool selecting;

        private SnippingWindow()
        {
            Left = SystemParameters.VirtualScreenLeft;
            Top = SystemParameters.VirtualScreenTop;
            Width = SystemParameters.VirtualScreenWidth;
            Height = SystemParameters.VirtualScreenHeight;

            InitializeComponent();
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
                e.Handled = true;
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            startPoint = e.GetPosition(this);
            selecting = true;

            SelectionBox.Visibility = Visibility.Visible;
            e.Handled = true;
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (!selecting)
                return;

            var current = e.GetPosition(this);

            Canvas.SetLeft(SelectionBox, Math.Min(startPoint.X, current.X));
            Canvas.SetTop(SelectionBox, Math.Min(startPoint.Y, current.Y));

            SelectionBox.Width = Math.Abs(current.X - startPoint.X);
            SelectionBox.Height = Math.Abs(current.Y - startPoint.Y);
            e.Handled = true;
        }

        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            selecting = false;

            var area = GetSelectedScreenArea();
            this.Result = CaptureRegion(area);
            this.Close();
            e.Handled = true;
        }

        private System.Drawing.Rectangle GetSelectedScreenArea()
        {
            var left = Canvas.GetLeft(SelectionBox);
            var top = Canvas.GetTop(SelectionBox);

            if (double.IsNaN(left))
                left = 0;

            if (double.IsNaN(top))
                top = 0;

            var screenPoint = PointToScreen(new Point(left, top));

            return new System.Drawing.Rectangle(
                (int)screenPoint.X,
                (int)screenPoint.Y,
                (int)SelectionBox.Width,
                (int)SelectionBox.Height);
        }

        private System.Drawing.Bitmap CaptureRegion(System.Drawing.Rectangle area)
        {
            var bmp = new System.Drawing.Bitmap(area.Width, area.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            using (var g = System.Drawing.Graphics.FromImage(bmp))
            {
                g.CopyFromScreen(
                    area.Left,
                    area.Top,
                    0,
                    0,
                    area.Size);
            }
            return bmp;
        }
    }
}
