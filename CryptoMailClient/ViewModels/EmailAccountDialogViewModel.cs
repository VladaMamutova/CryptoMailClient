using System;
using System.Security;
using System.Threading.Tasks;
using CryptoMailClient.Models;
using CryptoMailClient.Utilities;
using MaterialDesignThemes.Wpf;

namespace CryptoMailClient.ViewModels
{
    class EmailAccountDialogViewModel : ViewModelBase
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
            MailProtocol.MailProtocols.SMTP,
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
            MailProtocol.MailProtocols.IMAP,
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
            IsNewEmailAccount ? "Добавление ящика" : "Управление аккаунтом";

        public bool IsReadOnly => !IsNewEmailAccount;

        public string SmtpPortHelpMessage =>
            MailProtocol.GetMessageAboutValidPorts(MailProtocol.MailProtocols.SMTP);

        public string ImapPortHelpMessage =>
            MailProtocol.GetMessageAboutValidPorts(MailProtocol.MailProtocols.IMAP);

        public RelayCommand AddCommand { get; }
        public RelayCommand UpdateCommand { get; }

        public RelayCommand DeleteCommand { get; }

        public EmailAccountDialogViewModel(bool isNewEmailAccount)
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

            AddCommand = new RelayCommand(Add);
            UpdateCommand = new RelayCommand(Update);
            DeleteCommand = new RelayCommand(Delete);
        }

        private async void Add(object o)
        {
            try
            {
                EmailAccount emailAccount = new EmailAccount(Address,
                    SecureStringHelper.SecureStringToString(SecurePassword),
                    _smtpPort, _imapPort);

                // Асинхронно вызываем метод подключения к почтовым серверам,
                // чтобы не блокировать интерфейс.
                await emailAccount.Connect();

                if (!UserManager.CurrentUser.AddEmailAccount(emailAccount))
                {
                    throw new Exception("Данный почтовый ящик уже добавлен.");
                }

                UserManager.SaveCurrectUserInfo();
                DialogHost.CloseDialogCommand.Execute(true, null);
            }
            catch (Exception ex)
            {
                OnMessageBoxDisplayRequest(Title, ex.Message);
            }
        }

        private async void Update(object o)
        {
            // Могут меняться только порты серверов, так как поле
            // email address - IsReadOnly, соответственно,
            // и почтовые сервера не меняются.
            try
            {
                UserManager.CurrentUser.CurrentEmailAccount.SetSmtpPort(
                    _smtpPort);
                UserManager.CurrentUser.CurrentEmailAccount.SetImapPort(
                    _imapPort);

                // Асинхронно вызываем метод подключения к почтовым серверам,
                // чтобы не блокировать интерфейс.
                await Task.Run(() =>
                    UserManager.CurrentUser.CurrentEmailAccount.Connect());

                UserManager.SaveCurrectUserInfo();
                DialogHost.CloseDialogCommand.Execute(true, null);
            }
            catch (Exception ex)
            {
                OnMessageBoxDisplayRequest(Title, ex.Message);
            }
        }

        private void Delete(object o)
        {
            UserManager.CurrentUser.RemoveEmailAccount(Address);
            UserManager.SaveCurrectUserInfo();
            DialogHost.CloseDialogCommand.Execute(true, null);
        }
    }
}
