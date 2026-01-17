using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Gomoku.Converter
{
    public class EqualToVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2 || values[0] == null || values[1] == null)
                return Visibility.Hidden;

            bool isequal = values[0].ToString() == values[1].ToString();
            return isequal ? Visibility.Visible : Visibility.Hidden;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
