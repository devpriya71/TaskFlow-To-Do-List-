using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace TaskFlow.Helpers
{
    public class DateToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime dt)
            {
                if (dt < DateTime.Now) return Brushes.LightSalmon; // Overdue
                if (dt <= DateTime.Now.AddDays(2)) return Brushes.LightYellow; // Due soon
            }
            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
