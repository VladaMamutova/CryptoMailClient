using System;
using System.Runtime.InteropServices;
using System.Security;
using CryptoMailClient.Models;
using CryptoMailClient.Utilities;

namespace CryptoMailClient.ViewModels
{
    public class LoginWindowViewModel : ViewModelBase
    {
        private string _login;
        public string Login
        {
            get => _login;
            set
            {
                _login = value;
                OnPropertyChanged(nameof(Login));
            }
        }

        public SecureString SecurePassword { private get; set; }
        public SecureString SecurePasswordConfirmation { private get; set; }

        private bool _isRegistration;
        public bool IsRegistration
        {
            get => _isRegistration;
            set
            {
                _isRegistration = value;
                OnPropertyChanged(nameof(IsRegistration));
                OnPropertyChanged(nameof(Title));
                OnPropertyChanged(nameof(CommandName));
                OnPropertyChanged(nameof(AlternateCommandName));
            }
        }

        public string Title => _isRegistration ? "Регистрация" : "Вход";

        public string CommandName =>
            _isRegistration ? "Зарегистрироваться" : "Войти";

        public string AlternateCommandName =>
            _isRegistration ? "Отмена" : "Создать аккаунт";

        public RelayCommand Command { get; }
        public RelayCommand AlternateCommand { get; }

        public LoginWindowViewModel()
        {
            Login= string.Empty;
            SecurePassword = new SecureString();
            SecurePasswordConfirmation = new SecureString();
            
            IsRegistration = false;

            Command = new RelayCommand(o =>
            {
                if (IsRegistration)
                {
                    SignUp();
                }
                else
                {
                    SignIn();
                }
            });

            AlternateCommand = new RelayCommand(o =>
            {
                IsRegistration = !IsRegistration;
            });
        }

        private void SignIn()
        {
            if (UserManager.SignIn(Login, SecurePassword.ToString()))
            {
                OnCloseRequested();
            }
            else
            {
                OnMessageBoxDisplayRequest(Title,
                    "Пользователь с данным логином" +
                    " и паролем не зарегистрирован.");
            }
        }

        private void SignUp()
        {
            if (IsUserDataValid())
            {
                if (!ComparePasswords(SecurePassword,
                    SecurePasswordConfirmation))
                {
                    OnMessageBoxDisplayRequest(Title, "Пароли не совпадают.");
                }
                else
                {
                    OnMessageBoxDisplayRequest(Title,
                        UserManager.SignUp(Login, SecurePassword.ToString())
                            ? "Вы успешно зарегистрированы."
                            : "Пользователь с данным логином уже зарегистрирован");
                }
            }
        }

        private bool IsUserDataValid()
        {
            bool result = true;
            //todo::проверка на латиницу и цифры
            if (string.IsNullOrWhiteSpace(Login))
            {
                OnMessageBoxDisplayRequest(Title,
                    "Логин не может быть пустым.");
                result = false;
            }
            else if (Login.Length < 3)
            {
                OnMessageBoxDisplayRequest(Title,
                    "Логин должен состоять минимум из 3 символов.");
                result = false;
            }
            else if (string.IsNullOrWhiteSpace(SecurePassword.ToString()))
            {
                OnMessageBoxDisplayRequest(Title,
                    "Пароль не может быть пустым.");
                result = false;
            }
            else if (SecurePassword.Length < 4)
            {
                OnMessageBoxDisplayRequest(Title,
                    "Пароль должен состоять минимум из 4 символов.");
                result = false;
            }

            return result;
        }

        private bool ComparePasswords(SecureString password1, SecureString password2)
        {
            bool result = true;
            if (ReferenceEquals(password1, password2))
            {
                result = false;
            }
            else if (password1 == null || password2 == null)
            {
                result = false;
            }
            else if (password1.Length != password2.Length)
            {
                result = false;
            }
            else
            {
                IntPtr value1 = IntPtr.Zero;
                IntPtr value2 = IntPtr.Zero;
                try
                {
                    value1 =
                        Marshal.SecureStringToGlobalAllocUnicode(password1);
                    value2 =
                        Marshal.SecureStringToGlobalAllocUnicode(password1);
                    for (int i = 0; i < password1.Length && result; i++)
                    {
                        short char1 = Marshal.ReadInt16(value1, i * 2);
                        short char2 = Marshal.ReadInt16(value2, i * 2);
                        if (char1 != char2)
                        {
                            result = false;
                        }
                    }
                }
                catch
                {
                    result = false;
                }
                finally
                {
                    Marshal.ZeroFreeGlobalAllocUnicode(value1);
                    Marshal.ZeroFreeGlobalAllocUnicode(value2);
                }
            }

            return result;
        }
    }
}