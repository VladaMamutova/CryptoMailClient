using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CryptoMailClient.ViewModels;

namespace CryptoMailClient.Views
{
    /// <summary>
    /// Логика взаимодействия для LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            LoginWindowViewModel loginViewModel = new LoginWindowViewModel();
            loginViewModel.MessageBoxDisplayRequested += (sender, e) =>
            {
                CustomMessageBox.Show(e.MessageBoxText, e.Caption);
            };
            loginViewModel.CloseDialogRequested += result => DialogResult = result;
            loginViewModel.ClearPasswordFieldsRequested += () =>
            {
                Password.Clear();
                PasswordConfirmation.Clear();
            };
            DataContext = loginViewModel;
        }

        private void Window_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void Password_OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext != null && sender is PasswordBox passwordBox)
            {
                ((LoginWindowViewModel)DataContext).SetPassword(passwordBox
                    .SecurePassword);
            }
        }

        private void PasswordConfirmation_OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext != null && sender is PasswordBox passwordBox)
            {
                ((LoginWindowViewModel)DataContext).SetPasswordConfirmation(
                    passwordBox.SecurePassword);
            }
        }
    }
}
