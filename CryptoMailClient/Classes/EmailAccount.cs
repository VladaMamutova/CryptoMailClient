using System;
using System.Net.Mail;
using System.Net.Sockets;
using MailKit;
using MailKit.Net.Imap;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace CryptoMailClient.Classes
{
    public class EmailAccount
    {
        public string Address { get; }
        private readonly string _password;

        public ProtocolConfig SmtpConfig;
        public ProtocolConfig ImapConfig;

        public int TotalCount { get; set; }

        public EmailAccount(string address, string password)
        {
            MailAddress mailAddress;
            try
            {
                mailAddress = new MailAddress(address);
            }
            catch
            {
                throw new Exception("Адрес электронной почты имеет неверный формат.");
            }

            if (mailAddress.Host != "gmail.com" && !mailAddress.Host.StartsWith("yandex."))
            {
                throw new ArgumentException("Адрес электронной почты указан неверно. " +
                                            "Выберите почтовый ящик на сервисе Gmail, " +
                                            "Яндекс.Почта или Mail.ru.");
            }

            Address = address;
            _password = password;

            SmtpConfig = new ProtocolConfig("smtp." + mailAddress.Host, 465);
            // 465 порт использует для шифрования SSL, 587 и 25 без шифрования.
            ImapConfig = new ProtocolConfig("imap." + mailAddress.Host, 993);
            // 993 порт пользуется протоколом SSL для шифрования, 143 - без шифрования.
        }

        public void Connect()
        {

            try
            {
                using (var client = new SmtpClient())
            {
                client.Connect(SmtpConfig.Server, SmtpConfig.Port,
                    SmtpConfig.UseSslTsl);
                client.Authenticate(Address, _password);

                client.Disconnect(true);
            }
                using (var client = new ImapClient())
                {
                    client.ServerCertificateValidationCallback =
                        (s, c, h, e) => true;
                    client.Connect(ImapConfig.Server,
                        ImapConfig.Port, ImapConfig.UseSslTsl);
                    client.Authenticate(Address,
                        _password);

                    var inbox = client.Inbox;
                    inbox.Open(FolderAccess.ReadOnly);
                    TotalCount = inbox.Count;
                    client.Disconnect(true);
                }
            }
            catch (SocketException ex)
            {
                string message = "Невозможно установить соединение с сервером.";
                if (ex.ErrorCode == 11001)
                {
                    throw new Exception(message + " Проверьте " +
                                        "подключение к интернету.");
                }
                throw new Exception(message);
            }
            catch
            {
                throw new Exception("Логин или пароль неверны.");
            }
        }
    }
}