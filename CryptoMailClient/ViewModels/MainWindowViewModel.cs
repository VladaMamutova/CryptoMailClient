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

        private bool _isPopupClose;

        public bool IsPopupClose
        {
            get => _isPopupClose;
            set
            {
                _isPopupClose = value;
                OnPropertyChanged(nameof(IsPopupClose));
            }
        }

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

        public RelayCommand RunNewEmailDialogCommand =>
            new RelayCommand(RunNewEmailDialog);

        public RelayCommand RunEmailSettingsDialogCommand =>
            new RelayCommand(RunEmailSettingsDialog);

        public RelayCommand SetEmailAccountCommand =>
            new RelayCommand(async address =>
            {
                try
                {
                    if (UserManager.CurrentUser
                        .SetCurrentEmailAccount((address ?? "").ToString()))
                    {
                        OnPropertyChanged(nameof(CurrentEmailAddress));
                        OnPropertyChanged(nameof(EmailAccounts));

                        UserManager.SaveCurrectUserInfo();

                        await Mailbox.ResetImapConnection();
                        await UpdateFolders();
                    }
                }
                catch (Exception ex)
                {
                    OnMessageBoxDisplayRequest(Title, ex.Message);
                }
            });

        public RelayCommand CloseCommand => new RelayCommand(async o =>
        {
            await Mailbox.ResetImapConnection();
            OnCloseRequested();
        });

        private async void RunNewEmailDialog(object o)
        {
            IsPopupClose = false;
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

            IsPopupClose = true;
        }

        private async void RunEmailSettingsDialog(object o)
        {
            IsPopupClose = false;

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

            IsPopupClose = true;
        }

        public async Task UpdateFolders()
        {
            try
            {
                await Mailbox.LoadFolders();
                if (Mailbox.MailFolders != null)
                {
                    int inboxIndex = -1;
                    int sentIndex = -1;
                    var folders = new ObservableCollection<FolderItem>();
                    for (var i = 0; i < Mailbox.MailFolders.Count; i++)
                    {
                        var folder = Mailbox.MailFolders[i];
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
                            folders.Add(new FolderItem(folder.Name,
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

        private void ClosingEventHandler(object sender,
            DialogClosingEventArgs eventArgs)
        {
            //if ((bool)eventArgs.Parameter == false) return;

            ////OK, lets cancel the close...
            //eventArgs.Cancel();

            ////...now, lets update the "session" with some new content!
            //eventArgs.Session.UpdateContent(new ProgressDialog());
            ////note, you can also grab the session when the dialog opens via the DialogOpenedEventHandler

            ////lets run a fake operation for 3 seconds then close this baby.
            //Task.Delay(TimeSpan.FromSeconds(3))
            //    .ContinueWith((t, _) => eventArgs.Session.Close(false), null,
            //        TaskScheduler.FromCurrentSynchronizationContext());
        }

        public MainWindowViewModel()
        {
            _isPopupClose = true;
        }
    }
}
