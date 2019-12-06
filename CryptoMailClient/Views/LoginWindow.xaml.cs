using System;
using System.Windows;
using System.Windows.Input;
using CryptoMailClient.Models;

namespace CryptoMailClient.Views
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
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
            if (!IsUserDataValid())
            {
                return;
            }

            if (UserManager.SignIn(Login.Text, Password.Password))
            {
                DialogResult = true;
            }
            else
            {
                MessageBox.Show("Пользователь с данным логином" +
                                " и паролем не зарегистрирован.",
                    Title, MessageBoxButton.OK);
            }
        }

        private void SignUp_OnClick(object sender, RoutedEventArgs e)
        {
            if (!IsUserDataValid())
            {
                return;
            }

            if (UserManager.SignUp(Login.Text, Password.Password))
            {
                MessageBox.Show("Вы успешно зарегистрированы!",
                    Title, MessageBoxButton.OK);
            }
            else
            {
                MessageBox.Show("Пользователь с данным логином уже" +
                                " зарегистрирован!", Title, MessageBoxButton.OK);
            }
        }

        private bool IsUserDataValid()
        {
            if (string.IsNullOrWhiteSpace(Login.Text))
            {
                MessageBox.Show("Логин не задан. Заполните поле логина.",
                    Title);
                return false;
            }

            if (Login.Text.Length < 3)
            {
                MessageBox.Show(
                    "Логин слишком короткий. Логин должен состоять " +
                    "минимум из 3 символов.", Title);
                return false;
            }

            if (string.IsNullOrWhiteSpace(Password.Password))
            {
                MessageBox.Show("Пароль не задан. Заполните поле пароля.",
                    Title);
                return false;
            }

            if (Password.Password.Length < 4)
            {
                MessageBox.Show(
                    "Пароль слишком короткий. Пароль должен состоять " +
                    "минимум из 4 символов.", Title);
                return false;
            }

            return true;
        }
    }
}
