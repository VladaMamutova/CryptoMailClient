using CryptoMailClient.Models;
using CryptoMailClient.Utilities;
using CryptoMailClient.Views;
using MaterialDesignThemes.Wpf;

namespace CryptoMailClient.ViewModels
{
    class MainWindowViewModel:ViewModelBase
    {
        public MainWindowViewModel()
        {

        }

        public RelayCommand RunDialogCommand =>
            new RelayCommand(ExecuteRunDialog);

        private async void ExecuteRunDialog(object o)
        {
            EmailAccount emailAccount = EmailAccount.Empty.DeepClone();
            var view = new EmailSettingsDialog
            {
                DataContext = new EmailSettingsDialogViewModel(emailAccount)
            };

            await DialogHost.Show(view, "RootDialog", ClosingEventHandler);
            OnMessageBoxDisplayRequest("", emailAccount.ToString());
        }

        private void ClosingEventHandler(object sender,
            DialogClosingEventArgs eventArgs)
        {
            //if ((bool)eventArgs.Parameter == false) return;

            ////OK, lets cancel the close...
            //eventArgs.Cancel();

            ////...now, lets update the "session" with some new content!
            //eventArgs.Session.UpdateContent(new SampleProgressDialog());
            ////note, you can also grab the session when the dialog opens via the DialogOpenedEventHandler

            ////lets run a fake operation for 3 seconds then close this baby.
            //Task.Delay(TimeSpan.FromSeconds(3))
            //    .ContinueWith((t, _) => eventArgs.Session.Close(false), null,
            //        TaskScheduler.FromCurrentSynchronizationContext());
        }
    }
}
