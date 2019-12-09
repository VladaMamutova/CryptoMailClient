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
