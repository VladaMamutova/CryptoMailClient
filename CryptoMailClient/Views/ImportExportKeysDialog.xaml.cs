using System.Windows;
using System.Windows.Input;

namespace CryptoMailClient.Views
{
    /// <summary>
    /// Логика взаимодействия для ImportKeysDialog.xaml
    /// </summary>
    public partial class ImportExportKeysDialog : Window
    {
        public ImportExportKeysDialog(bool isImport)
        {
            InitializeComponent();
            if (isImport)
            {
                Title = "Импорт";
                Caption.Text = "Введите пароль от файла с ключами:";
                PrivateKeyRadioButton.IsChecked = true;
                CommandButton.Content = "Импортировать".ToUpper();
                KeyTypesRadioButtons.Visibility = Visibility.Collapsed;
            }
            else
            {
                Title = "Экспорт";
                Caption.Text = "Выберите ключ для экспорта:";
                PublicKeyRadioButton.IsChecked = true;
                CommandButton.Content = "Экспортировать".ToUpper();
            }
        }

        private void Title_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            if (PrivateKeyRadioButton.IsChecked.HasValue &&
                PrivateKeyRadioButton.IsChecked.Value
                && Password.Password.Length < 4)
            {
                MessageBox.Show("Пароль должен содержать минимум 4 символа.",
                    Title);
            }
            else
            {
                DialogResult = true;
            }
        }
    }
}
