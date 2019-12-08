using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CryptoMailClient.Models;

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

        private void ButtonClose_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
