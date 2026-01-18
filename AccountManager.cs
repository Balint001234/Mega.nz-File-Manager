using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace FileShare
{
    public class SavedAccount
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        public string AccountName { get; set; } = "";
        public DateTime LastUsed { get; set; }
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
            var existingAccount = _accounts.FirstOrDefault(a => a.Email == email);
            if (existingAccount != null)
            {
                existingAccount.Password = password;
                existingAccount.LastUsed = DateTime.Now;
            }
            else
            {
                var newAccount = new SavedAccount
                {
                    Email = email,
                    Password = password,
                    AccountName = $"login_{_accounts.Count + 1}",
                    LastUsed = DateTime.Now
                };
                _accounts.Add(newAccount);
            }
            SaveAccounts();
        }

        public static void UpdateLastUsed(string email)
        {
            var account = _accounts.FirstOrDefault(a => a.Email == email);
            if (account != null)
            {
                account.LastUsed = DateTime.Now;
                SaveAccounts();
            }
        }

        public static void DeleteAccount(string email)
        {
            _accounts.RemoveAll(a => a.Email == email);
            SaveAccounts();
        }

        public static SavedAccount? GetAccount(string email)
        {
            return _accounts.FirstOrDefault(a => a.Email == email);
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
            catch
            {
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

                var json = JsonSerializer.Serialize(_accounts, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(AccountsFilePath, json);
            }
            catch { }
        }
    }
}