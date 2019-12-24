using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CryptoMailClient.Utilities;
using CryptoMailClient.ViewModels;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MimeKit;

namespace CryptoMailClient.Models
{
    static class Mailbox
    {
        private const int DEFAULT_CURRENT_MESSAGES_COUNT = 10;

        private static ImapClient _imapClient;

        public static List<FolderItem> Folders { get; }
        public static FolderItem CurrentFolder { get; private set; }
        public static Dictionary<string, MimeMessage> CurrentMessages { get; }

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
            Folders = new List<FolderItem>();
            CurrentMessages = new Dictionary<string, MimeMessage>();
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

        public static void LoadFolders()
        {
            Folders.Clear();

            string emailFolder = UserManager.GetCurrentUserEmailFolder();
            if (emailFolder == null) return;

            DirectoryInfo emailFolderInfo = new DirectoryInfo(emailFolder);
            if (!emailFolderInfo.Exists)
            {
                emailFolderInfo.Create();
            }

            foreach (var directoryInfo in emailFolderInfo.GetDirectories("*",
                SearchOption.AllDirectories))
            {
                if (directoryInfo.Name != "[Gmail]")
                {
                    int count = directoryInfo
                        .GetFiles("*.msg", SearchOption.TopDirectoryOnly)
                        .Length;
                    Folders.Add(new FolderItem(
                        directoryInfo.FullName.Substring(
                            emailFolderInfo.FullName.Length + 1),
                        directoryInfo.Name, count));
                }
            }
        }

        public static void LoadMessages()
        {
            if (UserManager.CurrentUser?.CurrentEmailAccount == null)
            {
                return;
            }

            CurrentMessages.Clear();

            string folderPath =
                Path.Combine(UserManager.GetCurrentUserEmailFolder(),
                    CurrentFolder.FullName);
            List<string> messagesFiles =
                Directory.GetFiles(folderPath, "*.msg").ToList();
            messagesFiles.Sort(new NaturalStringComparer());

            CurrentCount = Math.Min(DEFAULT_CURRENT_MESSAGES_COUNT,
                CurrentFolder.Count - FirstMessage + 1);
            for (int i = CurrentFolder.Count - FirstMessage;
                i > CurrentFolder.Count - FirstMessage - CurrentCount &&
                i > -1;
                i--)
            {
                CurrentMessages.Add(messagesFiles[i],
                    MimeMessage.Load(messagesFiles[i]));
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
                .GetDirectories(emailFolderPath, "*",
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
                    .Select(Path.GetFileName)
                    .Select(m => m.Substring(0, m.IndexOf(' ')))
                    .ToList().Select(uint.Parse).ToList());

                foreach (var serverLetter in serverLetters)
                {
                    var uid = serverLetter.UniqueId;
                    if (serverLetter.Flags != null)
                    {
                        // Получаем ожидаемое имя файла письма.
                        var letterName =
                            uid + " (" + serverLetter.Flags.Value + ")";

                        // Проверяем, есть ли письмо на клиенте
                        // с данным идентификатором.
                        int index = localUids.FindIndex(x => x == uid.Id);
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

                        await _imapClient.NoOpAsync();
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

        public static async Task SendMessage(string address, string subject,
            string htmlContent, string[] attachments = null,
            bool needToEncrypt = false, bool needToSign = false)
        {
            if (UserManager.CurrentUser?.CurrentEmailAccount == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(address))
            {
                throw new Exception("Введите адрес получателя.");
            }

            var regex = new Regex(@"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$");
            if (!regex.IsMatch(address))
            {
                throw new Exception(
                    "Неверный адрес почтовыго ящика получателя.");
            }


            // логическое И (&) - чтобы обязательно был инициализирован publicKey
            if (needToEncrypt &
                !UserManager.TryFindEmailAddressPublicKey(address,
                    out string publicKey))
            {
                throw new Exception("Публичный ключ пользователя с почтовым " +
                                    $"адресом {address} не найден.\n" +
                                    "Получите ключ от пользователя и поместите файл " +
                                    $"с ключом \"{address}.key\" в папку \"public\".");
            }

            MimeMessage message = new MimeMessage();
            message.From.Add(new MailboxAddress("",
                UserManager.CurrentUser.CurrentEmailAccount.Address));
            message.Subject = subject ?? "";
            message.To.Add(new MailboxAddress(address));

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = "<!doctype html>" +
                           "<head><meta charset = \"utf-8\"></head>" +
                           "<body>" + htmlContent + "</body>" +
                           "</html>"
            };

            if (needToEncrypt)
            {
                // Добавляем собственный заголовок типа содержимого
                // с указанием того, что письмо зашифровано.
                message.Headers.Add(Cryptography.HEADER_ENCRYPTED,
                    bool.TrueString);
                bodyBuilder.HtmlBody =
                    Cryptography.EncryptData(bodyBuilder.HtmlBody, publicKey);
            }

            if (needToSign)
            {
                message.Headers.Add(Cryptography.HEADER_SIGNED,
                    bool.TrueString);
                string privateKey = UserManager.CurrentUser.CurrentEmailAccount
                    .RsaPrivateKey;
                string signature =
                    Cryptography.SignData(bodyBuilder.HtmlBody, privateKey);
                message.Headers.Add(Cryptography.HEADER_SIGNATURE, signature);
            }

            if (attachments != null)
            {
                foreach (var attachment in attachments)
                {
                    bodyBuilder.Attachments.Add(attachment);
                }
            }

            message.Body = bodyBuilder.ToMessageBody();

            SmtpClient smtp = await UserManager.CurrentUser.CurrentEmailAccount
                .GetSmtpClient();
            smtp.Send(message);
            smtp.Disconnect(true);

            if (!await CheckImapConnection()) return;

            var sentFolder = _imapClient.GetFolder(SpecialFolder.Sent);
            UniqueId? uid =
                sentFolder.Append(message, MessageFlags.Seen, message.Date);
            if (uid != null)
            {
                await message.WriteToAsync(Path.Combine(
                    UserManager.GetCurrentUserEmailFolder(),
                    sentFolder.FullName,
                    uid + " (" + MessageFlags.Seen + ").msg"));
            }
        }

        public static async Task MarkLetters(bool setSeenFlag,
            List<string> messageFullNameList, string folderFullName)
        {
            if (UserManager.CurrentUser?.CurrentEmailAccount == null) return;

            if (await CheckImapConnection())
            {
                // Из полного имени файлов писем получаем идентификаторы.
                List<string> names = messageFullNameList
                    .Select(Path.GetFileName)
                    .Select(m => m.Substring(0, m.IndexOf(' ')))
                    .ToList();
                List<UniqueId> uniqueIdList =
                    names.Select(uint.Parse).ToList()
                        .Select(i => new UniqueId(i)).ToList();

                // Получаем и открываем папку, в которой находятся необходимые письма.
                var serverFolder = await _imapClient.GetFolderAsync(
                    folderFullName.Replace('\\', '/'));
                await serverFolder.OpenAsync(FolderAccess.ReadWrite);

                // Устаналиваем флаги.
                if (setSeenFlag)
                {
                    await serverFolder.AddFlagsAsync(uniqueIdList,
                        MessageFlags.Seen, true);
                }
                else
                {
                    await serverFolder.RemoveFlagsAsync(uniqueIdList,
                        MessageFlags.Seen, true);
                }

                // Получаем обновлённый список флагов сообщений.
                var serverLetters = serverFolder.Fetch(uniqueIdList,
                    MessageSummaryItems.UniqueId | MessageSummaryItems.Flags);

                await serverFolder.CloseAsync();

                // Изменяем имена файлов писем (с обновлёнными флагами).
                string folderPath = Path.Combine(
                    UserManager.GetCurrentUserEmailFolder(), folderFullName);
                foreach (var serverLetter in serverLetters)
                {
                    int index = uniqueIdList.FindIndex(x =>
                        x.Id == serverLetter.UniqueId.Id);
                    string letterName = Path.Combine(folderPath,
                        serverLetter.UniqueId + " (" + serverLetter.Flags +
                        ")" + ".msg");
                    if (serverLetter.Flags != null)
                    {
                        File.Move(messageFullNameList[index], letterName);
                    }
                }
            }
        }

        public static async Task DeleteMessages(
            List<string> messageFullNameList,
            string folderFullName)
        {
            if (UserManager.CurrentUser?.CurrentEmailAccount == null) return;

            if (await CheckImapConnection())
            {
                // Из полного имени файлов писем получаем идентификаторы.
                List<string> names = messageFullNameList
                    .Select(Path.GetFileName)
                    .Select(m => m.Substring(0, Math.Max(m.IndexOf(' '), 0)))
                    .ToList();
                names.RemoveAll(string.IsNullOrEmpty);
                List<UniqueId> uniqueIdList =
                    names.Select(uint.Parse).ToList()
                        .Select(i => new UniqueId(i)).ToList();

                // Получаем и открываем папку, в которой находятся необходимые письма.
                var serverFolder = await _imapClient.GetFolderAsync(
                    folderFullName.Replace('\\', '/'));
                await serverFolder.OpenAsync(FolderAccess.ReadWrite);

                // Устаналиваем флаг удаления сообщения.
                await serverFolder.AddFlagsAsync(uniqueIdList,
                    MessageFlags.Deleted, true);

                // Окончательно удаляем все сообщения, помеченные на удаление.
                await serverFolder.ExpungeAsync();
                await serverFolder.CloseAsync();

                // Удаляем сообщения с клиента.
                foreach (var messageFullName in messageFullNameList)
                {
                    File.Delete(messageFullName);
                }

                // ОБновляем количество сообщений в папке.
                int messageCount = Directory
                    .GetFiles(Path.Combine(
                        UserManager.GetCurrentUserEmailFolder(),
                        folderFullName)).Length;
                Folders.Find(f => f.FullName.Equals(folderFullName)).Count =
                    messageCount;
                if (CurrentFolder.FullName == folderFullName)
                {
                    CurrentFolder.Count = messageCount;
                }
            }
        }

        public static void OpenFolder(string fullName)
        {
            CurrentFolder = Folders.FirstOrDefault(f =>
                string.Equals(f.FullName,
                    fullName, StringComparison.CurrentCultureIgnoreCase));
            FirstMessage = 1;
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
