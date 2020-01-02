using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using CryptoMailClient.Models;
using CryptoMailClient.Utilities;
using CryptoMailClient.Views;
using Microsoft.Win32;
using MimeKit;

namespace CryptoMailClient.ViewModels
{
    public class MessageItem : INotifyPropertyChanged
    {
        private bool _isSelected;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value) return;
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }

        private bool _seen;

        public bool Seen
        {
            get => _seen;
            set
            {
                if (_seen == value) return;
                _seen = value;
                OnPropertyChanged(nameof(Seen));
            }
        }

        public MimeMessage Message { get; }
        public string FullName { get; }

        // Эти поля отображаются как в списке писем,
        // так и в окне просмотра конкретного письма.
        public string NameFrom { get; }
        public char CodeFrom { get; }
        public string Subject { get; }
        public string DateText { get; }

        // Эти поля отображаются только
        // в окне просмотра конкретного письма.
        public string AddressFrom { get; set; }
        public string AddressTo { get; set; }
        public string HtmlBody { get; set; }
        public string DateTimeText { get; }

        private CryptographicResult _decryptionResult;

        public CryptographicResult DecryptionResult
        {
            get => _decryptionResult;
            private set
            {
                _decryptionResult = value;
                OnPropertyChanged(nameof(DecryptionResult));
                OnPropertyChanged(nameof(DecryptionToolTip));
            }
        }

        public string DecryptionToolTip => DecryptionResult == CryptographicResult.None
            ? "Письмо не зашифровано"
            : DecryptionResult == CryptographicResult.Success
                ? "Письмо зашифровано, содержимое успешно расшифровано"
                : "Письмо зашифровано, но содержимое не может быть расшифровано вашим ключом";

        private CryptographicResult _verificationResult;

        public CryptographicResult VerificationResult
        {
            get => _verificationResult;
            private set
            {
                _verificationResult = value;
                OnPropertyChanged(nameof(VerificationResult));
                OnPropertyChanged(nameof(VerificationToolTip));
            }
        }

        public string VerificationToolTip =>
            VerificationResult == CryptographicResult.None
                ? "Письмо не подписано"
                : VerificationResult == CryptographicResult.Success
                    ? "Письмо подписано, отправитель проверен и подтверждён"
                    : VerificationResult == CryptographicResult.KeyNotFound
                        ? "Письмо подписано, но отправитель не может быть " +
                          "проверен, ключи не найдены"
                        : "Письмо подписано, но подпись не соответствует отправителю, ключи неверны";

        public ObservableCollection<AttachmentItem> Attachments { get; }

        public RelayCommand DownloadAttachmentCommand { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(
            [CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this,
                new PropertyChangedEventArgs(propertyName));
        }

        public MessageItem(string fileName, MimeMessage message)
        {
            Message = message;
            FullName = fileName ?? "";
            Seen = FullName.Contains("Seen");
            DecryptionResult = CryptographicResult.None;
            VerificationResult = CryptographicResult.None;

            Attachments = new ObservableCollection<AttachmentItem>();
            foreach (var attachment in Message.Attachments)
            {
                Attachments.Add(new AttachmentItem(
                    attachment.ContentDisposition?.FileName
                    ?? attachment.ContentType.Name, attachment));
            }

            MailboxAddress from = Message.From.Mailboxes.First();
            NameFrom = from.Name?.Length > 0 ? from.Name : from.ToString();
            AddressFrom = from.Address;
            CodeFrom = NameFrom.Length > 0 ? NameFrom.ToUpper()[0] : ' ';

            Subject = string.IsNullOrEmpty(message.Subject)
                ? "(без темы)"
                : message.Subject;

            AddressTo = message.To.Mailboxes.Any()
                ? message.To.Mailboxes.First()?.Address
                : "0 получателей";
            DateTime mailDate = message.Date.UtcDateTime;
            DateText = mailDate.ToString(mailDate.Year == DateTime.Today.Year
                ? "d MMM"
                : "dd.MM.yyyy");

            DateTimeText = DateText + " в " + mailDate.ToString("HH:mm");

            DownloadAttachmentCommand = new RelayCommand(DownloadAttachment);
        }

        public void SetCryptographicResults(
            CryptographicResult decryptionResult,
            CryptographicResult verificationResult)
        {
            DecryptionResult = decryptionResult;
            VerificationResult = verificationResult;
        }

        private void DownloadAttachment(object o)
        {
            if (!(o is AttachmentItem attachment)) return;

            SaveFileDialog saveFileDialog =
                new SaveFileDialog
                {
                    FileName = attachment.FileName,
                    Filter = !string.IsNullOrEmpty(attachment.Extension)
                        ? $"*{attachment.Extension}|*{attachment.Extension}"
                        : "All files (*.*)|*.*"
                };

            if (saveFileDialog.ShowDialog() == true)
            {
                using (var stream = File.Create(saveFileDialog.FileName))
                {
                    MemoryStream contentStream = new MemoryStream();
                    if (attachment.Content is MessagePart messagePart)
                    {
                        messagePart.Message.WriteTo(contentStream);
                    }
                    else
                    {
                        ((MimePart) attachment.Content).Content.DecodeTo(
                            contentStream);
                    }

                    if (DecryptionResult == CryptographicResult.Success)
                    {
                        try
                        {
                            byte[] encryptedAttachment =
                                Cryptography.DecryptAES(
                                    contentStream.ToArray(),
                                    UserManager.CurrentUser.CurrentEmailAccount
                                        .RsaPrivateKey);
                            stream.Write(encryptedAttachment, 0,
                                encryptedAttachment.Length);
                        }
                        catch (Exception ex)
                        {
                            CustomMessageBox.Show("Не удалось скачать вложение. " + ex.Message,
                                "Загрузка вложений");
                        }
                    }
                    else
                    {
                        stream.Write(contentStream.ToArray(), 0,
                            contentStream.ToArray().Length);
                    }
                }
            }
        }

        public override string ToString()
        {
            return $"Name: {NameFrom}, Address: {AddressFrom}, " +
                   $"Subject: {Subject}, Date: {DateTimeText}";
        }
    }
}
