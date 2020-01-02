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
                Caption.Text = "Импорт ключей:";
                PrivateKeyRadioButton.IsChecked = true;
                CommandButton.Content = "Импортировать".ToUpper();
                KeyTypesRadioButtons.Visibility = Visibility.Collapsed;
            }
            else
            {
                Caption.Text = "Экспорт ключей:";
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
                CustomMessageBox.Show("Пароль должен содержать минимум 4 символа.",
                    Title);
            }
            else
            {
                DialogResult = true;
            }
        }
    }
}
