using AdonisUI.Controls;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using Unmute.App.WPF.Extensions;
using Unmute.App.WPF.UI.Windows.Overlay;
using Unmute.Core.Services;
using Unmute.OCR.Services;

namespace Unmute.App.WPF.UI.Windows.Splash
{
    public partial class SplashWindow : AdonisWindow, INotifyPropertyChanged
    {
        private readonly IServiceProvider serviceProvider;

        public SplashWindow(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
            this.InitializeComponent();
        }

        public void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            Task.Run(async () =>
            {
                try
                {
                    var ocrService = serviceProvider.GetRequiredService<IOcrService>();
                    await ocrService.InitializeAsync(OCR.OcrEngineType.Tesseract);

                    var ttsService = serviceProvider.GetRequiredService<ITtsService>();
                    await ttsService.InitializeAsync();
                    await ttsService.StartAsync();
                    
                    this.RunOnUiThread(() => {
                        var nextWindow = serviceProvider.GetRequiredService<OverlayWindow>();
                        nextWindow.Show();
                    });
                }
                finally
                {
                    this.RunOnUiThread(() => this.Close()); 
                }
            });
        }

        private void Titlebar_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        #endregion
    }
}
