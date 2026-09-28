using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Effects;
using TheRoad.ViewModels;

namespace TheRoad.Converters
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return b ? Visibility.Visible : (parameter?.ToString() == "Hidden" ? Visibility.Hidden : Visibility.Collapsed);
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => value is Visibility v && v == Visibility.Visible;
    }

    public class BoolToSegmentBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool filled)
            {
                if (filled)
                {
                    var brush = new LinearGradientBrush
                    {
                        StartPoint = new Point(0, 0),
                        EndPoint = new Point(0, 1)
                    };
                    brush.GradientStops.Add(new GradientStop(Color.FromRgb(0xE8, 0xC5, 0x65), 0));
                    brush.GradientStops.Add(new GradientStop(Color.FromRgb(0xD8, 0xA5, 0x45), 0.5));
                    brush.GradientStops.Add(new GradientStop(Color.FromRgb(0xB8, 0x85, 0x25), 1));
                    brush.Freeze();
                    return brush;
                }
                return new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x10));
            }
            return new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x10));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    public class BoolToGlowEffectConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool filled && filled)
            {
                var effect = new DropShadowEffect
                {
                    BlurRadius = 8,
                    ShadowDepth = 0,
                    Opacity = 0.6,
                    Color = Color.FromRgb(0xD8, 0xA5, 0x45)
                };
                effect.Freeze();
                return effect;
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    public class InverseBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool b ? !b : true;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool b ? !b : true;
    }

    public class ResourceLevelToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double ratio)
            {
                if (ratio > 0.6) return new SolidColorBrush(Color.FromRgb(0x6B, 0x8E, 0x5C));
                if (ratio > 0.3) return new SolidColorBrush(Color.FromRgb(0xD8, 0xA5, 0x45));
                return new SolidColorBrush(Color.FromRgb(0xE0, 0x7A, 0x5F));
            }
            return new SolidColorBrush(Color.FromRgb(0x6B, 0x8E, 0x5C));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    public class ItemCountToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int count && count > 1)
                return $"x{count}";
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str && parameter is string param)
                return str == param ? Visibility.Visible : Visibility.Collapsed;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    public class StringToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str && parameter is string param)
                return str == param;
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b && b && parameter is string param)
                return param;
            return Binding.DoNothing;
        }
    }

    public class StringToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string hex)
            {
                try
                {
                    var color = (Color)ColorConverter.ConvertFromString(hex);
                    return new SolidColorBrush(color);
                }
                catch { }
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    public class BoolToOpacityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool b ? (b ? 1.0 : 0.5) : 1.0;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    public class NotificationTypeToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value switch
            {
                NotificationType.Success => "✓",
                NotificationType.Warning => "⚠",
                NotificationType.Danger => "☠",
                NotificationType.Scavenge => "🔍",
                _ => "ℹ"
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    public class NotificationTypeToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value switch
            {
                NotificationType.Success => new SolidColorBrush(Color.FromRgb(0x6B, 0x8E, 0x5C)),
                NotificationType.Warning => new SolidColorBrush(Color.FromRgb(0xD8, 0xA5, 0x45)),
                NotificationType.Danger => new SolidColorBrush(Color.FromRgb(0xE0, 0x7A, 0x5F)),
                NotificationType.Scavenge => new SolidColorBrush(Color.FromRgb(0x5A, 0x6A, 0x7A)),
                _ => new SolidColorBrush(Color.FromRgb(0xD8, 0xA5, 0x45))
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    public class HalfConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d) return d / 2;
            return 0.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value != null ? Visibility.Visible : Visibility.Collapsed;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    public class SliderWidthConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 4 &&
                values[0] is double value &&
                values[1] is double minimum &&
                values[2] is double maximum &&
                values[3] is double actualWidth)
            {
                double ratio = (value - minimum) / (maximum - minimum);
                return Math.Max(0, ratio * (actualWidth - 18));
            }
            return 0.0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}