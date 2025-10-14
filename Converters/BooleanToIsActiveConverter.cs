using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace FehlzeitApp.Converters
{
    public class BooleanToIsActiveConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                // Return color brush for status badge
                if (targetType == typeof(Brush) || targetType == typeof(SolidColorBrush))
                {
                    return boolValue ? 
                        new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981")) : // Green for active
                        new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));   // Red for inactive
                }
                
                // Return text for status
                return boolValue ? "Aktiv" : "Inaktiv";
            }
            
            // Default return
            if (targetType == typeof(Brush) || targetType == typeof(SolidColorBrush))
            {
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9CA3AF")); // Gray for unknown
            }
            
            return "Unbekannt";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
