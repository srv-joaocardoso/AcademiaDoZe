// João Victor Cardoso Gonçalves - 155953 - DEV02

using AcademiaDoZe.Application.DTOs;

namespace AcademiaDoZe.Presentation.AppMaui.Helpers;

// usado pelo cadastro de Aluno e de Colaborador
public static class CadastroHelper
{
    // pergunta a origem (câmera ou galeria) e devolve a foto pronta para o DTO, ou null se cancelar
    public static async Task<ArquivoDto?> SelecionarFotoAsync()
    {
        string escolha = await Shell.Current.DisplayActionSheetAsync("Origem da Imagem", "Cancelar", null, "Câmera", "Galeria");

        FileResult? result = null;

        if (escolha == "Galeria")
        {
            result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Selecione uma imagem",
                FileTypes = FilePickerFileType.Images
            });
        }
        else if (escolha == "Câmera")
        {
            if (!MediaPicker.Default.IsCaptureSupported)
            {
                await Shell.Current.DisplayAlertAsync("Câmera", "Captura de foto não suportada neste dispositivo.", "OK");
                return null;
            }

            result = await MediaPicker.Default.CapturePhotoAsync();
        }

        if (result == null)
            return null;

        // a foto fica só em memória e vai direto para o banco como array de bytes
        using var stream = await result.OpenReadAsync();
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);

        return new ArquivoDto { Conteudo = ms.ToArray() };
    }

    public static bool EmailValido(string email)
    {
        var partes = email.Split('@');
        if (partes.Length != 2) return false;
        if (string.IsNullOrWhiteSpace(partes[0])) return false;

        var dominio = partes[1];
        if (string.IsNullOrWhiteSpace(dominio)) return false;
        if (dominio.StartsWith('.') || dominio.EndsWith('.')) return false;

        var labels = dominio.Split('.');
        if (labels.Length < 2) return false;
        if (labels.Any(string.IsNullOrWhiteSpace)) return false;

        return true;
    }

    public static string ApenasDigitos(string? texto) => new([.. (texto ?? string.Empty).Where(char.IsDigit)]);
}
