using Avalonia;
using Avalonia.Controls;
using System.Threading.Tasks;

namespace FileShare
{
    public static class MessageBox
    {
        public static async Task Show(Window parent, string message, string title)
        {
            var dialog = new Window
            {
                Title = title,
                Content = new TextBlock { 
                    Text = message, 
                    Margin = new Thickness(20),
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap
                },
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            
            await dialog.ShowDialog(parent);
        }
    }
}