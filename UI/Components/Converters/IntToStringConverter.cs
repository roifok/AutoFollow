using System;
using System.Globalization;
using System.Windows.Data;

namespace AutoFollow.UI.Components.Converters
{
    public class IntToStringConverter : IValueConverter
    {
        #region IValueConverter Members

        /// <summary>Converts a value.</summary>
        /// <param name="value">The value produced by the binding source.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>
        /// A converted value. If the method returns null, the valid null value is used.
        /// </returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {            
            return string.Format("{0:#.0}", value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return int.Parse((string) value, CultureInfo.InvariantCulture);
        }

        #endregion
    }
}
