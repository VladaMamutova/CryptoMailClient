using System;
using System.Net.Mail;
using System.Net.Sockets;
using MailKit;
using MailKit.Net.Imap;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace CryptoMailClient.Models
{
    public class EmailAccount
    {
        private readonly string _password;

        public string Address { get; }
        public MailProtocol Smtp { get; }
        public MailProtocol Imap { get; }

        public EmailAccount(string address, string password,
            int smtpPort = MailProtocol.DEFAULT_SMTP_PORT,
            int imapPort = MailProtocol.DEFAULT_IMAP_PORT)
        {
            MailAddress mailAddress;
            try
            {
                mailAddress = new MailAddress(address);
            }
            catch
            {
                throw new Exception(
                    "Адрес электронной почты имеет неверный формат.\n" +
                    "Пример адреса: ivanov@yandex.ru.");
            }

            Address = address;
            _password = password ??
                        throw new ArgumentNullException(nameof(password));

            Smtp = new MailProtocol(MailProtocols.SMTP, mailAddress.Host,
                smtpPort);

            Imap = new MailProtocol(MailProtocols.IMAP, mailAddress.Host,
                imapPort);
        }

        public void SetSmtpPort(int port)
        {
            Smtp.SetPort(port);
        }

        public void SetImapPort(int port)
        {
            Imap.SetPort(port);
        }

        public void Connect()
        {
            try
            {
                using (var client = new SmtpClient())
                {
                    client.Connect(Smtp.Server, Smtp.Port, Smtp.UseSslTsl);
                    client.Authenticate(Address, _password);
                    client.Disconnect(true);
                }

                using (var client = new ImapClient())
                {
                    client.Connect(Imap.Server, Imap.Port, Imap.UseSslTsl);
                    client.Authenticate(Address, _password);
                    client.Disconnect(true);
                }
            }
            catch (SocketException ex)
            {
                string message = "Невозможно установить соединение " +
                                 "с почтовым сервером.";
                if (ex.ErrorCode == 11001)
                {
                    message += "\nПроверьте подключение к интернету.";
                }

                throw new Exception(message);
            }
            catch
            {
                throw new Exception("Логин или пароль неверны.");
            }
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (!(obj is EmailAccount emailAccount))
            {
                return false;
            }

            if (ReferenceEquals(this, emailAccount))
            {
                return true;
            }

            return Address == emailAccount.Address &&
                   _password == emailAccount._password &&
                   Smtp.Equals(emailAccount.Smtp) &&
                   Imap.Equals(emailAccount.Imap);
        }

        public bool Equals(EmailAccount emailAccount)
        {
            if (emailAccount == null)
            {
                return false;
            }

            if (ReferenceEquals(this, emailAccount))
            {
                return true;
            }

            return Address == emailAccount.Address &&
                   _password == emailAccount._password &&
                   Smtp.Equals(emailAccount.Smtp) &&
                   Imap.Equals(emailAccount.Imap);
        }

        public override int GetHashCode()
        {
            return Address.GetHashCode();
        }

        public override string ToString()
        {
            return Address;
        }
    }
}