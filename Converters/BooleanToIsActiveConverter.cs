using System;
using System.Globalization;
using System.Windows.Data;

namespace FehlzeitApp.Converters
{
    public class BooleanToIsActiveConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? "Aktiv" : "Inaktiv";
            }
            return "Unbekannt";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
