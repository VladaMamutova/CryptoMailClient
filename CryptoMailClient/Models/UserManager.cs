using System.IO;
using System.Text;

namespace CryptoMailClient.Models
{
    public class UserManager
    {
        private const string USERS_DIRECTORY = "Users";
        private const string USER_INFO_FILE = "user.inf";

        public static User CurrentUser { get; private set; }

        public static bool SignIn(string login, string password)
        {
            bool result = !IsUniqueLogin(login);

            if (result)
            {
                User user = new User(login, password);
                string passwordHash = File.ReadAllText(GetUserInfoFile(user), Encoding.Unicode);
                if (user.PasswordHash != passwordHash)
                {
                    result = false;
                }
                else
                {
                    CurrentUser = user;
                }
            }

            return result;
        }

        public static bool SignUp(string login, string password)
        {
            bool result = IsUniqueLogin(login);

            if (result)
            {
                User user = new User(login, password);
                Directory.CreateDirectory(GetUserDirectory(user));
                File.WriteAllText(GetUserInfoFile(user), user.PasswordHash,
                    Encoding.Unicode);
            }

            return result;
        }

        private static string GetUserDirectory(User user)
        {
            return Path.Combine(USERS_DIRECTORY, user.Login);
        }

        private static string GetUserDirectory(string login)
        {
            return Path.Combine(USERS_DIRECTORY, login);
        }

        private static string GetUserInfoFile(User user)
        {
            return Path.Combine(USERS_DIRECTORY, user.Login, USER_INFO_FILE);
        }

        private static bool IsUniqueLogin(string login)
        {
            bool result = true;

            if (!Directory.Exists(USERS_DIRECTORY))
            {
                Directory.CreateDirectory(USERS_DIRECTORY);
            }

            string[] directories = Directory.GetDirectories(USERS_DIRECTORY);
            string userDirectory = GetUserDirectory(login);
            for (int i = 0; i < directories.Length && result; i++)
            {
                if (directories[i] == userDirectory)
                {
                    result = false;
                }
            }

            return result;
        }
    }
}