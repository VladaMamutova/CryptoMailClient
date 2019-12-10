using System;
using System.Security;
using CryptoMailClient.Models;
using CryptoMailClient.Utilities;
using MaterialDesignThemes.Wpf;

namespace CryptoMailClient.ViewModels
{
    class EmailSettingsDialogViewModel : ViewModelBase
    {
        private string _address;

        public string Address
        {
            get => _address;
            set
            {
                _address = value;
                OnPropertyChanged(nameof(Address));
                OnPropertyChanged(nameof(SmtpServer));
                OnPropertyChanged(nameof(ImapServer));
            }
        }

        public SecureString SecurePassword { private get; set; }

        public string SmtpServer => MailProtocol.GetServerFromMailAddress(
            MailProtocols.SMTP,
            Address);

        private int _smtpPort;

        public string SmtpPort
        {
            get => _smtpPort == -1 ? string.Empty : _smtpPort.ToString();
            set
            {
                // Изменяем значение порта, только если было введено число.
                // Если строка пустая, то приравниваем значение порта значению -1.
                if (string.IsNullOrWhiteSpace(value))
                {
                    _smtpPort = -1;
                    OnPropertyChanged(nameof(SmtpPort));
                }
                else if (int.TryParse(value, out int newPort))
                {
                    _smtpPort = newPort;
                    OnPropertyChanged(nameof(SmtpPort));
                }
            }
        }

        public string ImapServer => MailProtocol.GetServerFromMailAddress(
            MailProtocols.IMAP,
            Address);

        private int _imapPort;

        public string ImapPort
        {
            get => _imapPort == -1 ? string.Empty : _imapPort.ToString();
            set
            {
                // Изменяем значение порта, только если было введено число.
                // Если строка пустая, то приравниваем значение порта значению -1.
                if (string.IsNullOrWhiteSpace(value))
                {
                    _imapPort = -1;
                    OnPropertyChanged(nameof(ImapPort));
                }
                else if (int.TryParse(value, out int newPort))
                {

                    _imapPort = newPort;
                    OnPropertyChanged(nameof(ImapPort));
                }
            }
        }

        public bool IsNewEmailAccount { get; }

        public string Title =>
            IsNewEmailAccount ? "Добавление ящика" : "Настройки";

        public bool IsReadOnly => !IsNewEmailAccount;

        public RelayCommand DeleteCommand => new RelayCommand(o =>
        {
            //todo:добавить connect
            //todo:попробовать отправлять объект наружу для отображения диалога с прогресс баром
            UserManager.CurrentUser.RemoveEmailAccount(Address);
            DialogHost.CloseDialogCommand.Execute(true, null);
        });

        public RelayCommand AddCommand => new RelayCommand(o =>
        {
            try
            {
                EmailAccount emailAccount = new EmailAccount(Address,
                    SecureStringHelper.SecureStringToString(SecurePassword),
                    _smtpPort, _imapPort);

                //todo:добавить connect
                //todo:попробовать отправлять объект наружу для отображения диалога с прогресс баром
                UserManager.CurrentUser.AddEmailAccount(emailAccount);
                DialogHost.CloseDialogCommand.Execute(true, null);
            }
            catch (Exception ex)
            {
                OnMessageBoxDisplayRequest(Title, ex.Message);
            }
        });

        public RelayCommand UpdateCommand => new RelayCommand(o =>
        {
            // Могут меняться только порты серверов, так как поле
            // email address - IsReadOnly, соответственно,
            // и постовые сервера не меняются.
            //todo:добавить connect
            //todo:попробовать отправлять объект наружу для отображения диалога с прогресс баром
            UserManager.CurrentUser.CurrentEmailAccount.SetSmtpPort(_smtpPort);
            UserManager.CurrentUser.CurrentEmailAccount.SetImapPort(_imapPort);

            DialogHost.CloseDialogCommand.Execute(true, null);
        });

        public EmailSettingsDialogViewModel(bool isNewEmailAccount)
        {
            IsNewEmailAccount = isNewEmailAccount;
            SecurePassword = new SecureString();
            if (IsNewEmailAccount ||
                UserManager.CurrentUser.CurrentEmailAccount == null)
            {
                _address = string.Empty;
                _smtpPort = MailProtocol.DEFAULT_SMTP_PORT;
                _imapPort = MailProtocol.DEFAULT_IMAP_PORT;
            }
            else
            {
                _address = UserManager.CurrentUser.CurrentEmailAccount.Address;
                _smtpPort = UserManager.CurrentUser.CurrentEmailAccount.Smtp
                    .Port;
                _imapPort = UserManager.CurrentUser.CurrentEmailAccount.Imap
                    .Port;
            }
        }
    }
}

