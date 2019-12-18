using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace CryptoMailClient.Models
{
    public class UserManager
    {
        private const string USERS_DIRECTORY = "Users";
        private const string USER_INFO_FILE = "user.inf";

        public static User CurrentUser { get; private set; }

        private static string GetUserDirectory(string login)
        {
            return Path.Combine(USERS_DIRECTORY, login);
        }

        private static string GetUserInfoFile(string login)
        {
            return Path.Combine(USERS_DIRECTORY, login, USER_INFO_FILE);
        }

        public static string GetCurrentUserEmailFolder()
        {
            if (CurrentUser?.CurrentEmailAccount != null)
            {
                return Path.Combine(USERS_DIRECTORY, CurrentUser.Login,
                    CurrentUser.CurrentEmailAccount.Address);
            }

            return null;
        }

        private static bool HasUserInfoFile(string login)
        {
            if (!Directory.Exists(USERS_DIRECTORY))
            {
                Directory.CreateDirectory(USERS_DIRECTORY);
                return false;
            }

            return File.Exists(GetUserInfoFile(login));
        }

        public static bool SignIn(string login, string password)
        {
            if (!HasUserInfoFile(login))
            {
                return false;
            }

            string userFile = GetUserInfoFile(login);


            if (ComputeHash(password) == User.GetPasswordHashFromFile(userFile))
            {
                CurrentUser = User.FromBytes(File.ReadAllBytes(userFile));
                return true;
            }

            return false;
        }

        public static bool SignUp(string login, string password)
        {
            if (HasUserInfoFile(login))
            {
                return false;
            }

            User user = new User(login, ComputeHash(password));
            Directory.CreateDirectory(GetUserDirectory(login));

            //todo: encrypt info
            File.WriteAllBytes(GetUserInfoFile(user.Login),
                user.GetBytes());

            return true;
        }

        public static void SaveCurrectUserInfo()
        {
            //todo: encrypt info
            File.WriteAllBytes(GetUserInfoFile(CurrentUser.Login),
                CurrentUser.GetBytes());
        }

        private static string ComputeHash(string data)
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