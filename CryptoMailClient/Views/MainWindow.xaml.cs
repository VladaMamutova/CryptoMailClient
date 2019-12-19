using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CryptoMailClient.ViewModels;

namespace CryptoMailClient.Views
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            // Подключаем модель представления в событии первой загрузки окна,
            // а не в конструкторе, для того, чтобы подключились привязки
            // для вошедшего через LoginWindow пользователя (так как MainWindow
            // создается раньше, чем LoginWindow).
            var viewModel = new MainWindowViewModel();
            viewModel.MessageBoxDisplayRequested += (s, o) =>
            {
                MessageBox.Show(o.MessageBoxText, o.Caption);
            };
            viewModel.CloseRequested += Close;
            viewModel.ShowDialogRequested += dialogViewModel =>
            {
                if (dialogViewModel is MessageItem item)
                {
                    var readEmailWindow = new ReadEmailWindow
                    {
                        DataContext = item
                    };
                    readEmailWindow.ShowDialog();
                }
            };
            
            DataContext = viewModel;

            viewModel.UpdateFolders();
            viewModel.SelectFolder(viewModel.SelectedFolder?.Name);
        }

        private void Window_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void ButtonExpand_OnClick(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
                ((Button)sender).ToolTip = "Развернуть";
            }
            else
            {
                WindowState = WindowState.Maximized;
                ((Button)sender).ToolTip = "Свернуть";
            }
        }
    }
}
