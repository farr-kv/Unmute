using AdonisUI.Controls;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Windows;
using Unmute.App.WPF.Extensions;
using Unmute.App.WPF.UI.Windows.Settings;
using Unmute.Core.Services;

namespace Unmute.App.WPF.UI.Windows.Splash
{
    public partial class SplashWindow : AdonisWindow, INotifyPropertyChanged
    {
        private readonly IServiceProvider serviceProvider;

        public bool Loading { get; private set; } = true;
        public event PropertyChangedEventHandler? PropertyChanged;

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
                    var ocrEngine = serviceProvider.GetRequiredService<IOCREngine>();
                    await ocrEngine.InitializeAsync();

                    var ttsService = serviceProvider.GetRequiredService<ITtsService>();
                    await ttsService.InitializeAsync();
                    await ttsService.StartAsync();
                    
                    this.RunOnUiThread(() => {
                        var nextWindow = serviceProvider.GetRequiredService<SettingsWindow>();
                        nextWindow.Show();
                    });
                }
                finally
                {
                    Loading = false;
                    this.RunOnUiThread(() => this.Close()); 
                }
            });
        }

        private void Titlebar_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.DragMove();
        }
    }
}
