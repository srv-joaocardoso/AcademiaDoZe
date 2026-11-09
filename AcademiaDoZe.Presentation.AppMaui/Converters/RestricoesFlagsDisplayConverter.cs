// João Victor Cardoso Gonçalves - 155953 - DEV02

using AcademiaDoZe.Application.Enums;
using System.Globalization;

namespace AcademiaDoZe.Presentation.AppMaui.Converters;

// Converte o enum [Flags] AppMatriculaRestricoes em texto amigável (lista separada por vírgula),
// usando o [Display(Name = "...")] de cada valor marcado
public sealed class RestricoesFlagsDisplayConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not AppMatriculaRestricoes flags)
            return string.Empty;

        if (flags == AppMatriculaRestricoes.None)
            return AppMatriculaRestricoes.None.GetDisplayName();

        var partes = new List<string>();
        foreach (var v in Enum.GetValues<AppMatriculaRestricoes>())
        {
            if (v == AppMatriculaRestricoes.None) continue;
            if (flags.HasFlag(v)) partes.Add(v.GetDisplayName());
        }

        return partes.Count > 0 ? string.Join(", ", partes) : AppMatriculaRestricoes.None.GetDisplayName();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
