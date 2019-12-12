using System;
using System.IO;
using System.Linq;
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

        private static bool IsUniqueLogin(string login)
        {
            if (!Directory.Exists(USERS_DIRECTORY))
            {
                Directory.CreateDirectory(USERS_DIRECTORY);
                return false;
            }

            string[] directories = Directory.GetDirectories(USERS_DIRECTORY);
            return !directories.Contains(GetUserDirectory(login));
        }

        public static bool SignIn(string login, string password)
        {
            if (IsUniqueLogin(login))
            {
                return false;
            }

            string userFile = GetUserInfoFile(login);
            if (!File.Exists(userFile))
            {
                return false;
            }

            User user = new User(login, password);
            string passwordHash = File.ReadAllText(userFile, Encoding.Unicode);
            if (user.PasswordHash == passwordHash)
            {
                CurrentUser = user;
                return true;
            }

            return false;
        }

        public static bool SignUp(string login, string password)
        {
            if (!IsUniqueLogin(login))
            {
                return false;
            }

            User user = new User(login, password);
            Directory.CreateDirectory(GetUserDirectory(login));
            File.WriteAllText(GetUserInfoFile(login), user.PasswordHash,
                Encoding.Unicode);

            return true;
        }
    }
}