using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace CryptoMailClient.Utilities
{
    public class RelayAsyncCommand : RelayCommand
    {
        public event EventHandler Started;

        public event EventHandler Ended;

        public bool IsExecuting { get; private set; }

        public RelayAsyncCommand(Action<object> execute, Func<object, bool> canExecute)
            : base(execute, canExecute)
        {
        }

        public RelayAsyncCommand(Action<object> execute)
            : base(execute)
        {
        }

        //public override bool CanExecute(object parameter)
        //{
        //    return base.CanExecute(parameter) && !IsExecuting;
        //}

        public override void Execute(object parameter)
        {
            try
            {
                IsExecuting = true;
                Started?.Invoke(this, EventArgs.Empty);

                Task task = Task.Factory.StartNew(() =>
                {
                    _execute(parameter);
                });
                task.ContinueWith(t =>
                {
                    OnRunWorkerCompleted(EventArgs.Empty);
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }
            catch (Exception ex)
            {
                OnRunWorkerCompleted(new RunWorkerCompletedEventArgs(null, ex, true));
            }
        }

        private void OnRunWorkerCompleted(EventArgs e)
        {
            IsExecuting = false;
            Ended?.Invoke(this, e);
        }
    }
}