using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CryptoMailClient.Models
{
    public class User
    {
        private int _currentEmailAccountIndex;

        public string Login { get; }
        public string PasswordHash { get; }

        public List<EmailAccount> EmailAccounts { get; }

        public EmailAccount CurrentEmailAccount =>
            _currentEmailAccountIndex >= 0 &&
            _currentEmailAccountIndex < EmailAccounts.Count
                ? EmailAccounts[_currentEmailAccountIndex]
                : null;
        
        public User(string login, string passwordHash)
        {
            Login = login ?? throw new ArgumentNullException(nameof(login));
            PasswordHash = passwordHash ??
                           throw new ArgumentNullException(nameof(passwordHash));
            EmailAccounts = new List<EmailAccount>();
            _currentEmailAccountIndex = -1;
        }

        public static User FromBytes(byte[] bytes)
        {
            User user;
            using (BinaryReader reader =
                new BinaryReader(new MemoryStream(bytes)))
            {
                int loginLength = reader.ReadInt32();
                string login = Encoding.Unicode.GetString(
                    reader.ReadBytes(loginLength));

                int passwordHashLength = reader.ReadInt32();
                string passwordHash = Encoding.Unicode.GetString(
                    reader.ReadBytes(passwordHashLength));

                user = new User(login, passwordHash);

                int emailAccountsCount = reader.ReadInt32();
                for (int i = 0; i < emailAccountsCount; i++)
                {
                    int emailAccountLength = reader.ReadInt32();
                    EmailAccount emailAccount = EmailAccount.FromBytes(
                        reader.ReadBytes(emailAccountLength));
                    user.AddEmailAccount(emailAccount);
                }

                if (user.EmailAccounts.Count > 0)
                {
                    user._currentEmailAccountIndex = reader.ReadInt32();
                }
            }

            return user;
        }

        public static string GetPasswordHashFromFile(string userFile)
        {
            using (BinaryReader reader =
                new BinaryReader(File.Open(userFile, FileMode.Open)))
            {
                int loginLength = reader.ReadInt32();
                reader.BaseStream.Seek(4 + loginLength, SeekOrigin.Begin);

                int passwordHashLength = reader.ReadInt32();
                return Encoding.Unicode.GetString(
                    reader.ReadBytes(passwordHashLength));

            }
        }

        public bool AddEmailAccount(EmailAccount emailAccount)
        {
            bool result = false;
            if (!ContainsEmailAddress(emailAccount.Address))
            {
                EmailAccounts.Add(emailAccount);
                if (_currentEmailAccountIndex == -1)
                {
                    _currentEmailAccountIndex = 0;
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
                int index = EmailAccounts.FindIndex(e => e.Address == address);
                EmailAccounts.RemoveAt(index);
                if (_currentEmailAccountIndex == index)
                {
                    _currentEmailAccountIndex =
                        EmailAccounts.Count > 0 ? 0 : -1;
                }
                result = true;
            }

            return result;
        }

        public bool SetCurrentEmailAccount(string address)
        {
            bool result = false;
            if (ContainsEmailAddress(address))
            {
                _currentEmailAccountIndex =
                    EmailAccounts.FindIndex(e => e.Address == address);
                result = true;
            }

            return result;
        }

        private bool ContainsEmailAddress(string address)
        {
            return EmailAccounts.Exists(e => e.Address == address);
        }

        public byte[] GetBytes()
        {       
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    byte[] login = Encoding.Unicode.GetBytes(Login);
                    writer.Write(login.Length);
                    writer.Write(login);

                    byte[] passwordHash = Encoding.Unicode.GetBytes(PasswordHash);
                    writer.Write(passwordHash.Length);
                    writer.Write(passwordHash);

                    writer.Write(EmailAccounts.Count);
                    foreach (var email in EmailAccounts)
                    {
                        byte[] emailAccount = email.GetBytes();
                        writer.Write(emailAccount.Length);
                        writer.Write(emailAccount);
                    }

                    if (_currentEmailAccountIndex != -1)
                    {
                        writer.Write(_currentEmailAccountIndex);
                    }
                }

                return stream.ToArray();
            }
        }

        public override string ToString()
        {
            return Login;
        }
    }
}
