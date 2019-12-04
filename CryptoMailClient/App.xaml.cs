using System;
using System.Windows;
using CryptoMailClient.Classes;

namespace CryptoMailClient
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            MainWindow = new MainWindow();

            LoginWindow loginWindow = new LoginWindow();
            if (loginWindow.ShowDialog() == true)
            {
                ((MainWindow) MainWindow).SetEmailAccount(loginWindow
                    .EmailAccount);
                MainWindow.Show();
            }
            else
            {
                MainWindow.Close();
            }
        }
    }
}
