using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using CG.Web.MegaApiClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FileShare
{
    public class MegaFileItem
    {
        public string Icon { get; set; } = "📄";
        public string Name { get; set; } = "";
        public long Size { get; set; }
        public string SizeFormatted 
        { 
            get 
            {
                if (Size == 0) return "-";
                string[] sizes = { "B", "KB", "MB", "GB", "TB" };
                int order = 0;
                double len = Size;
                while (len >= 1024 && order < sizes.Length - 1)
                {
                    order++;
                    len /= 1024;
                }
                return $"{len:0.##} {sizes[order]}";
            }
        }
        public string Type { get; set; } = "";
        public string ModifiedDate { get; set; } = "";
        public INode Node { get; set; } = null!;
    }

    public partial class MainWindow : Window
    {
        private IMegaApiClient? _megaClient;
        private string _currentEmail = "";
        private string _currentPassword = "";
        private List<MegaFileItem> _fileItems = new List<MegaFileItem>();
        private List<SavedAccount> _availableAccounts = new List<SavedAccount>();

        public MainWindow(string email, string password)
        {
            InitializeComponent();
            _currentEmail = email;
            _currentPassword = password;
            Title = $"MEGA Client - {email}";
            InitializeAccounts();
            InitializeMegaClient(email, password);
        }

        private void InitializeAccounts()
        {
            _availableAccounts = AccountManager.GetAccounts();
            AccountComboBox.ItemsSource = _availableAccounts;
            
            var currentAccount = _availableAccounts.FirstOrDefault(a => a.Email == _currentEmail);
            if (currentAccount != null)
            {
                AccountComboBox.SelectedItem = currentAccount;
            }
            
            UpdateAccountDisplay();
        }

        private async void InitializeMegaClient(string email, string password)
        {
            try
            {
                StatusTextBlock.Text = "Connecting to MEGA...";
                
                // Dispose old client if exists
                if (_megaClient != null && _megaClient.IsLoggedIn)
                {
                    try { await _megaClient.LogoutAsync(); } catch { }
                    (_megaClient as IDisposable)?.Dispose();
                }
                
                _megaClient = new MegaApiClient();
                await _megaClient.LoginAsync(email, password);
                
                _currentEmail = email;
                _currentPassword = password;
                AccountManager.UpdateLastUsed(email);
                UpdateAccountDisplay();
                
                StatusTextBlock.Text = $"Connected as {email}";
                await LoadFiles();
            }
            catch (Exception ex)
            {
                await ShowErrorMessage($"Login failed: {ex.Message}");
                // Don't close, just show error
                StatusTextBlock.Text = $"Login failed: {ex.Message}";
            }
        }

        private async Task LoadFiles()
        {
            try
            {
                if (_megaClient == null || !_megaClient.IsLoggedIn) return;
                
                StatusTextBlock.Text = "Loading files...";
                
                var nodes = await _megaClient.GetNodesAsync();
                _fileItems.Clear();
                
                foreach (var node in nodes)
                {
                    if (node.Type == NodeType.File || node.Type == NodeType.Directory)
                    {
                        var item = new MegaFileItem
                        {
                            Name = node.Name ?? "Unnamed",
                            Size = node.Type == NodeType.File ? node.Size : 0,
                            Type = node.Type == NodeType.File ? "File" : "Folder",
                            ModifiedDate = node.ModificationDate?.ToString("yyyy-MM-dd HH:mm") ?? "N/A",
                            Node = node
                        };
                        
                        item.Icon = node.Type == NodeType.File ? GetFileIcon(node.Name) : "📁";
                        _fileItems.Add(item);
                    }
                }
                
                _fileItems = _fileItems
                    .OrderByDescending(f => f.Type == "Folder")
                    .ThenBy(f => f.Name)
                    .ToList();
                
                FilesListBox.ItemsSource = _fileItems;
                EmptyStateText.IsVisible = _fileItems.Count == 0;
                
                StatusTextBlock.Text = $"Loaded {_fileItems.Count} items";
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Error loading files: {ex.Message}";
                await ShowErrorMessage($"Error loading files: {ex.Message}");
            }
        }

        private async void AccountComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AccountComboBox.SelectedItem is SavedAccount selectedAccount && 
                selectedAccount.Email != _currentEmail)
            {
                // Switch account in place
                await SwitchAccount(selectedAccount.Email, selectedAccount.Password);
            }
        }

        private async Task SwitchAccount(string email, string password)
        {
            try
            {
                OperationProgressBar.IsVisible = true;
                OperationProgressBar.Value = 0;
                StatusTextBlock.Text = $"Switching to {email}...";
                
                await Task.Delay(100);
                
                // Reinitialize with new credentials
                InitializeMegaClient(email, password);
                
                OperationProgressBar.Value = 100;
                await Task.Delay(300);
            }
            catch (Exception ex)
            {
                await ShowErrorMessage($"Failed to switch account: {ex.Message}");
            }
            finally
            {
                OperationProgressBar.IsVisible = false;
            }
        }

        private void UpdateAccountDisplay()
        {
            CurrentAccountText.Text = $"Logged in as: {_currentEmail}";
            Title = $"MEGA Client - {_currentEmail}";
        }

        private string GetFileIcon(string? fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return "📄";
            
            try
            {
                var ext = Path.GetExtension(fileName).ToLower();
                
                return ext switch
                {
                    ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".tiff" or ".webp" or ".svg" => "🖼️",
                    ".pdf" => "📕",
                    ".doc" or ".docx" or ".odt" or ".rtf" => "📄",
                    ".xls" or ".xlsx" or ".ods" => "📊",
                    ".ppt" or ".pptx" or ".odp" => "📽️",
                    ".txt" or ".md" or ".log" => "📝",
                    ".zip" or ".rar" or ".7z" or ".tar" or ".gz" or ".bz2" => "📦",
                    ".mp3" or ".wav" or ".flac" or ".aac" or ".ogg" or ".m4a" => "🎵",
                    ".mp4" or ".avi" or ".mkv" or ".mov" or ".wmv" or ".flv" or ".webm" => "🎬",
                    ".exe" or ".msi" => "⚙️",
                    ".bat" or ".sh" or ".ps1" => "💻",
                    ".db" or ".sqlite" or ".mdb" => "🗃️",
                    ".cs" or ".cpp" or ".h" or ".java" or ".py" or ".js" or ".html" or ".css" or ".xml" or ".json" => "📋",
                    _ => "📄"
                };
            }
            catch
            {
                return "📄";
            }
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadFiles();
        }

        private async void UploadButton_Click(object sender, RoutedEventArgs e)
        {
            if (_megaClient == null) return;
            
            var storageProvider = GetStorageProvider();
            if (storageProvider == null) return;
            
            var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select file to upload",
                AllowMultiple = false
            });
            
            if (files.Count > 0 && files[0] != null)
            {
                var file = files[0];
                var filePath = file.Path.LocalPath;
                
                if (!File.Exists(filePath))
                {
                    await ShowErrorMessage("File does not exist");
                    return;
                }

                OperationProgressBar.IsVisible = true;
                OperationProgressBar.Value = 0;
                StatusTextBlock.Text = $"Uploading {file.Name}...";
                
                try
                {
                    var progress = new Progress<double>(percent =>
                    {
                        OperationProgressBar.Value = percent;
                    });
                    
                    var nodes = await _megaClient.GetNodesAsync();
                    var root = nodes.First(x => x.Type == NodeType.Root);
                    
                    using (var fileStream = File.OpenRead(filePath))
                    {
                        await _megaClient.UploadAsync(fileStream, file.Name, root, progress);
                    }
                    
                    StatusTextBlock.Text = $"{file.Name} uploaded successfully";
                    await LoadFiles();
                }
                catch (Exception ex)
                {
                    await ShowErrorMessage($"Upload failed: {ex.Message}");
                }
                finally
                {
                    OperationProgressBar.IsVisible = false;
                }
            }
        }

        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            if (_megaClient == null) return;
            
            if (FilesListBox.SelectedItem is MegaFileItem selectedItem)
            {
                if (selectedItem.Node.Type != NodeType.File)
                {
                    await ShowErrorMessage("Please select a file (not a folder)");
                    return;
                }

                var storageProvider = GetStorageProvider();
                if (storageProvider == null) return;
                
                var file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = "Save file",
                    SuggestedFileName = selectedItem.Name,
                    DefaultExtension = Path.GetExtension(selectedItem.Name)
                });
                
                if (file != null)
                {
                    var filePath = file.Path.LocalPath;
                    
                    OperationProgressBar.IsVisible = true;
                    OperationProgressBar.Value = 0;
                    StatusTextBlock.Text = $"Downloading {selectedItem.Name}...";
                    
                    try
                    {
                        var progress = new Progress<double>(percent =>
                        {
                            OperationProgressBar.Value = percent;
                        });
                        
                        await _megaClient.DownloadFileAsync(selectedItem.Node, filePath, progress);
                        StatusTextBlock.Text = $"{selectedItem.Name} downloaded successfully";
                    }
                    catch (Exception ex)
                    {
                        await ShowErrorMessage($"Download failed: {ex.Message}");
                    }
                    finally
                    {
                        OperationProgressBar.IsVisible = false;
                    }
                }
            }
            else
            {
                await ShowErrorMessage("Please select a file to download");
            }
        }

        private async void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_megaClient != null && _megaClient.IsLoggedIn)
                {
                    await _megaClient.LogoutAsync();
                }
            }
            catch { }
            
            // Show login window again
            var loginWindow = new LoginWindow();
            loginWindow.LoginSuccessful += (email, password) =>
            {
                // Reinitialize with new credentials
                InitializeMegaClient(email, password);
                loginWindow.Close();
            };
            
            loginWindow.Show();
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
                            Text = "❌",
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

        private IStorageProvider? GetStorageProvider()
        {
            return TopLevel.GetTopLevel(this)?.StorageProvider;
        }

        protected override void OnClosed(EventArgs e)
        {
            if (_megaClient != null && _megaClient.IsLoggedIn)
            {
                try { _megaClient.Logout(); } catch { }
            }
            (_megaClient as IDisposable)?.Dispose();
            base.OnClosed(e);
        }
    }
}