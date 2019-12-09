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
        private readonly EmailAccount _sourceEmailAccount;

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

        private bool _isNewEmailAccount;
        public bool IsNewEmailAccount
        {
            get => _isNewEmailAccount;
            set
            {
                _isNewEmailAccount = value;
                OnPropertyChanged(nameof(IsNewEmailAccount));
                OnPropertyChanged(nameof(Title));
                OnPropertyChanged(nameof(IsReadOnly));
            }
        }

        public string Title => _isNewEmailAccount ? "Добавление ящика" : "Настройки";
        public bool IsReadOnly => !_isNewEmailAccount;

        public RelayCommand DeleteCommand => new RelayCommand(o =>
        {
            _sourceEmailAccount.Set(EmailAccount.Empty);

            DialogHost.CloseDialogCommand.Execute(true, null);
        });

        public RelayCommand UpdateCommand => new RelayCommand(o =>
        {
            _sourceEmailAccount.SetSmtpProtocolConfig(_emailAccount.SmtpConfig);
            _sourceEmailAccount.SetImapProtocolConfig(_emailAccount.ImapConfig);

            DialogHost.CloseDialogCommand.Execute(true, null);
        });
        
        public EmailSettingsDialogViewModel(EmailAccount emailAccount)
        {
            if (emailAccount == null)
            {
                throw new ArgumentException(nameof(emailAccount));
            }

            IsNewEmailAccount = !emailAccount.Equals(EmailAccount.Empty);
            _emailAccount = emailAccount.DeepClone();
            _sourceEmailAccount = emailAccount;
        }
    }
}

