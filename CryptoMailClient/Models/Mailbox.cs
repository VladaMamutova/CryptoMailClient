using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MailKit;
using MailKit.Net.Imap;
using MimeKit;

namespace CryptoMailClient.Models
{
    static class Mailbox
    {
        private const int DEFAULT_CURRENT_MESSAGES_COUNT = 10;

        private static ImapClient _imapClient;

        public static IList<IMailFolder> Folders { get; }
        public static IMailFolder CurrentFolder { get; private set; }
        public static List<MimeMessage> CurrentMessages { get; }

        private static int _firstMessage;

        public static int FirstMessage
        {
            get => _firstMessage;
            set
            {
                if (value > CurrentFolder?.Count) value = CurrentFolder.Count;
                if (value < 1) value = 1;
                _firstMessage = value;
            }
        }

        public static int CurrentCount { get; set; }

        static Mailbox()
        {
            Folders = new List<IMailFolder>();
            CurrentMessages = new List<MimeMessage>();
            _firstMessage = 0;
            CurrentCount = 0;
        }

        public static async Task<bool> CheckImapConnection()
        {
            if (_imapClient != null)
            {
                if (!_imapClient.IsConnected)
                {
                    _imapClient.Dispose();
                    _imapClient = null;
                }
            }

            if (_imapClient == null)
            {
                if (UserManager.CurrentUser.CurrentEmailAccount != null)
                {
                    try
                    {
                        _imapClient = await UserManager.CurrentUser
                            .CurrentEmailAccount.GetImapClient();
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Невозможно подключиться к " +
                                            "почтовому ящику.\n" + ex.Message);
                    }
                }
            }

            return _imapClient != null;
        }

        public static async Task ResetImapConnection()
        {
            if (_imapClient != null)
            {
                if (_imapClient.IsConnected)
                {
                    await _imapClient.DisconnectAsync(true);
                }

                _imapClient.Dispose();
                _imapClient = null;
            }
        }

        public static async Task LoadFolders()
        {
            Folders.Clear();
            if (await CheckImapConnection())
            {
                IList<IMailFolder> folders = await _imapClient.GetFoldersAsync(
                    _imapClient.PersonalNamespaces[0]);

                foreach (var folder in folders)
                {
                    if (folder.FullName != "[Gmail]")
                    {
                        await folder.OpenAsync(FolderAccess.ReadWrite);
                        Folders.Add(folder);
                        await folder.CloseAsync();
                    }
                }
            }
        }

        public static async Task LoadMessages()
        {
            CurrentMessages.Clear();
            if (await CheckImapConnection() && CurrentFolder != null)
            {
                CurrentFolder.Open(FolderAccess.ReadOnly);

                CurrentCount = Math.Min(DEFAULT_CURRENT_MESSAGES_COUNT,
                    CurrentFolder.Count - FirstMessage + 1);
                for (int i = CurrentFolder.Count - FirstMessage;
                    i > CurrentFolder.Count - FirstMessage - CurrentCount &&
                    i > -1;
                    i--)
                {
                    CurrentMessages.Add(CurrentFolder.GetMessage(i));
                }
            }
        }

        public static bool OpenFolder(string name)
        {
            try
            {
                if (CurrentFolder?.Name == name) return false;

                CurrentFolder = Folders.First(f => string.Equals(f.Name, name,
                    StringComparison.CurrentCultureIgnoreCase));
                FirstMessage = 1;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool SetNextMessageRange()
        {
            if (FirstMessage == CurrentFolder?.Count) return false;
            FirstMessage += DEFAULT_CURRENT_MESSAGES_COUNT;
            return true;
        }

        public static bool SetPreviousMessageRange()
        {
            if (FirstMessage == 1) return false;
            FirstMessage -= DEFAULT_CURRENT_MESSAGES_COUNT;
            return true;
        }

        public static string GetMessageRange()
        {
            string result = CurrentFolder?.Count > 0
                ? _firstMessage.ToString()
                : 0.ToString();
            if (CurrentCount > 0)
            {
                result += " - " + (_firstMessage + CurrentCount - 1);
            }

            result += " из " + (CurrentFolder?.Count > 0
                          ? CurrentFolder.Count.ToString("N0")
                          : 0.ToString());

            return result;
        }
    }
}
