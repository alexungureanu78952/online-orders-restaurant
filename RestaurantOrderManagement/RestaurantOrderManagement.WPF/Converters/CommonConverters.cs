using System;
using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace RestaurantOrderManagement.Converters
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ConverterHelpers.IsTruthy(value) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is Visibility visibility && visibility == Visibility.Visible;
        }
    }

    public class InverseBoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ConverterHelpers.IsTruthy(value) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is Visibility visibility && visibility != Visibility.Visible;
        }
    }

    public class ObjectToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ConverterHelpers.IsTruthy(value) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }

    public class ZeroToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ConverterHelpers.IsZero(value) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }

    public class InverseBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var inverted = !ConverterHelpers.IsTruthy(value);

            if (targetType == typeof(Visibility))
                return inverted ? Visibility.Visible : Visibility.Collapsed;

            return inverted;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
                return visibility != Visibility.Visible;

            return value is bool boolValue ? !boolValue : Binding.DoNothing;
        }
    }

    public class BoolToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var options = ConverterHelpers.SplitParameter(parameter);
            var trueText = options.Length > 0 ? options[0] : "Yes";
            var falseText = options.Length > 1 ? options[1] : "No";

            return ConverterHelpers.IsTruthy(value) ? trueText : falseText;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }

    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var options = ConverterHelpers.SplitParameter(parameter);
            var trueColor = options.Length > 0 ? options[0] : "#D4EDDA";
            var falseColor = options.Length > 1 ? options[1] : "#F8D7DA";

            return ConverterHelpers.ToBrush(ConverterHelpers.IsTruthy(value) ? trueColor : falseColor);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }

    public class StringEqualsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var options = ConverterHelpers.SplitParameter(parameter);
            var expected = options.Length > 0 ? options[0] : string.Empty;
            var isEqual = string.Equals(value?.ToString(), expected, StringComparison.OrdinalIgnoreCase);

            if (targetType == typeof(Visibility))
                return isEqual ? Visibility.Visible : Visibility.Collapsed;

            if (targetType == typeof(bool) || targetType == typeof(bool?))
                return isEqual;

            if (targetType == typeof(Brush) || targetType == typeof(object))
                return ConverterHelpers.ToBrush(isEqual ? "#3498DB" : "#95A5A6");

            return isEqual;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }

    internal static class ConverterHelpers
    {
        public static bool IsTruthy(object value)
        {
            return value switch
            {
                null => false,
                bool boolValue => boolValue,
                string text => !string.IsNullOrWhiteSpace(text),
                int number => number != 0,
                long number => number != 0,
                decimal number => number != 0,
                double number => Math.Abs(number) > double.Epsilon,
                ICollection collection => collection.Count > 0,
                _ => true
            };
        }

        public static bool IsZero(object value)
        {
            return value switch
            {
                null => true,
                int number => number == 0,
                long number => number == 0,
                decimal number => number == 0,
                double number => Math.Abs(number) <= double.Epsilon,
                ICollection collection => collection.Count == 0,
                _ => false
            };
        }

        public static string[] SplitParameter(object parameter)
        {
            return (parameter?.ToString() ?? string.Empty)
                .Trim('\'')
                .Split('|', StringSplitOptions.TrimEntries);
        }

        public static Brush ToBrush(string color)
        {
            try
            {
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
            }
            catch
            {
                return Brushes.Transparent;
            }
        }
    }
}
