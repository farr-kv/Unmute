using Microsoft.Extensions.DependencyInjection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using Unmute.App.WPF.Extensions;
using Unmute.App.WPF.Interops;
using Unmute.App.WPF.UI.Windows.Settings;
using Unmute.Core.Services;

namespace Unmute.App.WPF.UI.Windows.Overlay
{
    public partial class OverlayWindow : Window
    {
        private readonly IServiceProvider serviceProvider;
        private readonly IOCREngine ocrEngine;
        private readonly ITtsService ttsService;
        private ulong currentPHash;

        public OverlayWindow(IServiceProvider serviceProvider, IOCREngine ocrEngine, ITtsService ttsService)
        {
            this.serviceProvider = serviceProvider;
            this.ocrEngine = ocrEngine;
            this.ttsService = ttsService;

            Left = SystemParameters.VirtualScreenLeft;
            Top = SystemParameters.VirtualScreenTop;
            Width = SystemParameters.VirtualScreenWidth;
            Height = SystemParameters.VirtualScreenHeight;

            InitializeComponent();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            this.SetNoActivate();
        }

        private void OnClick_Close(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void OnClick_Settings(object sender, RoutedEventArgs e)
        {
            var settingsWindow = this.serviceProvider.GetRequiredService<SettingsWindow>();
            settingsWindow.Show();
            settingsWindow.Focus(); // Required because of SetNoActivate
        }

        private async void OnClick_ParseScreen(object sender, RoutedEventArgs e)
        {
            var screenshot = this.CaptureScreenshot();
            var phash = screenshot.GetPerceptualHash();
            var diff = this.HammingDistance(phash, currentPHash);
            if (diff < 2)
                return;

            this.currentPHash = phash;
            var results = await this.ocrEngine.ReadTextAsync(screenshot);
            this.AnnotationContainer.Children.Clear();
            var style = (Style)FindResource("NarrationButtonStyle");
            foreach (var result in results)
            {
                // TODO replace with model xml declaration
                var button = new Button();
                button.Style = style;
                button.Width = result.Bounds.Width;
                button.Height = result.Bounds.Height;
                Canvas.SetLeft(button, result.Bounds.Left);
                Canvas.SetTop(button, result.Bounds.Top);

                button.Click += async (s, e) => {
                    await ttsService.NarrateAsync(result.Text);
                };
                this.AnnotationContainer.Children.Add(button);
            }
        }

        private System.Drawing.Bitmap CaptureScreenshot()
        {
            var bmp = new System.Drawing.Bitmap((int)this.Width, (int)this.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            using (var g = System.Drawing.Graphics.FromImage(bmp))
            {
                g.CopyFromScreen(
                    (int)this.Left,
                    (int)this.Top,
                    0,
                    0,
                    new System.Drawing.Size((int)this.Width, (int)this.Height));
            }
            return bmp;
        }

        private int HammingDistance(ulong a, ulong b)
        {
            ulong value = a ^ b;
            int count = 0;

            while (value != 0)
            {
                value &= value - 1;
                count++;
            }

            return count;
        }
    }
}
