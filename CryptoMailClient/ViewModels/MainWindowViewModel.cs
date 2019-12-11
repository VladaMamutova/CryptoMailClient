using System.Collections.Generic;
using System.Linq;
using CryptoMailClient.Models;
using CryptoMailClient.Utilities;
using CryptoMailClient.Views;
using MaterialDesignThemes.Wpf;

namespace CryptoMailClient.ViewModels
{
    class MainWindowViewModel : ViewModelBase
    {
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

        public RelayCommand RunNewEmailDialogCommand =>
            new RelayCommand(RunNewEmailDialog);

        public RelayCommand RunEmailSettingsDialogCommand =>
            new RelayCommand(RunEmailSettingsDialog);

        public RelayCommand SetEmailAccountCommand =>
            new RelayCommand(address =>
            {
                bool result = UserManager.CurrentUser
                    .SetCurrentEmailAccount((address ?? "").ToString());
                if (result)
                {
                    OnPropertyChanged(nameof(CurrentEmailAddress));
                    OnPropertyChanged(nameof(EmailAccounts));
                }
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
