using System;
using System.IO;
using System.Net.Mail;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using MailKit;
using MailKit.Net.Imap;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace CryptoMailClient.Models
{
    public class EmailAccount
    {
        private readonly string _password;

        public string Address { get; }
        public string RsaPrivateKey { get; private set; }
        public string RsaPublicKey { get; private set; }
        public MailProtocol Smtp { get; }
        public MailProtocol Imap { get; }

        public static EmailAccount FromBytes(byte[] bytes)
        {
            EmailAccount emailAccount;
            try
            {
                using (BinaryReader reader =
                    new BinaryReader(new MemoryStream(bytes)))
                {
                    int addressLength = reader.ReadInt32();
                    string address = Encoding.Unicode.GetString(
                        reader.ReadBytes(addressLength));

                    int passwordLength = reader.ReadInt32();
                    string password = Encoding.Unicode.GetString(
                        reader.ReadBytes(passwordLength));

                    int rsaKeyInfoLength = reader.ReadInt32();
                    string rsaKeyInfo = Encoding.Unicode.GetString(
                        reader.ReadBytes(rsaKeyInfoLength));

                    int smtpPort = reader.ReadInt32();
                    int imapPort = reader.ReadInt32();

                    emailAccount = new EmailAccount(address, password, smtpPort, imapPort);
                    emailAccount.SetRsaFullKeyPair(rsaKeyInfo);
                }
            }
            catch
            {
                emailAccount = null;
            }

            return emailAccount;
        }

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

            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            RsaPrivateKey = rsa.ToXmlString(true);
            RsaPublicKey = rsa.ToXmlString(false);
        }

        public void SetSmtpPort(int port)
        {
            Smtp.SetPort(port);
        }

        public void SetImapPort(int port)
        {
            Imap.SetPort(port);
        }

        public void SetRsaFullKeyPair(string xmlKeyInfo)
        {
            var rsaKeyPair = new RSACryptoServiceProvider();
            rsaKeyPair.FromXmlString(xmlKeyInfo);
            RsaPublicKey = rsaKeyPair.ToXmlString(false);
            RsaPrivateKey = xmlKeyInfo;
        }

        public async Task Connect()
        {
            try
            {
                using (var client = new SmtpClient())
                {
                    await client.ConnectAsync(Smtp.Server, Smtp.Port, Smtp.UseSsl);
                    await client.AuthenticateAsync(Address, _password);
                    await client.DisconnectAsync(true);
                }

                using (var client = new ImapClient())
                {
                    await client.ConnectAsync(Imap.Server, Imap.Port, Imap.UseSsl);
                    await client.AuthenticateAsync(Address, _password);
                    await client.DisconnectAsync(true);
                }
            }
            catch (SocketException ex)
            {
                string message = "Невозможно установить соединение " +
                                 "с почтовым сервером. ";
                if (ex.ErrorCode == 11001)
                {
                    message += "Проверьте подключение к интернету.";
                }
                else
                {
                    message += ex.Message;
                }

                throw new Exception(message);
            }
            catch
            {
                throw new Exception("Логин или пароль неверны.");
            }
        }

        public async Task<ImapClient> GetImapClient()
        {
            try
            {
                ImapClient client = new ImapClient();
                await client.ConnectAsync(Imap.Server, Imap.Port, Imap.UseSsl);
                await client.AuthenticateAsync(Address, _password);
                return client;
            }
            catch (SocketException ex)
            {
                string message = "Соединение с почтовым сервером не установлено. ";
                if (ex.ErrorCode == 11001)
                {
                    message += "Проверьте подключение к интернету.";
                }
                else
                {
                    message += ex.Message;
                }

                throw new Exception(message);
            }
            catch
            {
                throw new Exception("Логин или пароль неверны.");
            }
        }

        public byte[] GetBytes()
        {
            MemoryStream stream = new MemoryStream();
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                byte[] address = Encoding.Unicode.GetBytes(Address);
                writer.Write(address.Length);
                writer.Write(address);

                byte[] password = Encoding.Unicode.GetBytes(_password);
                writer.Write(password.Length);
                writer.Write(password);

                byte[] rsaPrivateKey = Encoding.Unicode.GetBytes(RsaPrivateKey);
                writer.Write(rsaPrivateKey.Length);
                writer.Write(rsaPrivateKey);

                writer.Write(Smtp.Port);
                writer.Write(Imap.Port);
            }

            return stream.ToArray();
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