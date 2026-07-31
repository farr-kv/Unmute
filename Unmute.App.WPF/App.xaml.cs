using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System.Windows;
using Unmute.App.WPF.UI.SystemTray;
using Unmute.App.WPF.UI.Windows.Overlay;
using Unmute.App.WPF.UI.Windows.Settings;
using Unmute.App.WPF.UI.Windows.Splash;
using Unmute.Core.Extensions;
using Unmute.OCR.PaddleOCR.Extensions;
using Unmute.TTS.PocketTTS.Extensions;

namespace Unmute.App.WPF
{
    public partial class App : Application
    {
        private IServiceProvider? serviceProvider;
        private TaskbarIcon? taskbarIcon;

        protected override void OnStartup(StartupEventArgs e)
        {
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            serviceProvider = serviceCollection.BuildServiceProvider();

            this.DispatcherUnhandledException += (_, e) => this.OnUnhandledException(e.Exception);
            AppDomain.CurrentDomain.UnhandledException += (_, e) => this.OnUnhandledException((Exception)e.ExceptionObject);
            TaskScheduler.UnobservedTaskException += (_, e) => this.OnUnhandledException(e.Exception);

            var window = serviceProvider.GetRequiredService<SplashWindow>();
            window.Show();

            taskbarIcon = (TaskbarIcon)FindResource("NotifyIcon");
            taskbarIcon.DataContext = serviceProvider.GetRequiredService<SystemTrayViewModel>();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            var fileLogger = new LoggerConfiguration()
                .WriteTo.File(@"Logs\.log", rollingInterval: RollingInterval.Day, retainedFileTimeLimit: TimeSpan.FromDays(14), rollOnFileSizeLimit: true)
                .CreateLogger();

            services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(fileLogger, dispose: true));

            // Services
            services.AddUnmuteCore()
                    .UsePaddleOCR()
                    .UsePocketTTS();

            // Windows
            services.AddTransient<SplashWindow>();
            services.AddTransient<SettingsWindow>();
            services.AddTransient<OverlayWindow>();

            services.AddSingleton<SystemTrayViewModel>();
        }

        private void OnExit(object sender, ExitEventArgs e)
        {
            taskbarIcon?.Dispose();

            if (serviceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        private void OnUnhandledException(Exception e)
        {
            serviceProvider?.GetRequiredService<ILogger<App>>()?.LogError("Unexpected exception: {0}", e);
        }
    }
}
