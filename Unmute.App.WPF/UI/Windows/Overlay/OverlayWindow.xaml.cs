using AdonisUI.Controls;
using System.Windows;
using System.Windows.Controls;
using Unmute.App.WPF.Extensions;
using Unmute.App.WPF.Interops;
using Unmute.Core;
using Unmute.Core.Services;

namespace Unmute.App.WPF.UI.Windows.Overlay
{
    public partial class OverlayWindow : AdonisWindow
    {
        private readonly IScreenCaptureService screenCapture;
        private readonly IOCREngine ocrEngine;
        private readonly ITtsService ttsService;
        private ulong currentHash;

        private IDisposable? refreshTask;
        private bool IsEnabled => refreshTask is not null;

        public int StartMenuHeight => (int)(SystemParameters.VirtualScreenHeight - SystemParameters.WorkArea.Bottom);

        public OverlayWindow(IScreenCaptureService screenCapture, IOCREngine ocrEngine, ITtsService ttsService)
        {
            this.screenCapture = screenCapture;
            this.ocrEngine = ocrEngine;
            this.ttsService = ttsService;            

            Left = SystemParameters.VirtualScreenLeft;
            Top = SystemParameters.VirtualScreenTop;
            Width = SystemParameters.VirtualScreenWidth;
            Height = SystemParameters.VirtualScreenHeight;

            // TODO add an "is reading" indicator
            // TODO add voice changing on mouse wheel

            InitializeComponent();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            this.SetNoActivate();
            this.SetExcludeFromCapture();
        }

        private void OnClick_Disable(object sender, RoutedEventArgs e)
        {
            this.refreshTask?.Dispose();
            this.refreshTask = null;
            this.AnnotationContainer.Children.Clear();
        }

        private async void OnClick_Enable(object sender, RoutedEventArgs e)
        {
            if (!IsEnabled)
                refreshTask = PeriodicTask.Run(TimeSpan.FromSeconds(0.5), ProcessScreen);
        }

        private async Task ProcessScreen()
        {
            using var screenshot = this.screenCapture.CaptureFrame();
            var phash = screenshot.GetPerceptualHash();
            var diff = this.HammingDistance(phash, currentHash);
            if (diff < 1.15f) // TODO make this configurable
                return;

            this.currentHash = phash;
            var results = await this.ocrEngine.ReadTextAsync(screenshot);

            this.RunOnUiThread(() =>
            {
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

                    button.Click += (s, e) => Task.Run(() => ttsService?.NarrateAsync(result.Text));
                    this.AnnotationContainer.Children.Add(button);
                }
            });
        }

        private int HammingDistance(ulong a, ulong b)
        {
            ulong x = a ^ b;
            int count = 0;

            while (x != 0)
            {
                count++;
                x &= x - 1;
            }

            return count;
        }
    }
}
