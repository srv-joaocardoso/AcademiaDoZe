// João Victor Cardoso Gonçalves - 155953 - DEV02

using System.Globalization;

namespace AcademiaDoZe.Presentation.AppMaui.Converters;

// inverte um bool, ex.: esconder um campo quando IsEditMode for true
public sealed class InverseBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && !b;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && !b;
}
