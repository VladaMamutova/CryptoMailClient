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
            var viewModel = new EmailSettingsDialogViewModel(isNewEmailAccount);
            viewModel.MessageBoxDisplayRequested += (s, o) =>
            {
                MessageBox.Show(o.MessageBoxText, o.Caption);
            };
            DataContext = viewModel;
        }

        private void Password_OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext != null && sender is PasswordBox passwordBox)
            {
                ((EmailSettingsDialogViewModel) DataContext).SecurePassword =
                    passwordBox.SecurePassword;
            }
        }
    }
}
