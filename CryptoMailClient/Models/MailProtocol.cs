using System;

namespace CryptoMailClient.Models
{
    public struct MailProtocol
    {
        public const int DEFAULT_SMTP_PORT = 465;
        // 465 порт использует для шифрования SSL, 587 и 25 без шифрования.

        public const int DEFAULT_IMAP_PORT = 993;
        // 993 порт пользуется протоколом SSL для шифрования, 143 - без шифрования.

        public string Server { get; }
        public int Port { get; private set; }
        public bool UseSslTsl { get; }

        public MailProtocol(MailProtocols mailProtocol, string domain, int port,
            bool useSslTsl = true)
        {
            if (domain == null)
            {
                throw new ArgumentNullException(nameof(domain));
            }

            if (domain != "gmail.com" &&
                domain != "mail.ru" &&
                !domain.StartsWith("yandex."))
            {
                throw new ArgumentException("Invalid server domain.");
            }

            Server = mailProtocol.ToString().ToLower() + '.' + domain;
            Port = port;
            UseSslTsl = useSslTsl;
        }

        public void SetPort(int port)
        {
            Port = port;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is MailProtocol))
            {
                return false;
            }

            MailProtocol protocolConfig = (MailProtocol) obj;

            return string.Compare(Server, protocolConfig.Server,
                       StringComparison.CurrentCulture) == 0 &&
                   Port == protocolConfig.Port &&
                   UseSslTsl == protocolConfig.UseSslTsl;
        }

        public override int GetHashCode()
        {
            return Server.GetHashCode();
        }

        public override string ToString()
        {
            return $"{Server}:{Port}, {UseSslTsl}";
        }

        public static string GetServerFromMailAddress(
            MailProtocols mailProtocol,
            string address)
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            return mailProtocol.ToString().ToLower() + '.' +
                   address.Substring(address.IndexOf('@') + 1);
        }
    }
}