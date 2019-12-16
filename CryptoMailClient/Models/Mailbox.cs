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

                for (int i = CurrentFolder.Count - StartMessage;
                    i > CurrentFolder.Count - StartMessage - CurrentCount &&
                    i > 0;
                    i--)
                {
                    CurrentMessages.Add(CurrentFolder.GetMessage(i));
                }
            }
        }

        public static void OpenFolder(string name)
        {
            CurrentFolder = Folders.First(f => f.Name.ToLower() == name);
            StartMessage = 1;
            CurrentCount = DEFAULT_CURRENT_MESSAGES_COUNT;
        }
    }
}
