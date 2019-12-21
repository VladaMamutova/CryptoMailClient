using System.Windows;
using System.Windows.Input;
using CryptoMailClient.ViewModels;

namespace CryptoMailClient.Views
{
    /// <summary>
    /// Логика взаимодействия для WriteEmailWindow.xaml
    /// </summary>
    public partial class WriteEmailWindow : Window
    {
        public WriteEmailWindow()
        {
            InitializeComponent();

            var viewModel = new WriteEmailWindowViewModel();
            viewModel.MessageBoxDisplayRequested += (s, o) =>
            {
                MessageBox.Show(o.MessageBoxText, o.Caption);
            };
            viewModel.CloseRequested += Close;

            DataContext = viewModel;

        }

        private void Window_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void Close_OnClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
