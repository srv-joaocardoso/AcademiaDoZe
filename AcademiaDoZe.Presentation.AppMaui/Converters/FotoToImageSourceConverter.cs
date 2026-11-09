// João Victor Cardoso Gonçalves - 155953 - DEV02

using AcademiaDoZe.Application.DTOs;
using System.Globalization;

namespace AcademiaDoZe.Presentation.AppMaui.Converters;

// A foto vem do banco como array de bytes; aqui vira ImageSource para exibir na tela
public class FotoToImageSourceConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ArquivoDto foto && foto.Conteudo != null && foto.Conteudo.Length > 0)
        {
            var bytes = foto.Conteudo;
            return ImageSource.FromStream(() => new MemoryStream(bytes));
        }

        // sem foto, mostra um avatar padrão
        return ImageSource.FromFile("sem_foto.png");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
