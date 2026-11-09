// João Victor Cardoso Gonçalves - 155953 - DEV02

using System.Globalization;

namespace AcademiaDoZe.Presentation.AppMaui.Converters;

// O DTO usa DateOnly e o DatePicker trabalha com DateTime
public class DateOnlyToDateTimeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DateOnly dateOnly)
            return dateOnly.ToDateTime(TimeOnly.MinValue);

        return DateTime.Today; // fallback
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DateTime dateTime)
            return DateOnly.FromDateTime(dateTime);

        return DateOnly.FromDateTime(DateTime.Today); // fallback
    }
}
