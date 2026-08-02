using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Input;
using Unmute.App.WPF.UI.Windows.Settings;

namespace Unmute.App.WPF.UI.SystemTray
{
    public class SystemTrayViewModel
    {
        private readonly IServiceProvider serviceProvider;

        // TODO expose visibility property to hide options from menu
        // TODO get status of the screen reading process

        public SystemTrayViewModel(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public ICommand ShowSettingsWindowCommand => new DelegateCommand
        {
            CanExecuteFunc = () => true,
            CommandAction = () =>
            {
                var window = this.serviceProvider.GetRequiredService<SettingsWindow>();
                window.Show();
            }
        };

        public ICommand ExitApplicationCommand => new DelegateCommand 
        {
            CommandAction = Application.Current.Shutdown 
        };

        public ICommand EnableScreenReadingCommand => new DelegateCommand
        {
            CanExecuteFunc = () => false,
            CommandAction = () =>
            {
                // TODO publish event
            }
        };

        public ICommand DisableScreenReadingCommand => new DelegateCommand
        {
            CanExecuteFunc = () => false,
            CommandAction = () =>
            {
                // TODO publish event
            }
        };
    }
}
