using System;
using System.Windows;
using System.Windows.Input;
using CryptoMailClient.Classes;

namespace CryptoMailClient
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        public EmailAccount EmailAccount;

        public LoginWindow()
        {
            InitializeComponent();
        }

        private void Window_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void Close_MouseDown(object sender, MouseButtonEventArgs e)
        {
            DialogResult = false;
        }

        private void SignIn_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                EmailAccount = new EmailAccount(Login.Text, Password.Password);
                EmailAccount.Connect();
                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось войти в почтовый ящик с введёнными " +
                                "входными данными." + Environment.NewLine +
                                ex.Message, "Ошибка авторизации", MessageBoxButton.OK);
            }
            //InitializePages();

            //LoginPage.Visibility = Visibility.Collapsed;
            //MailBoxPage.Visibility = Visibility.Visible;

            //UserEmail.Text = Login.Text;
            //Main.Navigate(loadPage);
            //await incomingEmailsPage.RecieveFromStart(true);
            //Main.Navigate(incomingEmailsPage);
        }
    }
}
