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

            SynchronizeCommand = new RelayCommand(async o =>
            {
                await SynchronizeMailbox();
            });

            GetNextMessagesCommand =
                new RelayCommand(o =>
                {
                    if (Mailbox.SetNextMessageRange())
                    {
                        LoadMessages();
                    }
                });

            GetPreviousMessagesCommand =
                new RelayCommand(o =>
                {
                    if (Mailbox.SetPreviousMessageRange())
                    {
                        LoadMessages();
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

                        UpdateFolders();
                        SelectFolder(SelectedFolder?.Name);
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

                    await Mailbox.ResetImapConnection();
                    UpdateFolders();
                    SelectFolder(SelectedFolder?.Name);

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
                if (o is string folderName)
                {
                    string engFolderName = null;
                    if (folderName.ToLower() == "входящие")
                        engFolderName = "inbox";
                    if (folderName.ToLower() == "социальные сети")
                        engFolderName = "social";
                    if (folderName.ToLower() == "рассылки")
                        engFolderName = "newsletters";

                    if (Mailbox.OpenFolder(engFolderName ?? folderName))
                    {
                        LoadMessages();
                        SelectedFolder = Folders.First(f =>
                            f.Name == folderName);
                        OnPropertyChanged(nameof(SelectedFolder));
                    }
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

        private void ReadEmail(object o)
        {
            if (o != null && o is MessageItem item)
            {
                string content;
                if (!string.IsNullOrEmpty(item.Message.HtmlBody))
                {
                    int index = item.Message.HtmlBody.IndexOf("html>",
                        StringComparison.OrdinalIgnoreCase);
                    if (index < 0)
                    {
                        content = "<!doctype html>" +
                                  "<head><meta charset = \"utf-8\"></head>" +
                                  "<body>" + item.Message.HtmlBody + "</body>" +
                                  "</html>";
                    }
                    else
                    {
                        content = item.Message.HtmlBody.Insert(
                            index + "html>".Length,
                            "<head><meta charset=\"utf-8\"></head>");
                    }
                }
                else
                {
                    content =
                        "<!doctype html><head><meta charset = \"utf-8\"></head>" +
                        "<body>" + item.Message.TextBody + "</body></html>";
                }

                item.HtmlBody = content;

                OnShowDialogRequested(item);
            }
        }

        private void WriteEmail(object o)
        {
            OnShowDialogRequested(null);
        }

        private async Task SynchronizeMailbox()
        {
            DialogHost.OpenDialogCommand.Execute(new ProgressDialog(), null);

            try
            {
                await Mailbox.Synchronize();
                UpdateFolders();
                SelectFolder(SelectedFolder?.Name);
                DialogHost.CloseDialogCommand.Execute(null, null);
                // Явно закрываем диалог, так как предыдущая
                // команда иногда может не выполняться.
                IsDialogOpen = false;
            }
            catch (Exception e)
            {
                UpdateFolders();
                SelectFolder(SelectedFolder?.Name);
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
                        if (folder.Name.ToLower() == "отправленные")
                        {
                            sentIndex = i;
                        }

                        if (folder.Name.ToLower() == "inbox")
                        {
                            folders.Add(
                                new FolderItem("Входящие", folder.Count));
                            inboxIndex = i;
                        }
                        else
                        {
                            string ruFolderName = null;
                            if (folder.Name.ToLower() == "social")
                                ruFolderName = "Социальные сети";
                            if (folder.Name.ToLower() == "newsletters")
                                ruFolderName = "Рассылки";

                            folders.Add(new FolderItem(
                                ruFolderName ?? folder.Name,
                                folder.Count));
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

                    SelectedFolder = Folders.Count > 0 ? Folders[0] : null;
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
                        messages.Add(new MessageItem(message));
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
