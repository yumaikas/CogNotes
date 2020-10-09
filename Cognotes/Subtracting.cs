using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Cognotes
{
    [ValueConversion(typeof(double), typeof(double))]
    public class Subtracting : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (double)value - (double)parameter;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => (int)value + (int)parameter;
    }

    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class CollapseIf: IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return (bool)value ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (Visibility)value == Visibility.Visible;
        }

    }
}
