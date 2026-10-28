using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Ludo.Presentation.Wpf.Views;

/// <summary>Visible for a count above zero; with the parameter "invert", visible only for zero.</summary>
public sealed class CountToVisibilityConverter : IValueConverter
{
    public static readonly CountToVisibilityConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        (value is int count && count > 0) != (parameter as string == "invert") ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
