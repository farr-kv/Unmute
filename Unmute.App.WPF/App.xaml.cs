using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System.Windows;
using Unmute.App.WPF.UI.Windows.Splash;
using Unmute.App.WPF.UI.Windows.Settings;
using Unmute.Core.Extensions;
using Unmute.TTS.PocketTTS.Extensions;
using Unmute.OCR.PaddleOCR.Extensions;

namespace Unmute.App.WPF
{
    public partial class App : Application
    {
        private IServiceProvider? serviceProvider;

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
            services.AddSingleton<SplashWindow>();
            services.AddSingleton<SettingsWindow>();
        }

        private void OnExit(object sender, ExitEventArgs e)
        {
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
