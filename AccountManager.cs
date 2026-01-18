using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Security.Cryptography;

namespace FileShare
{
    public class SavedAccount
    {
        public string EncryptedEmail { get; set; } = "";
        public string EncryptedPassword { get; set; } = "";
        public string AccountName { get; set; } = "";
        public DateTime LastUsed { get; set; }

        [JsonIgnore]
        public string Email
        {
            get => EncryptionHelper.DecryptString(EncryptedEmail);
            set => EncryptedEmail = EncryptionHelper.EncryptString(value);
        }

        [JsonIgnore]
        public string Password
        {
            get => EncryptionHelper.DecryptString(EncryptedPassword);
            set => EncryptedPassword = EncryptionHelper.EncryptString(value);
        }
    }

    public static class EncryptionHelper
    {
        private static readonly string KeyFilePath = 
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                        "MegaDesktopClient", "encryption.key");
        
        private static byte[]? _key = null;

        private static void EnsureKeyExists()
        {
            if (_key != null) return;

            var directory = Path.GetDirectoryName(KeyFilePath);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory!);

            if (File.Exists(KeyFilePath))
            {
                try
                {
                    _key = File.ReadAllBytes(KeyFilePath);
                    if (_key.Length != 32) // Check if key is the correct length
                    {
                        throw new InvalidDataException("Key file is corrupted");
                    }
                }
                catch
                {
                    // If key file is corrupted, generate a new one
                    _key = null;
                }
            }

            if (_key == null)
            {
                // Generate a new 256-bit key
                using var rng = RandomNumberGenerator.Create();
                _key = new byte[32]; // 256 bits
                rng.GetBytes(_key);
                
                // Save the key
                File.WriteAllBytes(KeyFilePath, _key);
                
                // Try to set hidden attribute
                try
                {
                    File.SetAttributes(KeyFilePath, FileAttributes.Hidden);
                }
                catch
                {
                    // Ignore on platforms that don't support this
                }
            }
        }

        public static string EncryptString(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) 
                return "";

            try
            {
                EnsureKeyExists();
                
                using var aes = Aes.Create();
                aes.Key = _key!;
                aes.GenerateIV();
                
                using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                using var ms = new MemoryStream();
                
                // Write IV first
                ms.Write(aes.IV, 0, aes.IV.Length);
                
                using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                using (var sw = new StreamWriter(cs))
                {
                    sw.Write(plainText);
                }
                
                return Convert.ToBase64String(ms.ToArray());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Encryption error: {ex.Message}");
                // Fallback to simple base64 encoding if encryption fails
                return Convert.ToBase64String(Encoding.UTF8.GetBytes(plainText));
            }
        }

        public static string DecryptString(string encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText)) 
                return "";

            try
            {
                EnsureKeyExists();
                
                var fullCipher = Convert.FromBase64String(encryptedText);
                
                // Check if this is our encrypted format (should have IV + ciphertext)
                if (fullCipher.Length >= 16)
                {
                    using var aes = Aes.Create();
                    aes.Key = _key!;
                    
                    // Extract IV from the beginning of the ciphertext
                    var iv = new byte[16];
                    Array.Copy(fullCipher, 0, iv, 0, iv.Length);
                    aes.IV = iv;
                    
                    // The actual ciphertext starts after the IV
                    var cipherText = new byte[fullCipher.Length - iv.Length];
                    Array.Copy(fullCipher, iv.Length, cipherText, 0, cipherText.Length);
                    
                    using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                    using var ms = new MemoryStream(cipherText);
                    using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
                    using var sr = new StreamReader(cs);
                    
                    return sr.ReadToEnd();
                }
                else
                {
                    // Try to decode as simple base64 (for backward compatibility)
                    return Encoding.UTF8.GetString(Convert.FromBase64String(encryptedText));
                }
            }
            catch
            {
                // If decryption fails, try to decode as simple base64
                try
                {
                    return Encoding.UTF8.GetString(Convert.FromBase64String(encryptedText));
                }
                catch
                {
                    return "";
                }
            }
        }
    }

    public static class AccountManager
    {
        private static readonly string AccountsFilePath = 
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                        "MegaDesktopClient", "accounts.json");
        private static List<SavedAccount> _accounts = new List<SavedAccount>();

        static AccountManager()
        {
            LoadAccounts();
        }

        public static List<SavedAccount> GetAccounts()
        {
            return _accounts.OrderByDescending(a => a.LastUsed).ToList();
        }

        public static void SaveAccount(string email, string password)
        {
            // Check if account already exists using decrypted emails
            var existingAccount = _accounts.FirstOrDefault(a => 
            {
                try
                {
                    return EncryptionHelper.DecryptString(a.EncryptedEmail) == email;
                }
                catch
                {
                    return false;
                }
            });

            if (existingAccount != null)
            {
                // Update existing account
                existingAccount.EncryptedPassword = EncryptionHelper.EncryptString(password);
                existingAccount.LastUsed = DateTime.Now;
            }
            else
            {
                // Create new account
                var newAccount = new SavedAccount
                {
                    AccountName = $"login_{_accounts.Count + 1}",
                    LastUsed = DateTime.Now
                };
                
                // Set encrypted values directly
                newAccount.EncryptedEmail = EncryptionHelper.EncryptString(email);
                newAccount.EncryptedPassword = EncryptionHelper.EncryptString(password);
                
                _accounts.Add(newAccount);
            }
            SaveAccounts();
        }

        public static void UpdateLastUsed(string email)
        {
            var account = _accounts.FirstOrDefault(a => 
            {
                try
                {
                    return EncryptionHelper.DecryptString(a.EncryptedEmail) == email;
                }
                catch
                {
                    return false;
                }
            });
            
            if (account != null)
            {
                account.LastUsed = DateTime.Now;
                SaveAccounts();
            }
        }

        public static void DeleteAccount(string email)
        {
            _accounts.RemoveAll(a => 
            {
                try
                {
                    return EncryptionHelper.DecryptString(a.EncryptedEmail) == email;
                }
                catch
                {
                    return false;
                }
            });
            SaveAccounts();
        }

        public static SavedAccount? GetAccount(string email)
        {
            return _accounts.FirstOrDefault(a => 
            {
                try
                {
                    return EncryptionHelper.DecryptString(a.EncryptedEmail) == email;
                }
                catch
                {
                    return false;
                }
            });
        }

        private static void LoadAccounts()
        {
            try
            {
                if (File.Exists(AccountsFilePath))
                {
                    var json = File.ReadAllText(AccountsFilePath);
                    _accounts = JsonSerializer.Deserialize<List<SavedAccount>>(json) ?? new List<SavedAccount>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading accounts: {ex.Message}");
                _accounts = new List<SavedAccount>();
            }
        }

        private static void SaveAccounts()
        {
            try
            {
                var directory = Path.GetDirectoryName(AccountsFilePath);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory!);

                var json = JsonSerializer.Serialize(_accounts, new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
                });
                File.WriteAllText(AccountsFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving accounts: {ex.Message}");
            }
        }

        // Helper method for debugging - prints all accounts
        public static void PrintAllAccounts()
        {
            Console.WriteLine($"Total accounts: {_accounts.Count}");
            foreach (var account in _accounts)
            {
                try
                {
                    var email = EncryptionHelper.DecryptString(account.EncryptedEmail);
                    var password = EncryptionHelper.DecryptString(account.EncryptedPassword);
                    Console.WriteLine($"Account: {account.AccountName}, Email: {email}, Password: {password}, LastUsed: {account.LastUsed}");
                }
                catch
                {
                    Console.WriteLine($"Account: {account.AccountName}, [DECRYPTION ERROR]");
                }
            }
        }
    }
}
