using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MimeKit;

namespace CryptoMailClient.ViewModels
{
    public class MessageItem : INotifyPropertyChanged
    {
        private bool _isSelected;
        private bool _wasSeen;

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

        public bool WasSeen
        {
            get => _wasSeen;
            set
            {
                if (_wasSeen == value) return;
                _wasSeen = value;
                OnPropertyChanged(nameof(WasSeen));
            }
        }

        public string From { get; }
        public string Subject { get; }
        public string Date { get; }
        public char Code { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(
            [CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this,
                new PropertyChangedEventArgs(propertyName));
        }

        public MessageItem(InternetAddress from, string subject, DateTimeOffset date)
        {
            if (from != null)
            {
                From = from.Name;
                Code = From.Length > 0 ? From[0] : ' ';
            }

            Subject = subject;
            Date = date.UtcDateTime.ToString("d MMM");
        }

        public override string ToString()
        {
            return $"From: {From}, Subject: {Subject}, Date: {Date}";
        }
    }
}
