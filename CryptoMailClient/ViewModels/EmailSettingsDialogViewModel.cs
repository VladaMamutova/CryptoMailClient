using System;
using System.Security;
using CryptoMailClient.Models;
using CryptoMailClient.Utilities;
using MaterialDesignThemes.Wpf;

namespace CryptoMailClient.ViewModels
{
    class EmailSettingsDialogViewModel : ViewModelBase
    {
        private readonly EmailAccount _emailAccount;

        public string Login
        {
            get => _emailAccount.Address;
            set
            {
                _emailAccount.SetAddress(value);
                OnPropertyChanged(nameof(Login));
                OnPropertyChanged(nameof(SmtpServer));
                OnPropertyChanged(nameof(ImapServer));
            }
        }

        public SecureString SecurePassword { private get; set; }

        public string SmtpServer => _emailAccount.SmtpConfig.Server;
        public string SmtpPort
        {
            get =>
                _emailAccount.SmtpConfig.Port ==
                EmailAccount.Empty.SmtpConfig.Port
                    ? string.Empty
                    : _emailAccount.SmtpConfig.Port.ToString();
            set
            {
                // Изменяем значение порта, только если было введено число.
                // Если строка пустая, то приравниваем значение порта
                // значению из пустого объекта Email Account.
                if (string.IsNullOrWhiteSpace(value))
                {
                    _emailAccount.SmtpConfig.Port = EmailAccount.Empty.SmtpConfig.Port;
                    OnPropertyChanged(nameof(SmtpPort));
                }
                else if (int.TryParse(value, out int newPort))
                {
                    
                    _emailAccount.SmtpConfig.Port = newPort;
                    OnPropertyChanged(nameof(SmtpPort));
                }
            }
        }

        public string ImapServer => _emailAccount.ImapConfig.Server;
        public string ImapPort
        {
            get => _emailAccount.ImapConfig.Port ==
                   EmailAccount.Empty.ImapConfig.Port
                ? string.Empty
                : _emailAccount.ImapConfig.Port.ToString();
            set
            {
                // Изменяем значение порта, только если было введено число.
                // Если строка пустая, то приравниваем значение порта
                // значению из пустого объекта Email Account.
                if (string.IsNullOrWhiteSpace(value))
                {
                    _emailAccount.ImapConfig.Port = EmailAccount.Empty.ImapConfig.Port;
                    OnPropertyChanged(nameof(ImapPort));
                }
                else if (int.TryParse(value, out int newPort))
                {

                    _emailAccount.ImapConfig.Port = newPort;
                    OnPropertyChanged(nameof(ImapPort));
                }
            }
        }

        public bool IsNewEmailAccount { get; }

        public string Title => IsNewEmailAccount ? "Добавление ящика" : "Настройки";
        public bool IsReadOnly => !IsNewEmailAccount;

        public RelayCommand DeleteCommand => new RelayCommand(o =>
        {
            UserManager.CurrentUser.RemoveEmailAccount(_emailAccount);
            DialogHost.CloseDialogCommand.Execute(true, null);
        });

        public RelayCommand AddCommand => new RelayCommand(o =>
        {
            UserManager.CurrentUser.AddEmailAccount(_emailAccount);
            DialogHost.CloseDialogCommand.Execute(true, null);
        });

        public RelayCommand UpdateCommand => new RelayCommand(o =>
        {
            UserManager.CurrentUser.CurrentEmailAccount.SetSmtpProtocolConfig(_emailAccount.SmtpConfig);
            UserManager.CurrentUser.CurrentEmailAccount.SetImapProtocolConfig(_emailAccount.ImapConfig);
            DialogHost.CloseDialogCommand.Execute(true, null);
        });

        public EmailSettingsDialogViewModel(bool isNewEmailAccount)
        {
            IsNewEmailAccount = isNewEmailAccount;
            _emailAccount = IsNewEmailAccount
                ? EmailAccount.Empty.DeepClone()
                : UserManager.CurrentUser.CurrentEmailAccount;
        }
    }
}

