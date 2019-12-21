using System;
using System.Collections.Generic;
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
                await Mailbox.SendMessage(Address, Subject, Body, Attachments.ToArray());
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