using System;
using System.ComponentModel;
using System.Linq;
using System.Security;
using CryptoMailClient.Models;
using CryptoMailClient.Utilities;

namespace CryptoMailClient.ViewModels
{
    public class LoginWindowViewModel : ViewModelBase, IDataErrorInfo
    {
        #region Properties

        private string _login;
        public string Login
        {
            get => _login;
            set
            {
                _login = value;
                _loginValidation = true;
                OnPropertyChanged(nameof(Login));
            }
        }

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
            _isRegistration ? "Зарегистрироваться".ToUpper() : "Войти".ToUpper();

        public string AlternateCommandName =>
            _isRegistration ? "Отмена".ToUpper() : "Создать аккаунт".ToUpper();

        private SecureString _securePassword;
        public void SetPassword(SecureString securePassword)
        {
            _securePassword = securePassword.Copy();
            _securePassword.MakeReadOnly();
            PasswordValidation = true;
        }

        private SecureString _securePasswordConfirmation;
        public void SetPasswordConfirmation(SecureString securePassword)
        {
            _securePasswordConfirmation = securePassword.Copy();
            _securePasswordConfirmation.MakeReadOnly();
            ConfirmPasswordValidation = true;
        }

        private bool _loginValidation;

        private bool _passwordValidation;
        public bool PasswordValidation
        {
            get => _passwordValidation;
            set
            {
                _passwordValidation = value;
                OnPropertyChanged(nameof(PasswordValidation));
                OnPropertyChanged(nameof(ConfirmPasswordValidation));
            }
        }

        private bool _confirmPasswordValidation;

        public bool ConfirmPasswordValidation
        {
            get => _confirmPasswordValidation;
            set
            {
                _confirmPasswordValidation = value;
                OnPropertyChanged(nameof(ConfirmPasswordValidation));
            }
        }

        #endregion

        #region Commands

        public RelayCommand Command => new RelayCommand(o =>
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

        public RelayCommand AlternateCommand => new RelayCommand(o =>
        {
            IsRegistration = !IsRegistration;

            // При смене типа действия очищаем все поля и устанавливаем
            // флаги, что пустые поля не проверяеются на ошибки
            // (до первого обновления значения в поле).
            Login = string.Empty;
            _loginValidation = false;
            OnPropertyChanged(nameof(Login));

            ClearPasswordFieldsRequested?.Invoke();
            PasswordValidation = false;
            ConfirmPasswordValidation = false;
            OnPropertyChanged(nameof(PasswordValidation));
            OnPropertyChanged(nameof(ConfirmPasswordValidation));
        });

        #endregion

        #region IDataErrorInfo Members

        public string Error { get; set; }

        public string this[string propertyName]
        {
            get
            {
                Error = string.Empty;
                if (IsRegistration)
                {
                    switch (propertyName)
                    {
                        case nameof(Login):
                            {
                                if (_loginValidation)
                                {
                                    Error = GetLoginValidError(Login);
                                }

                                break;
                            }
                        case nameof(PasswordValidation):
                            {
                                if (PasswordValidation)
                                {
                                    Error = GetPasswordValidError(_securePassword);
                                }

                                break;
                            }
                        case nameof(ConfirmPasswordValidation):
                            {
                                if (ConfirmPasswordValidation)
                                {
                                    Error = GetPasswordComparisonError(
                                        _securePassword,
                                        _securePasswordConfirmation);
                                }

                                break;
                            }
                        default:
                            {
                                Error = string.Empty;
                                break;
                            }
                    }
                }

                return Error;
            }
        }

        #endregion

        public event Action ClearPasswordFieldsRequested;

        public LoginWindowViewModel()
        {
            Login = string.Empty;
            _securePassword = new SecureString();
            _securePasswordConfirmation = new SecureString();

            // При загрузке View пустые поля не будут проверяться на ошибки.
            _loginValidation = false;
            _passwordValidation = false;
            _confirmPasswordValidation = false;

            IsRegistration = false;
        }

        private void SignIn()
        {
            if (UserManager.SignIn(Login,
                SecureStringHelper.SecureStringToString(_securePassword)))
            {
                _securePassword.Dispose();
                _securePasswordConfirmation.Dispose();
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
                if (UserManager.SignUp(Login,
                    SecureStringHelper.SecureStringToString(_securePassword))
                )
                {
                    OnMessageBoxDisplayRequest(Title,
                        "Вы успешно зарегистрированы.");
                    AlternateCommand.Execute(null);
                }
                else
                {
                    OnMessageBoxDisplayRequest(Title,
                        "Пользователь с данным логином уже зарегистрирован.");
                }
            }
        }

        private bool IsUserDataValid()
        {
            bool result = false;

            // После проверки будут отображены все ошибки под полями,
            // поэтому обновляем свойства и уставнавливаем флаги о
            // необходимости проверки.
            _loginValidation = true;
            OnPropertyChanged(Login);

            PasswordValidation = true;
            ConfirmPasswordValidation = true;

            // Последовательно проверяем все поля на ошибки.
            string error = GetLoginValidError(Login);
            if (string.IsNullOrWhiteSpace(error))
            {
                error = GetPasswordValidError(_securePassword);
                if (string.IsNullOrWhiteSpace(error))
                {
                    error = GetPasswordComparisonError(_securePassword,
                        _securePasswordConfirmation);
                    result = string.IsNullOrWhiteSpace(error);
                }
            }

            if (!result)
            {
                OnMessageBoxDisplayRequest(Title, error);
            }

            return result;
        }

        #region Error Validation

        private string GetPasswordValidError(SecureString securePassword)
        {
            string error = string.Empty;

            if (securePassword == null || securePassword.Length == 0)
            {
                error = "Необходимо выбрать пароль.";
            }
            else if (securePassword.Length < 8)
            {
                error = "Пароль слишком короткий.";
            }

            return error;
        }

        private string GetPasswordComparisonError(SecureString securePassword1,
            SecureString securePassword2)
        {
            string error = string.Empty;

            if (!SecureStringHelper.CompareSecureStrings(securePassword1,
                securePassword2))
            {
                error = "Пароли не совпадают.";
            }

            return error;
        }

        private string GetLoginValidError(string login)
        {
            string error = string.Empty;
            if (string.IsNullOrWhiteSpace(login))
            {
                error = "Необходимо выбрать логин.";
            }
            else if (login.Length < 3)
            {
                error = "Логин слишком короткий.";
            }
            else if (login[0] >= '0' && login[0] <= '9')
            {
                error = "Логин не должен начинаться с цифры.";
            }
            else if (login.Any(c => (c < 'a' || c > 'z') &&
                                    (c < 'A' || c > 'Z') &&
                                    (c < '0' || c > '9') &&
                                    c != '.' && c != '-'))
            {
                error = "Логин может содержать латиницу, цифры, точку и дефис.";
            }

            return error;
        }

        #endregion
    }
}