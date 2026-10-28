using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Ludo.Presentation.Wpf.Views;

/// <summary>True when all bound values are equal, e.g. to mark the selected swatch.</summary>
public sealed class EqualityConverter : IMultiValueConverter
{
    public static readonly EqualityConverter Instance = new();

    public object Convert(object?[] values, Type targetType, object? parameter, CultureInfo culture) =>
        values.Length > 0 && values.All(v => Equals(v, values[0]));

    public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Visible when the value is set, collapsed when it is null.</summary>
public sealed class NullToVisibilityConverter : IValueConverter
{
    public static readonly NullToVisibilityConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is null ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
