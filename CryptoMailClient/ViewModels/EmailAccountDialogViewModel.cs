using System;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using CryptoMailClient.Models;
using CryptoMailClient.Utilities;
using CryptoMailClient.Views;
using Ionic.Zip;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;

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
            MailProtocol.GetMessageAboutValidPorts(MailProtocol.MailProtocols
                .SMTP);

        public string ImapPortHelpMessage =>
            MailProtocol.GetMessageAboutValidPorts(MailProtocol.MailProtocols
                .IMAP);

        public RelayCommand AddCommand { get; }
        public RelayCommand UpdateCommand { get; }
        public RelayCommand DeleteCommand { get; }
        public RelayCommand ImportKeysCommand { get; }
        public RelayCommand ExportKeysCommand { get; }

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
            ImportKeysCommand = new RelayCommand(ImportKeys);
            ExportKeysCommand =
                new RelayCommand(ExportKeys, o => !isNewEmailAccount);
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

        private async void Delete(object o)
        {
            UserManager.CurrentUser.RemoveEmailAccount(Address);
            UserManager.SaveCurrectUserInfo(Address);
            await Mailbox.ResetState();
            DialogHost.CloseDialogCommand.Execute(true, null);
        }

        private void ImportKeys(object obj)
        {
            var view = new ImportExportKeysDialog(true);
            if (view.ShowDialog() != true) return;

            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                InitialDirectory = Environment.CurrentDirectory,
                Filter = "*.zip|*.zip"
            };

            if (openFileDialog.ShowDialog() != true) return;

            string zipFileName = openFileDialog.FileName;
            string keyFileName =
                Path.GetFileNameWithoutExtension(zipFileName) + ".key";

            string content;
            try
            {
                using (ZipFile zip = new ZipFile(zipFileName, Encoding.UTF8))
                {
                    if (zip.EntryFileNames.Contains(keyFileName))
                    {
                        ZipEntry entry = zip.Entries.ToList()
                            .Find(e => e.FileName == keyFileName);
                        using (var sr = new StreamReader(
                            entry.OpenReader(view.Password.Password),
                            Encoding.UTF8))
                        {
                            content = sr.ReadToEnd();
                        }
                    }
                    else
                    {
                        OnMessageBoxDisplayRequest(Title,
                            "Ключи не были импортированы в ваш аккаунт.\n" +
                            "Файл архива не содержит " +
                            $"файл с ключами: \"{keyFileName}\".");
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                OnMessageBoxDisplayRequest(Title,
                    "Ключи не были импортированы в ваш аккаунт.\n" +
                    "Невозможно прочитать данные из файла. " +
                    ex.Message);
                return;
            }

            try
            {
                var rsa = new RSACryptoServiceProvider();
                rsa.FromXmlString(content);
                if (rsa.PublicOnly)
                {
                    OnMessageBoxDisplayRequest(Title,
                        "Ключи не были импортированы в ваш аккаунт.\n" +
                        "Файл содержит только публичный ключ.");
                }
                else
                {
                    UserManager.CurrentUser.CurrentEmailAccount
                        .SetRsaFullKeyPair(rsa.ToXmlString(true));
                    UserManager.SaveCurrectUserInfo();
                    OnMessageBoxDisplayRequest(Title,
                        "Публичный и приватный ключи были " +
                        "успешно импортированы в ваш аккаунт.");
                }
            }
            catch
            {
                OnMessageBoxDisplayRequest(Title,
                    "Ключи не были импортированы в ваш аккаунт.\n" +
                    "Файл содержит не содержит приватный и публичный ключ.");
            }
        }

        private void ExportKeys(object obj)
        {
            var view = new ImportExportKeysDialog(false);
            if (view.ShowDialog() != true) return;

            bool exportPrivateKey =
                view.PrivateKeyRadioButton.IsChecked.HasValue &&
                view.PrivateKeyRadioButton.IsChecked.Value;

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                InitialDirectory = Environment.CurrentDirectory,
                FileName = UserManager.CurrentUser.CurrentEmailAccount.Address,
                Filter = exportPrivateKey ? "*.zip|*.zip" : "*.key|*.key"
            };

            if (saveFileDialog.ShowDialog() != true) return;

            string fileName = saveFileDialog.FileName;

            if (exportPrivateKey)
            {
                ExportPrivateKey(fileName, view.Password.Password);
            }
            else
            {
                ExportPublicKey(fileName);
            }
        }

        private void ExportPublicKey(string keyFileName)
        {
            try
            {
                using (StreamWriter writer =
                    new StreamWriter(keyFileName, false))
                {
                    writer.Write(UserManager.CurrentUser.CurrentEmailAccount
                        .RsaPublicKey);
                }
                OnMessageBoxDisplayRequest(Title,
                    "Публичный ключ был успешно экспортирован.");
            }
            catch(Exception ex)
            {
                OnMessageBoxDisplayRequest(Title,
                    "Ключ не был экспортирован. " + ex.Message);
            }
        }

        private void ExportPrivateKey(string zipFileName, string password)
        {
            string keyFileName =
                Path.GetFileNameWithoutExtension(zipFileName) + ".key";

            try
            {
                using (ZipFile zip = new ZipFile(zipFileName, Encoding.UTF8))
                {
                    zip.CompressionLevel = Ionic.Zlib.CompressionLevel.Default;
                    zip.TempFileFolder = Path.GetTempPath();
                    zip.Password = password;
                    zip.AddEntry(keyFileName,
                        Encoding.UTF8.GetBytes(UserManager.CurrentUser
                            .CurrentEmailAccount.RsaPrivateKey));
                    zip.Save();
                }
                OnMessageBoxDisplayRequest(Title,
                    "Публичный и приватный ключи были успешно экспортированы.");
            }
            catch (Exception ex)
            {
                OnMessageBoxDisplayRequest(Title,
                    "Ключи не были экспортированы. " + ex.Message);
            }
        }
    }
}
