using System;
using System.Collections.ObjectModel;
using System.Linq;
using CryptoMailClient.Models;
using CryptoMailClient.Utilities;

namespace CryptoMailClient.ViewModels
{
    public class WriteEmailWindowViewModel: ViewModelBase
    {
        public string Title => "Новое сообщение";

        private string _address;
        public string Address
        {
            get => _address;
            set
            {
                _address = value;
                OnPropertyChanged(nameof(Address));
            }
        }

        private string _subject;
        public string Subject
        {
            get => _subject;
            set
            {
                _subject = value;
                OnPropertyChanged(nameof(Subject));
            }
        }

        private string _body;

        public string Body
        {
            get => _body;
            set
            {
                _body = value;
                OnPropertyChanged(nameof(Body));
            }
        }

        private ObservableCollection<string> _attachments;

        public ObservableCollection<string> Attachments
        {
            get => _attachments;
            set
            {
                _attachments = value;
                OnPropertyChanged(nameof(Attachments));
            }
        }

        private bool _encryptionChecked;

        public bool EncryptionChecked
        {
            get => _encryptionChecked;
            set
            {
                _encryptionChecked = value;
                OnPropertyChanged(nameof(EncryptionChecked));
                OnPropertyChanged(nameof(EncryptionToolTip));
            }
        }

        public string EncryptionToolTip =>
            _encryptionChecked ?
                "Письмо будет зашифровано":
                "Защитить электронное письмо с помощью шифрования";

        private bool _signatureChecked;

        public bool SignatureChecked
        {
            get => _signatureChecked;
            set
            {
                _signatureChecked = value;
                OnPropertyChanged(nameof(SignatureChecked));
                OnPropertyChanged(nameof(SignatureToolTip));
            }
        }

        public string SignatureToolTip =>
            _signatureChecked ?
                "Письмо будет подписано" :
                "Подтвердить авторство электронного письма с помощью электронно-цифровой подписи";

        public RelayCommand SendCommand { get; }
        public RelayCommand CloseCommand { get; }

        public WriteEmailWindowViewModel()
        {
            _address = string.Empty;
            _subject = string.Empty;
            _body = string.Empty;
            _attachments = new ObservableCollection<string>();

            SendCommand = new RelayCommand(Send);
            CloseCommand = new RelayCommand(o => { OnCloseRequested(); });
        }

        private async void Send(object o)
        {
            try
            {
                await Mailbox.SendMessage(Address, Subject, Body,
                    Attachments.ToArray(), _encryptionChecked,
                    _signatureChecked);
                OnMessageBoxDisplayRequest(Title, "Письмо отправлено.");
                OnCloseRequested();
            }
            catch(Exception ex)
            {
                OnMessageBoxDisplayRequest(Title, ex.Message);
            }
        }
    }
}