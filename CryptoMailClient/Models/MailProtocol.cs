using System;
using System.Collections.Generic;
using System.Linq;

namespace CryptoMailClient.Models
{
    public class MailProtocol
    {
        public const int DEFAULT_SMTP_PORT = 465;
        public const int DEFAULT_IMAP_PORT = 993;

        private static readonly Dictionary<int, bool> SmtpConfig =
            new Dictionary<int, bool>
                {{DEFAULT_SMTP_PORT, true}, {25, false}, {587, false}};
        // 465 порт использует соединение шифрования SSL, 25 и 587 - без шифрования.

        private static readonly Dictionary<int, bool> ImapConfig =
            new Dictionary<int, bool> {{DEFAULT_IMAP_PORT, true}, {143, false}};
        // 993 порт использует соединение шифрования SSL, 143 - без шифрования.

        private readonly MailProtocols _mailProtocol;
        public string Server { get; }
        public int Port { get; private set; }
        public bool UseSsl { get; private set; }
       
        public MailProtocol(MailProtocols mailProtocol, string domain, int port)
        {
            if (domain == null)
            {
                throw new ArgumentNullException(nameof(domain));
            }

            if (domain != "gmail.com" &&
                domain != "mail.ru" &&
                !domain.StartsWith("yandex."))
            {
                throw new ArgumentException("Недействительное доменное имя " +
                                            "почтового сервера. Выберите почтовый " +
                                            "ящик на сервисе Gmail, Яндекс.Почта " +
                                            "или Mail.ru.");
            }

            _mailProtocol = mailProtocol;
            Server = _mailProtocol.ToString().ToLower() + '.' + domain;
            Port = 0;
            UseSsl = false;
            SetPort(port);
        }

        public void SetPort(int port)
        {
            bool result = false;
            switch (_mailProtocol)
            {
                case MailProtocols.SMTP:
                {
                    if (SmtpConfig.ContainsKey(port))
                    {
                        Port = port;
                        UseSsl = SmtpConfig[port];
                        result = true;
                    }

                    break;
                }
                case MailProtocols.IMAP:
                {
                    if (ImapConfig.ContainsKey(port))
                    {
                        Port = port;
                        UseSsl = ImapConfig[port];
                        result = true;
                    }

                    break;
                }
            }

            if (!result)
            {
                throw new ArgumentException(
                    $"Недействительный порт {_mailProtocol}.\n" +
                    GetMessageAboutValidPorts(_mailProtocol));
            }
        }
        
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (!(obj is MailProtocol mailProtocol))
            {
                return false;
            }

            if (ReferenceEquals(this, mailProtocol))
            {
                return true;
            }

            return Server == mailProtocol.Server &&
                   Port == mailProtocol.Port &&
                   UseSsl == mailProtocol.UseSsl;
        }

        public bool Equals(MailProtocol mailProtocol)
        {
            if (mailProtocol == null)
            {
                return false;
            }

            if (ReferenceEquals(this, mailProtocol))
            {
                return true;
            }

            return Server == mailProtocol.Server &&
                   Port == mailProtocol.Port &&
                   UseSsl == mailProtocol.UseSsl;
        }

        public override int GetHashCode()
        {
            return Server.GetHashCode();
        }

        public override string ToString()
        {
            return $"{_mailProtocol} - {Server}:{Port}, {UseSsl}";
        }

        public static string GetServerFromMailAddress(
            MailProtocols mailProtocol,
            string address)
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            string result = mailProtocol.ToString().ToLower() + '.';
            int index = address.IndexOf('@');
            if (index > 0)
            {
                result += address.Substring(index + 1);
            }

            return result;
        }

        public static string GetMessageAboutValidPorts(
            MailProtocols mailProtocol)
        {
            string purpose = string.Empty;
            int[] ports = new int[0];
            int[] portWithSsl = new int[0];

            switch (mailProtocol)
            {
                case MailProtocols.SMTP:
                {
                    purpose = "для безопасной передачи";
                    portWithSsl = SmtpConfig.Where(c => c.Value)
                        .Select(c => c.Key).ToArray();
                    ports = SmtpConfig.Where(c => !c.Value).Select(c => c.Key)
                        .ToArray();
                    break;
                }
                case MailProtocols.IMAP:
                {
                    purpose = "для безопасного приёма";
                    portWithSsl = ImapConfig.Where(c => c.Value)
                        .Select(c => c.Key).ToArray();
                    ports = ImapConfig.Where(c => !c.Value).Select(c => c.Key)
                        .ToArray();
                    break;
                }
            }

            string result = string.Empty;
            if (ports.Length > 0 && portWithSsl.Length > 0)
            {
                result = $"{mailProtocol} использует ";
                result += ports.Length == 1 ? "порт " : "порты ";
                result += string.Join(" и ", ports) + ", а также ";
                result += portWithSsl[0] + " " + purpose +
                          " почты (рекомендуется).";
            }

            return result;
        }
    }
}