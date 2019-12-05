using System.Windows;
using CryptoMailClient.Models;

namespace CryptoMailClient.Views
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private EmailAccount _emailAccount;

        public MainWindow()
        {
            InitializeComponent();
        }

        public void SetEmailAccount(EmailAccount emailAccount)
        {
            _emailAccount = emailAccount;
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            //TotalCount.Text = _emailAccount.TotalCount.ToString();
        }
    }
}
