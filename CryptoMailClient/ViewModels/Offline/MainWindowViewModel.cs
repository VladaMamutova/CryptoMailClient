using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CryptoMailClient.Models;
using CryptoMailClient.Models.Offline;
using CryptoMailClient.Utilities;
using CryptoMailClient.Views;
using MaterialDesignThemes.Wpf;

namespace CryptoMailClient.ViewModels.Offline
{
    class MainWindowViewModel : ViewModelBase
    {
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

        public RelayCommand RunNewEmailDialogCommand =>
            new RelayCommand(RunNewEmailDialog);

        public RelayCommand RunEmailSettingsDialogCommand =>
            new RelayCommand(RunEmailSettingsDialog);

        public RelayCommand SetEmailAccountCommand =>
            new RelayCommand(SetEmailAccount, o => !IsDialogOpen);

        public RelayCommand GetNextMessagesCommand =>
            new RelayCommand(o =>
            {
                if (Mailbox.SetNextMessageRange())
                {
                    try
                    {
                        LoadMessages();
                        OnPropertyChanged(nameof(MessageRangeText));
                    }
                    catch (Exception ex)
                    {
                        OnMessageBoxDisplayRequest(Title, ex.Message);
                    }
                }
            });

        public RelayCommand GetPreviousMessagesCommand =>
            new RelayCommand(o =>
            {
                if (Mailbox.SetPreviousMessageRange())
                {
                    try
                    {
                        LoadMessages();
                        OnPropertyChanged(nameof(MessageRangeText));
                    }
                    catch (Exception ex)
                    {
                        OnMessageBoxDisplayRequest(Title, ex.Message);
                    }
                }
            });

        public RelayCommand SelectFolderCommand =>
            new RelayCommand(SelectFolder);

        public RelayCommand ReadEmailCommand =>
            new RelayCommand(ReadEmail);

        public RelayCommand CloseCommand => new RelayCommand(async o =>
        {
            await Mailbox.ResetImapConnection();
            OnCloseRequested();
        }, o => !IsDialogOpen);

        public RelayCommand SynchronizeCommand => new RelayCommand(async o =>
        {
            await SynchronizeMailbox();
        });

        public async Task SynchronizeMailbox()
        {
            DialogHost.OpenDialogCommand.Execute(new ProgressDialog(), null);

            try
            {
                await Mailbox.Synchronize();
                UpdateFolders();
                SelectFolder(SelectedFolder?.Name);
                DialogHost.CloseDialogCommand.Execute(null, null);
                IsDialogOpen = false;
            }
            catch (Exception e)
            {
                UpdateFolders();
                SelectFolder(SelectedFolder?.Name);
                DialogHost.CloseDialogCommand.Execute(null, null);
                IsDialogOpen = false;
                OnMessageBoxDisplayRequest(Title,
                    "Не удалось синхронизировать почтовый ящик.\n" + e.Message);
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
                        OnPropertyChanged(nameof(MessageRangeText));
                        SelectedFolder = Folders.First(f =>
                            f.Name == folderName);
                        OnPropertyChanged(nameof(SelectedFolder));
                    }
                }
                else
                {
                    Messages = new ObservableCollection<MessageItem>();
                }
            }
            catch (Exception ex)
            {
                OnMessageBoxDisplayRequest(Title, ex.Message);
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

        private async void RunNewEmailDialog(object o)
        {
            var view = new EmailSettingsDialog(true);

            var result = await DialogHost.Show(view, "RootDialog");

            if (result != null && result is bool boolResult)
            {
                if (boolResult)
                {
                    OnPropertyChanged(nameof(CurrentEmailAddress));
                    OnPropertyChanged(nameof(HasCurrentEmailAccount));
                    OnPropertyChanged(nameof(EmailAccounts));
                    OnPropertyChanged(nameof(HasEmailAccounts));
                }
            }
        }

        private async void RunEmailSettingsDialog(object o)
        {
            var view = new EmailSettingsDialog(false);

            var result =
                await DialogHost.Show(view, "RootDialog");

            if (result != null && result is bool boolResult)
            {
                if (boolResult)
                {
                    OnPropertyChanged(nameof(CurrentEmailAddress));
                    OnPropertyChanged(nameof(HasCurrentEmailAccount));
                    OnPropertyChanged(nameof(EmailAccounts));
                    OnPropertyChanged(nameof(HasEmailAccounts));
                }
            }
        }

        private void ReadEmail(object o)
        {
            if (o != null && o is MessageItem item)
            {
                string content;
                if (!string.IsNullOrEmpty(item.Message.HtmlBody))
                {
                    int index = item.Message.HtmlBody.IndexOf("<html>",
                        StringComparison.OrdinalIgnoreCase);
                    if (index < 0)
                    {
                        content = "<html><meta charset=\"utf-8\"><body>" +
                                  item.Message.HtmlBody + "</body></html>";
                    }
                    else
                        content = item.Message.HtmlBody.Insert(
                            index + "<html>".Length,
                            "<meta charset=\"utf-8\">");
                }
                else
                    content =
                        "<!doctype html><head><meta charset = \"utf-8\"></head>" +
                        "<body>" + item.Message.TextBody + "</body></html>";

                item.HtmlBody = content;

                OnShowDialogRequested(item);
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
        }

        public MainWindowViewModel()
        {
            _isDialogOpen = false;
        }
    }
}
