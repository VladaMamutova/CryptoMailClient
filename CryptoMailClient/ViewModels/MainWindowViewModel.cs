using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CryptoMailClient.Models;
using CryptoMailClient.Utilities;
using CryptoMailClient.Views;
using MaterialDesignThemes.Wpf;

namespace CryptoMailClient.ViewModels
{
    class MainWindowViewModel : ViewModelBase
    {
        #region Properties

        public string Title => "Crypto Mail Client";

        public string CurrentUserLogin => UserManager.CurrentUser?.Login;

        public string CurrentEmailAddress =>
            UserManager.CurrentUser?.CurrentEmailAccount?.Address;

        public bool HasCurrentEmailAccount =>
            UserManager.CurrentUser?.CurrentEmailAccount != null;

        public List<EmailAccount> EmailAccounts =>
            UserManager.CurrentUser?.EmailAccounts?
                .Where(e => e.Address != CurrentEmailAddress).ToList();

        public bool HasEmailAccounts => EmailAccounts.Count != 0;

        public string MessageRangeText => Mailbox.GetMessageRange();

        private bool OpenEmail => Messages == null ||
                                  Messages.ToList().Exists(m =>
                                      m.IsSelected && !m.Seen);

        public PackIconKind MarkEmailIcon =>
            OpenEmail ? PackIconKind.EmailOpen : PackIconKind.Email;

        public string MarkEmailText => OpenEmail ? "Прочитано" : "Не прочитано";

        private bool _isDialogOpen;

        public bool IsDialogOpen
        {
            get => _isDialogOpen;
            set
            {
                _isDialogOpen = value;
                OnPropertyChanged(nameof(IsDialogOpen));
                OnPropertyChanged(nameof(IsDialogClose));
            }
        }

        public bool IsDialogClose => !_isDialogOpen;

        private ObservableCollection<FolderItem> _folders;

        public ObservableCollection<FolderItem> Folders
        {
            get => _folders;
            set
            {
                _folders = value;
                OnPropertyChanged(nameof(Folders));
            }
        }

        public FolderItem SelectedFolder { get; private set; }

        private ObservableCollection<MessageItem> _messages;

        public ObservableCollection<MessageItem> Messages
        {
            get => _messages;
            set
            {
                _messages = value;
                OnPropertyChanged(nameof(Messages));
            }
        }

        #endregion

        #region Commands

        public RelayCommand RunEmailDialogCommand { get; }
        public RelayCommand SetEmailAccountCommand { get; }
        public RelayCommand SelectFolderCommand { get; }
        public RelayCommand ReadEmailCommand { get; }
        public RelayCommand WriteEmailCommand { get; }
        public RelayCommand DeleteEmailCommand { get; }
        public RelayCommand MarkEmailCommand { get; }
        public RelayCommand SelectEmailCommand { get; }
        public RelayCommand SynchronizeCommand { get; }
        public RelayCommand GetNextMessagesCommand { get; }
        public RelayCommand GetPreviousMessagesCommand { get; }
        public RelayCommand CloseCommand { get; }

        #endregion

        public MainWindowViewModel()
        {
            _isDialogOpen = false;
            RunEmailDialogCommand = new RelayCommand(RunEmailDialog);
            SetEmailAccountCommand =
                new RelayCommand(SetEmailAccount, o => !IsDialogOpen);
            SelectFolderCommand = new RelayCommand(SelectFolder);
            ReadEmailCommand = new RelayCommand(ReadEmail);
            WriteEmailCommand = new RelayCommand(WriteEmail);
            DeleteEmailCommand = new RelayCommand(DeleteEmails,
                o => Messages?.Count(m => m.IsSelected) > 0);
            MarkEmailCommand = new RelayCommand(MarkEmails,
                o => Messages?.Count(m => m.IsSelected) > 0);
            SelectEmailCommand = new RelayCommand(o =>
            {
                OnPropertyChanged(nameof(MarkEmailIcon));
                OnPropertyChanged(nameof(MarkEmailText));
            });

            SynchronizeCommand = new RelayCommand(async o =>
            {
                await SynchronizeMailbox();
            });

            GetNextMessagesCommand =
                new RelayCommand(o =>
                {
                    if (HasCurrentEmailAccount)
                    {
                        if (Mailbox.SetNextMessageRange())
                        {
                            LoadMessages();
                        }
                    }
                });

            GetPreviousMessagesCommand =
                new RelayCommand(o =>
                {
                    if (HasCurrentEmailAccount)
                    {
                        if (Mailbox.SetPreviousMessageRange())
                        {
                            LoadMessages();
                        }
                    }
                });

            CloseCommand = new RelayCommand(async o =>
            {
                await Mailbox.ResetImapConnection();
                OnCloseRequested();
            }, o => !IsDialogOpen);
        }

        private async void RunEmailDialog(object o)
        {
            if (o is bool isNewEmailAccount)
            {
                var view = new EmailSettingsDialog(isNewEmailAccount);
                var result = await DialogHost.Show(view, "RootDialog");

                if (result != null && result is bool boolResult)
                {
                    if (boolResult)
                    {
                        OnPropertyChanged(nameof(CurrentEmailAddress));
                        OnPropertyChanged(nameof(HasCurrentEmailAccount));
                        OnPropertyChanged(nameof(EmailAccounts));
                        OnPropertyChanged(nameof(HasEmailAccounts));
                        OnPropertyChanged(nameof(MessageRangeText));

                        UpdateFolders();
                        SelectFolder(SelectedFolder?.FullName);
                    }
                }
            }
        }

        private async void SetEmailAccount(object address)
        {
            try
            {
                if (UserManager.CurrentUser.SetCurrentEmailAccount(
                    (address ?? "").ToString()))
                {
                    OnPropertyChanged(nameof(CurrentEmailAddress));
                    OnPropertyChanged(nameof(EmailAccounts));

                    UserManager.SaveCurrectUserInfo();

                    "пирожное" Mailbox.ResetImapConnection();
                    UpdateFolders();
                    SelectFolder(SelectedFolder?.FullName);

                    OnPropertyChanged(nameof(MessageRangeText));
                }
            }
            catch (Exception ex)
            {
                OnMessageBoxDisplayRequest(Title, ex.Message);
            }
        }

        public void SelectFolder(object o)
        {
            try
            {
                if (o is string folderFullName)
                {
                    Mailbox.OpenFolder(folderFullName);
                    LoadMessages();
                    SelectedFolder = Folders.First(f =>
                        f.FullName == folderFullName);
                    OnPropertyChanged(nameof(SelectedFolder));
                }
                else
                {
                    Messages = new ObservableCollection<MessageItem>();
                    OnPropertyChanged(nameof(MessageRangeText));
                }
            }
            catch (Exception ex)
            {
                OnMessageBoxDisplayRequest(Title, ex.Message);
            }
        }

        private async void ReadEmail(object o)
        {
            if (o == null || !(o is MessageItem item)) return;

            string htmlBody = item.Message.HtmlBody;
            if (!string.IsNullOrEmpty(htmlBody))
            {
                var verificationResult = CryptographicResult.None;
                if (item.Message.Headers.Contains(Cryptography.HEADER_SIGNED))
                {
                    if (UserManager.TryFindEmailAddressPublicKey(
                        item.AddressFrom, out string publicKey))
                    {
                        try
                        {
                            verificationResult = Cryptography.VerifyData(
                                htmlBody.TrimEnd('\n', '\r'), publicKey,
                                item.Message.Headers[
                                    Cryptography.HEADER_SIGNATURE])
                                ? CryptographicResult.Success
                                : CryptographicResult.Error;
                        }
                        catch
                        {
                            verificationResult = CryptographicResult.Error;
                        }
                    }
                    else
                    {
                        verificationResult = CryptographicResult.KeyNotFound;
                    }

                }

                CryptographicResult decryptionResult =
                    CryptographicResult.None;
                if (item.Message.Headers.Contains(Cryptography.HEADER_ENCRYPTED)
                )
                {
                    try
                    {
                        htmlBody = Cryptography.DecryptData(htmlBody,
                            UserManager.CurrentUser.CurrentEmailAccount
                                .RsaPrivateKey);
                        decryptionResult = CryptographicResult.Success;
                    }
                    catch
                    {
                        decryptionResult = CryptographicResult.Error;
                    }
                }

                item.SetCryptographicResults(decryptionResult,
                    verificationResult);

                int index = htmlBody.IndexOf("html>",
                    StringComparison.OrdinalIgnoreCase);
                if (index < 0)
                {
                    htmlBody = "<!doctype html>" +
                               "<head><meta charset = \"utf-8\"></head>" +
                               "<body>" + htmlBody + "</body>" +
                               "</html>";
                }
                else
                {
                    htmlBody = htmlBody.Insert(
                        index + "html>".Length,
                        "<head><meta charset=\"utf-8\"></head>");
                }
            }
            else
            {
                htmlBody =
                    "<!doctype html><head><meta charset = \"utf-8\"></head>" +
                    "<body>" + item.Message.TextBody + "</body></html>";
            }

            item.HtmlBody = htmlBody;

            OnShowDialogRequested(item);

            if (!item.Seen)
            {
                DialogHost.OpenDialogCommand.Execute(new ProgressDialog(),
                    null);
                try
                {
                    await Mailbox.MarkLetters(true,
                        new List<string> {item.FullName},
                        SelectedFolder.FullName);

                    Messages.ToList().Find(m => m.FullName == item.FullName)
                        .Seen = true;
                    OnPropertyChanged(nameof(Messages));

                    DialogHost.CloseDialogCommand.Execute(null, null);
                    // Явно закрываем диалог, так как предыдущая
                    // команда иногда может не выполняться.
                    IsDialogOpen = false;
                }
                catch (Exception ex)
                {
                    DialogHost.CloseDialogCommand.Execute(null, null);
                    // Явно закрываем диалог, так как предыдущая
                    // команда иногда может не выполняться.
                    IsDialogOpen = false;
                    OnMessageBoxDisplayRequest(Title,
                        "Не удалось синхронизировать письмо с сервером. " +
                        ex.Message);
                }
            }
        }

        private void WriteEmail(object o)
        {
            if (UserManager.CurrentUser?.CurrentEmailAccount != null)
            {
                OnShowDialogRequested(null);
                UpdateFolders();
            }
        }

        private async void DeleteEmails(object obj)
        {
            DialogHost.OpenDialogCommand.Execute(new ProgressDialog(), null);

            try
            {
                List<string> messagesToDelete = Messages.ToList()
                    .FindAll(m => m.IsSelected).Select(m => m.FullName)
                    .ToList();
                await Mailbox.DeleteMessages(messagesToDelete,
                    SelectedFolder.FullName);
                UpdateFolders();
                LoadMessages();
                DialogHost.CloseDialogCommand.Execute(null, null);
                // Явно закрываем диалог, так как предыдущая
                // команда иногда может не выполняться.
                IsDialogOpen = false;
            }
            catch (Exception ex)
            {
                UpdateFolders();
                LoadMessages();
                DialogHost.CloseDialogCommand.Execute(null, null);
                // Явно закрываем диалог, так как предыдущая
                // команда иногда может не выполняться.
                IsDialogOpen = false;
                OnMessageBoxDisplayRequest(Title,
                    "Не удалось синхронизировать письма с сервером. " +
                    ex.Message);
            }
        }

        private async void MarkEmails(object obj)
        {
            DialogHost.OpenDialogCommand.Execute(new ProgressDialog(), null);

            try
            {
                List<string> messagesToMark;
                if (OpenEmail)
                {
                    messagesToMark = Messages.ToList()
                        .FindAll(m => m.IsSelected && !m.Seen)
                        .Select(m => m.FullName).ToList();
                }
                else
                {
                    messagesToMark = Messages.ToList()
                        .FindAll(m => m.IsSelected && m.Seen)
                        .Select(m => m.FullName).ToList();
                }

                await Mailbox.MarkLetters(OpenEmail, messagesToMark,
                    SelectedFolder.FullName);
                LoadMessages();
                DialogHost.CloseDialogCommand.Execute(null, null);
                // Явно закрываем диалог, так как предыдущая
                // команда иногда может не выполняться.
                IsDialogOpen = false;
            }
            catch (Exception ex)
            {
                LoadMessages();
                DialogHost.CloseDialogCommand.Execute(null, null);
                // Явно закрываем диалог, так как предыдущая
                // команда иногда может не выполняться.
                IsDialogOpen = false;
                OnMessageBoxDisplayRequest(Title,
                    "Не удалось синхронизировать письма с сервером. " +
                    ex.Message);
            }
        }

        private async Task SynchronizeMailbox()
        {
            DialogHost.OpenDialogCommand.Execute(new ProgressDialog(), null);

            try
            {
                await Mailbox.Synchronize();
                UpdateFolders();
                SelectFolder(SelectedFolder?.FullName);
                DialogHost.CloseDialogCommand.Execute(null, null);
                // Явно закрываем диалог, так как предыдущая
                // команда иногда может не выполняться.
                IsDialogOpen = false;
            }
            catch (Exception e)
            {
                UpdateFolders();
                SelectFolder(SelectedFolder?.FullName);
                DialogHost.CloseDialogCommand.Execute(null, null);
                // Явно закрываем диалог, так как предыдущая
                // команда иногда может не выполняться.
                IsDialogOpen = false;
                OnMessageBoxDisplayRequest(Title,
                    "Не удалось синхронизировать почтовый ящик.\n" + e.Message);
            }
        }

        public void UpdateFolders()
        {
            try
            {
                Mailbox.LoadFolders();
                if (Mailbox.Folders != null)
                {
                    int inboxIndex = -1;
                    int sentIndex = -1;
                    var folders = new ObservableCollection<FolderItem>();
                    for (var i = 0; i < Mailbox.Folders.Count; i++)
                    {
                        var folder = Mailbox.Folders[i];
                        if (folder.DisplayName.ToLower() == "отправленные")
                        {
                            sentIndex = i;
                        }

                        if (folder.DisplayName.ToLower() == "inbox")
                        {
                            folders.Add(new FolderItem(folder.FullName,
                                "Входящие", folder.Count));
                            inboxIndex = i;
                        }
                        else
                        {
                            string displayName = folder.DisplayName;
                            if (displayName.ToLower() == "social")
                            {
                                displayName = "Социальные сети";
                            }
                            else if (displayName.ToLower() == "newsletters")
                            {
                                displayName = "Рассылки";
                            }

                            folders.Add(new FolderItem(folder.FullName,
                                displayName, folder.Count));
                        }
                    }

                    // Папки с входящими и отправленными письмами
                    // в списке будут первыми.
                    if (inboxIndex != -1)
                    {
                        folders.Move(inboxIndex, 0);
                        // Если sentIndex был меньше, чем inboxIndex, значит
                        // перемещение элемента по inboxIndex на первую
                        // позицию, сдвинуло на одну позицию элемент с sentIndex.
                        if (sentIndex < inboxIndex)
                        {
                            sentIndex++;
                        }
                    }

                    if (sentIndex != -1)
                    {
                        folders.Move(sentIndex, 1);
                    }

                    Folders = folders;

                    if (SelectedFolder != null && Folders.ToList().Exists(f =>
                            f.FullName == SelectedFolder.FullName))
                    {
                        SelectedFolder = Folders.First(f =>
                            f.FullName == SelectedFolder.FullName);
                    }
                    else
                    {
                        SelectedFolder = Folders.FirstOrDefault();
                    }
                }
                else
                {
                    Folders = null;
                }
            }
            catch (Exception ex)
            {
                OnMessageBoxDisplayRequest(Title, ex.Message);
            }
        }

        public void LoadMessages()
        {
            try
            {
                Mailbox.LoadMessages();
                if (Mailbox.CurrentMessages != null)
                {
                    var messages = new ObservableCollection<MessageItem>();
                    foreach (var message in Mailbox.CurrentMessages)
                    {
                        messages.Add(
                            new MessageItem(message.Key, message.Value));
                    }

                    Messages = messages;
                }
                else
                {
                    Messages = null;
                }
            }
            catch (Exception ex)
            {
                OnMessageBoxDisplayRequest(Title, ex.Message);
            }
            finally
            {
                OnPropertyChanged(nameof(MessageRangeText));
            }
        }
    }
}
