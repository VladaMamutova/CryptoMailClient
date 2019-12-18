using System.Windows;
using CryptoMailClient.ViewModels;

namespace CryptoMailClient.Views
{
    /// <summary>
    /// Логика взаимодействия для MailWindow.xaml
    /// </summary>
    public partial class ReadEmailWindow : Window
    {
        public ReadEmailWindow()
        {
            InitializeComponent();
        }

        private void Close_OnClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void Window_OnMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void ReadEmailWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            if(DataContext is MessageItem message)
            {
                WebBrowser.NavigateToString(message.HtmlBody);
            }
        }
    }
}
