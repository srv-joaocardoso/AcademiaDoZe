// João Victor Cardoso Gonçalves - 155953 - DEV02

namespace AcademiaDoZe.Application.DTOs;

// retorno da catraca ao registrar entrada ou saída
public class RegistroAcessoDto
{
    public bool Autorizado { get; set; }
    public bool Entrada { get; set; }
    public required string Mensagem { get; set; }
    public DateTime DataHora { get; set; }
    public int? DiasRestantesPlano { get; set; }
    public TimeSpan? TempoPermanencia { get; set; }
    public TimeSpan? HorasTrabalhadasNoDia { get; set; }
}
