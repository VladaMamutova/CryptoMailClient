using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CryptoMailClient.Annotations;

namespace CryptoMailClient.Utilities
{
    public class ViewModelBase : INotifyPropertyChanged
    {
        // Классы, представляюшие аргументы для событий.
        public class MessageBoxEventArgs : EventArgs
        {
            public string Caption { get; set; }
            public string MessageBoxText { get; set; }
        }

        // Делегаты для обработки событий.
        public event PropertyChangedEventHandler PropertyChanged;

        public event EventHandler<MessageBoxEventArgs>
            MessageBoxDisplayRequested;

        public event Action CloseRequested;
        public event Action<bool> CloseDialogRequested;
        public event Action<object> ShowDialogRequested;

        // Методы, выполняющиеся в событиях.
        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(
            [CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this,
                new PropertyChangedEventArgs(propertyName));
        }

        protected void OnMessageBoxDisplayRequest(string caption,
            string messageBoxText)
        {
            MessageBoxDisplayRequested?.Invoke(
                this,
                new MessageBoxEventArgs
                {
                    Caption = caption,
                    MessageBoxText = messageBoxText
                });
        }

        protected void OnCloseRequested()
        {
            CloseRequested?.Invoke();
        }

        protected void OnCloseDialogRequested(bool result)
        {
            CloseDialogRequested?.Invoke(result);
        }

        protected void OnShowDialogRequested(object viewModel)
        {
            ShowDialogRequested?.Invoke(viewModel);
        }
    }
}
