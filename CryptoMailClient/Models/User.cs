using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace CryptoMailClient.Models
{
    public class User
    {
        private Dictionary<string, EmailAccount> _emailAccounts;

        public string Login { get; }
        public string PasswordHash { get; }

        public User(string login, string password)
        {
            if (string.IsNullOrWhiteSpace(login))
            {
                throw new ArgumentException(nameof(login));
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException(nameof(password));
            }

            Login = login;
            PasswordHash = ComputeHash(password);
            _emailAccounts = new Dictionary<string, EmailAccount>();
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
    }
}
