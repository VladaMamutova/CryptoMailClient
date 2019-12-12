using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace CryptoMailClient.Models
{
    public class User
    {
        public string Login { get; }
        public string PasswordHash { get; }

        public List<EmailAccount> EmailAccounts { get; }
        public EmailAccount CurrentEmailAccount { get; private set; }

        public User(string login, string password)
        {
            Login = login ?? throw new ArgumentNullException(nameof(login));
            if (password == null)
            {
                throw new ArgumentNullException(nameof(password));
            }

            PasswordHash = ComputeHash(password);
            EmailAccounts = new List<EmailAccount>();
        }

        public bool AddEmailAccount(EmailAccount emailAccount)
        {
            bool result = false;
            if (!ContainsEmailAddress(emailAccount.Address))
            {
                EmailAccounts.Add(emailAccount);
                if (CurrentEmailAccount == null)
                {
                    CurrentEmailAccount = emailAccount;
                }
                result = true;
            }

            return result;
        }

        public bool RemoveEmailAccount(string address)
        {
            bool result = false;
            if (ContainsEmailAddress(address))
            {
                EmailAccounts.RemoveAll(e => e.Address == address);
                if (address == CurrentEmailAccount?.Address)
                {
                    CurrentEmailAccount = EmailAccounts.Count > 0
                        ? EmailAccounts[0]
                        : null;
                }
                result = true;
            }

            return result;
        }

        public bool RemoveEmailAccount(EmailAccount emailAccount)
        {
            bool result = EmailAccounts.Remove(emailAccount);
            if (result)
            {
                if (CurrentEmailAccount.Equals(emailAccount))
                {
                    CurrentEmailAccount =
                        EmailAccounts.Count > 0 ? EmailAccounts[0] : null;
                }
            }

            return EmailAccounts.Remove(emailAccount);
        }

        public bool SetCurrentEmailAccount(string address)
        {
            bool result = false;
            if (ContainsEmailAddress(address))
            {
                CurrentEmailAccount =
                    EmailAccounts.Find(e => e.Address == address);
                result = true;
            }

            return result;
        }

        private bool ContainsEmailAddress(string address)
        {
            return EmailAccounts.Exists(e => e.Address == address);
        }

        private string ComputeHash(string data)
        {
            byte[] hash;
            using (MD5 md5 = MD5.Create())
            {
                hash = md5.ComputeHash(Encoding.Unicode.GetBytes(data));
            }

            return BitConverter.ToString(hash);
        }

        public override string ToString()
        {
            return Login;
        }
    }
}
