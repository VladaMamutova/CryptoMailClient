using System;
using System.Net.Mail;
using System.Net.Sockets;
using CryptoMailClient.Utilities;
using MailKit;
using MailKit.Net.Imap;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace CryptoMailClient.Models
{
    public class EmailAccount : IDeepCloneable<EmailAccount>
    {
        public string Address { get; private set; }
        private string _password;

        public ProtocolConfig SmtpConfig;
        public ProtocolConfig ImapConfig;

        public int TotalCount { get; set; }

        static EmailAccount() { Empty = new EmailAccount(); }

        public static readonly EmailAccount Empty;

        private EmailAccount()
        {
            Address = string.Empty;
            _password = string.Empty;
            
            SmtpConfig = new ProtocolConfig("", -1);
            ImapConfig = new ProtocolConfig("", -1);

            TotalCount = 0;
        }

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

            if (mailAddress.Host != "gmail.com" && !mailAddress.Host.StartsWith("yandex.") &&
                mailAddress.Host != "mail.ru")
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

        public void SetAddress(string address)
        {
            Address = address;
        }

        public void SetSmtpProtocolConfig(ProtocolConfig smtpProtocolConfig)
        {
            SmtpConfig.Set(smtpProtocolConfig);
        }

        public void SetImapProtocolConfig(ProtocolConfig imapProtocolConfig)
        {
            ImapConfig.Set(imapProtocolConfig);
        }

        public void Set(EmailAccount emailAccount)
        {
            Address = emailAccount.Address;
            _password = emailAccount._password;
            SmtpConfig.Set(emailAccount.SmtpConfig);
            ImapConfig.Set(emailAccount.ImapConfig);
            TotalCount = emailAccount.TotalCount;
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

        public EmailAccount DeepClone()
        {
            EmailAccount clone = new EmailAccount
            {
                Address = Address,
                _password = _password,
                SmtpConfig = SmtpConfig,
                ImapConfig = ImapConfig,
                TotalCount = TotalCount
            };
            return clone;
        }

        object IDeepCloneable.DeepClone()
        {
            return DeepClone();
        }

        public virtual bool Equals(EmailAccount emailAccount)
        {
            if (emailAccount == null)
            {
                return false;
            }

            if (ReferenceEquals(this, emailAccount))
            {
                return true;
            }

            if (string.Compare(Address, emailAccount.Address,
                    StringComparison.CurrentCulture) != 0 ||
                string.Compare(_password, emailAccount._password,
                    StringComparison.CurrentCulture) != 0)
            {
                return false;
            }

            return SmtpConfig.Equals(emailAccount.SmtpConfig) &&
                   ImapConfig.Equals(emailAccount.ImapConfig)
                && TotalCount == emailAccount.TotalCount;
        }

        public override int GetHashCode()
        {
            // ReSharper disable once NonReadonlyMemberInGetHashCode
            return Address.GetHashCode();
        }

        public override string ToString()
        {
            return Address;
        }
    }
}