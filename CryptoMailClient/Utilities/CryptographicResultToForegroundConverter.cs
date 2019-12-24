using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using CryptoMailClient.ViewModels;

namespace CryptoMailClient.Utilities
{
    public class CryptographicResultToForegroundConverter :IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
            CultureInfo culture)
        {
            string stringColor = "#FFC4C4C4"; // серый цвет

            if (value is CryptographicResult result)
            {
                switch (result)
                {
                    case CryptographicResult.Success:
                    {
                        stringColor = "#FF5ADC78"; // зелёный
                            break;
                    }
                    case CryptographicResult.Error:
                    {
                        stringColor = "#FFE04040"; // красный
                            break;
                    }
                    case CryptographicResult.KeyNotFound:
                    {
                        stringColor = "#FFCD10"; // жёлтый
                        break;
                    }
                    default:
                    {
                        stringColor = "#FFC4C4C4"; // серый цвет
                        break;
                        }
                }
            }

            if (ColorConverter.ConvertFromString(stringColor) is Color color)
            {
                return new SolidColorBrush(color);
            }

            return new SolidColorBrush(Colors.Black);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}