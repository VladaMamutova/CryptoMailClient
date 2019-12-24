using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CryptoMailClient.Utilities
{
    public class BoolToFontWeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null && (bool)value ? FontWeights.Normal : FontWeights.Medium;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null && (FontWeight)value == FontWeights.Normal;
        }
    }
}