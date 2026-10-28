using System.Globalization;
using System.Windows.Data;
using Ludo.Presentation.Wpf.ViewModels;

namespace Ludo.Presentation.Wpf.Views;

/// <summary>A player name to the one or two letters shown in its avatar.</summary>
public sealed class InitialsConverter : IValueConverter
{
    public static readonly InitialsConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        ChatMessageItem.InitialsOf(value as string ?? string.Empty);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
