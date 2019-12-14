using System.Collections.Generic;
using System.Threading.Tasks;
using MailKit;
using MailKit.Net.Imap;

namespace CryptoMailClient.Models
{
    static class Mailbox
    {
        private static ImapClient _imapClient;

        public static IList<IMailFolder> MailFolders { get; private set; }

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
            MailFolders = null;
            if (await CheckImapConnection())
            {
                IList<IMailFolder> folders = await _imapClient.GetFoldersAsync(
                    _imapClient.PersonalNamespaces[0]);
                
                MailFolders = new List<IMailFolder>();
                foreach (var folder in folders)
                {
                    if (folder.FullName != "[Gmail]")
                    {
                        await folder.OpenAsync(FolderAccess.ReadWrite);
                        MailFolders.Add(folder);
                        await folder.CloseAsync();
                    }
                }
            }
        }
    }
}
