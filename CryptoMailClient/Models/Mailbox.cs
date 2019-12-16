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
        private const int DEFAULT_CURRENT_MESSAGES_COUNT = 20;

        private static ImapClient _imapClient;

        public static IList<IMailFolder> Folders { get; }
        public static IMailFolder CurrentFolder { get; private set; }
        public static List<MimeMessage> CurrentMessages { get; }

        private static int _startMessage;

        public static int StartMessage
        {
            get => _startMessage;
            set
            {
                if (value < 1) value = 1;
                if (value > CurrentFolder?.Count) value = CurrentFolder.Count;
                _startMessage = value;
            }
        }

        public static int CurrentCount { get; set; }

        static Mailbox()
        {
            Folders = new List<IMailFolder>();
            CurrentMessages = new List<MimeMessage>();
            _startMessage = 0;
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
                    _imapClient = await UserManager.CurrentUser
                        .CurrentEmailAccount.GetImapClient();
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

                CurrentCount = 0;
                for (int i = CurrentFolder.Count - StartMessage;
                    i > CurrentFolder.Count - StartMessage -
                    DEFAULT_CURRENT_MESSAGES_COUNT &&
                    i > 0;
                    i--)
                {
                    CurrentMessages.Add(CurrentFolder.GetMessage(i));
                    CurrentCount++;
                }
            }
        }

        public static void OpenFolder(string name)
        {
            CurrentFolder = Folders.First(f => f.Name.ToLower() == name);
            StartMessage = 1;
        }

        public static bool SetNextMessageRange()
        {
            if (StartMessage == CurrentFolder?.Count) return false;
            StartMessage += DEFAULT_CURRENT_MESSAGES_COUNT;
            return true;
        }

        public static bool SetPreviousMessageRange()
        {
            if (StartMessage == 1) return false;
            StartMessage -= DEFAULT_CURRENT_MESSAGES_COUNT;
            return true;
        }

        public static string GetMessageRange()
        {
            string result = _startMessage.ToString();
            if (_startMessage != CurrentCount)
            {
                result += " - " + (_startMessage + CurrentCount - 1);
            }

            result += " из " + (CurrentFolder?.Count > 0
                          ? CurrentFolder.Count.ToString("N0")
                          : 0.ToString());

            return result;
        }
    }
}
