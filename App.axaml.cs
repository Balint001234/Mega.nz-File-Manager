using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace FileShare
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                ShowLoginWindow();
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void ShowLoginWindow()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var loginWindow = new LoginWindow();
                loginWindow.LoginSuccessful += (email, password) =>
                {
                    var mainWindow = new MainWindow(email, password);
                    desktop.MainWindow = mainWindow;
                    mainWindow.Show();
                    loginWindow.Close();
                };
                
                desktop.MainWindow = loginWindow;
                loginWindow.Show();
            }
        }
    }
}