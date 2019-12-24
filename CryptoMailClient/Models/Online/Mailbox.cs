using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MailKit;
using MailKit.Net.Imap;
using MimeKit;

namespace CryptoMailClient.Models.Online
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
                    _imapClient.PersonalNamespaces.First());

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

        public static async Task Synchronize()
        {
            if (!await CheckImapConnection()) return;

            string emailFolderPath =
                UserManager.GetCurrentUserEmailFolder();

            if (!Directory.Exists(emailFolderPath))
            {
                Directory.CreateDirectory(emailFolderPath);
            }

            // Получаем папки на сервере.
            IList<IMailFolder> serverFolders =
                await _imapClient.GetFoldersAsync(_imapClient.PersonalNamespaces
                    .First());

            // Получаем папки на клиенте. 
            List<string> localFolders = Directory
                .GetDirectories(emailFolderPath, "*.*",
                    SearchOption.AllDirectories).ToList();

            foreach (var serverFolder in serverFolders)
            {
                string serverFolderName = serverFolder.FullName
                    .Replace('/', '\\');

                var localFolderName =
                    localFolders.Find(f => f.EndsWith(serverFolderName));
                if (localFolderName != null)
                {
                    // Если папка существовала на клиенте, значит
                    // удаляем её из списка папок,
                    // которые необходимо ещё просмотреть.
                    localFolders.Remove(localFolderName);
                }

                string folderPath =
                    Path.Combine(emailFolderPath, serverFolderName);

                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                if (serverFolder.FullName == "[Gmail]")
                {
                    continue;
                }

                // Получаем идентификаторы и флаги писем в папке на сервере.
                await serverFolder.OpenAsync(FolderAccess.ReadOnly);
                IList<IMessageSummary> serverLetters =
                    await serverFolder.FetchAsync(0, -1,
                        MessageSummaryItems.UniqueId |
                        MessageSummaryItems.Flags);

                // Получаем имена файлов писем и их идентификаторы в папке на клиенте.
                // Формат имени файла: "<uid> (<flag>[, <flag>]).msg".
                List<string> localLetters =
                    Directory.GetFiles(folderPath, "*.msg").ToList();
                List<uint> localUids = new List<uint>(localLetters
                    .Select(l => l.Substring(l.LastIndexOf('\\') + 1,
                        l.LastIndexOf('(') - l.LastIndexOf('\\') - 2))
                    // -1 на пробел перед "("
                    .ToList().Select(uint.Parse).ToList());

                foreach (var serverLetter in serverLetters)
                {
                    var uid = serverLetter.UniqueId;
                    if (serverLetter.Flags != null)
                    {
                        // Получаем ожидаемое имя файла письма.
                        var letterName = uid + " (" + serverLetter.Flags.Value + ")";

                        // Проверяем, есть ли письмо на клиенте
                        // с данным идентификатором.
                        int index =
                            localUids.FindIndex(x => x == uid.Id);
                        if (index != -1)
                        {
                            // Если письмо на клиенте существует,
                            // проверяем имя файла.
                            var localLetter = localLetters[index];
                            if (!localLetter.EndsWith(letterName))
                            {
                                // Если фактическое имя отличается от ожидаемого
                                // (флаги были изменены), переименовываем файл.
                                string localLetterWithNewFlag =
                                    localLetter.Substring(0,
                                        localLetter.LastIndexOf('\\') + 1) +
                                    letterName + ".msg";

                                File.Move(localLetter,
                                    localLetterWithNewFlag);
                            }

                            // Письмо просмотрено, удаляем его из списка писем,
                            // которые необходимо ещё просмотреть.
                            localLetters.Remove(localLetter);
                            localUids.RemoveAt(index);
                        }
                        else
                        {
                            // Если письмо на клиенте не существует, создаём его.
                            await (await serverFolder.GetMessageAsync(uid))
                                .WriteToAsync(Path.Combine(folderPath,
                                    letterName + ".msg"));
                        }
                    }
                }

                // Если какие-то письма остались не просмотренными,
                // значит на сервере они были удалены. Удаляем их и на клиенте.
                foreach (var letter in localLetters)
                {
                    File.Delete(letter);
                }

                await serverFolder.CloseAsync();
            }

            // Если какие-то папки на клиенте остались не просмотренными,
            // значит на сервере они были удалены. Удаляем их и на клиенте.
            foreach (var folder in localFolders)
            {
                Directory.Delete(folder, true);
            }
        }

        public static bool OpenFolder(string relativePath)
        {
            try
            {
                if (CurrentFolder?.FullName == relativePath) return false;

                CurrentFolder = Folders.First(f => string.Equals(f.FullName, relativePath,
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
