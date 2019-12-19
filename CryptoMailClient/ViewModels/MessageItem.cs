using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(
            [CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this,
                new PropertyChangedEventArgs(propertyName));
        }

        public MessageItem(MimeMessage message)
        {
            Message = message;

            MailboxAddress from = Message.From.Mailboxes.First();
            NameFrom = from.Name?.Length > 0 ? from.Name : from.ToString();
            AddressFrom = from.Address;
            CodeFrom = NameFrom.Length > 0 ? NameFrom.ToUpper()[0] : ' ';

            Subject = string.IsNullOrEmpty(message.Subject)
                ? "(без темы)"
                : message.Subject;

            AddressTo = message.To.Mailboxes.Any() ? message.To.Mailboxes.First()?.Address : "0 получателей";
            DateTime mailDate = message.Date.UtcDateTime;
            DateText = mailDate.ToString(mailDate.Year == DateTime.Today.Year
                ? "d MMM"
                : "dd.MM.yyyy");

            DateTimeText = DateText + " в " + mailDate.ToString("HH:mm");
        }

        public override string ToString()
        {
            return $"Name: {NameFrom}, Address: {AddressFrom}, " +
                   $"Subject: {Subject}, Date: {DateTimeText}";
        }
    }
}
