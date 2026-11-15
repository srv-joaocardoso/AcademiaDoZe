// João Victor Cardoso Gonçalves - 155953 - DEV02

namespace AcademiaDoZe.Application.DTOs;

public class AcessoColaboradorDto
{
    public int Id { get; set; }
    public required ColaboradorDto ColaboradorAcesso { get; set; }
    public required DateTime DataHora { get; set; }
    // calculado pela ordem dos registros do dia (1º entrada, 2º saída, ...)
    public bool Entrada { get; set; }
}
