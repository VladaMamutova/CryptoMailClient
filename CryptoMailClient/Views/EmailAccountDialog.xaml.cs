using System.Windows;
using System.Windows.Controls;
using CryptoMailClient.ViewModels;

namespace CryptoMailClient.Views
{
    /// <summary>
    /// Логика взаимодействия для EmailSettingsWindow.xaml
    /// </summary>
    public partial class EmailSettingsDialog : UserControl
    {
        public EmailSettingsDialog()
        {
            InitializeComponent();
        }

        public EmailSettingsDialog(bool isNewEmailAccount)
        {
            InitializeComponent();
            var viewModel = new EmailAccountDialogViewModel(isNewEmailAccount);
            viewModel.MessageBoxDisplayRequested += (s, o) =>
            {
                CustomMessageBox.Show(o.MessageBoxText, o.Caption);
            };
            DataContext = viewModel;
        }

        private void Password_OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext != null && sender is PasswordBox passwordBox)
            {
                ((EmailAccountDialogViewModel) DataContext).SecurePassword =
                    passwordBox.SecurePassword;
            }
        }
    }
}
