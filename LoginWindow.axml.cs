using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Threading.Tasks;

namespace FileShare
{
    public partial class LoginWindow : Window
    {
        public event Action<string, string>? LoginSuccessful;

        public LoginWindow()
        {
            InitializeComponent();
            LoadSavedAccounts();
        }

        private void LoadSavedAccounts()
        {
            var accounts = AccountManager.GetAccounts();
            SavedAccountsListBox.ItemsSource = accounts;
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(EmailTextBox.Text) || 
                string.IsNullOrWhiteSpace(PasswordTextBox.Text))
            {
                await ShowErrorMessage("Please enter both email and password");
                return;
            }

            LoginButton.IsVisible = false;
            LoginProgressBar.IsVisible = true;
            LoginProgressBar.Value = 30;

            await Task.Delay(800);
            LoginProgressBar.Value = 100;
            await Task.Delay(200);

            if (SaveAccountCheckBox.IsChecked == true)
            {
                AccountManager.SaveAccount(EmailTextBox.Text, PasswordTextBox.Text);
            }

            LoginSuccessful?.Invoke(EmailTextBox.Text, PasswordTextBox.Text);
        }

        private void SavedAccountsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SavedAccountsListBox.SelectedItem is SavedAccount account)
            {
                EmailTextBox.Text = account.Email;
                PasswordTextBox.Text = account.Password;
                SaveAccountCheckBox.IsChecked = false;
            }
        }

        private async Task ShowErrorMessage(string message)
        {
            var dialog = new Window
            {
                Title = "Error",
                Content = new StackPanel
                {
                    Children = 
                    {
                        new TextBlock 
                        { 
                            Text = "⚠️",
                            FontSize = 24,
                            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                            Margin = new Thickness(0, 0, 0, 10)
                        },
                        new TextBlock 
                        { 
                            Text = message,
                            Margin = new Thickness(20),
                            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                            MaxWidth = 300
                        }
                    }
                },
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            
            await dialog.ShowDialog(this);
        }
    }
}