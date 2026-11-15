// João Victor Cardoso Gonçalves - 155953 - DEV02

using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Domain.Entities;

namespace AcademiaDoZe.Application.Mappings;

public static class AcessoMappingExtensions
{
    public static AcessoAlunoDto ToDto(this AcessoAluno acesso, AlunoDto alunoDto, bool entrada = true)
    {
        ArgumentNullException.ThrowIfNull(acesso);
        ArgumentNullException.ThrowIfNull(alunoDto);

        return new AcessoAlunoDto
        {
            Id = acesso.Id,
            AlunoAcesso = alunoDto,
            DataHora = acesso.DataHora,
            Entrada = entrada
        };
    }

    public static AcessoColaboradorDto ToDto(this AcessoColaborador acesso, ColaboradorDto colaboradorDto, bool entrada = true)
    {
        ArgumentNullException.ThrowIfNull(acesso);
        ArgumentNullException.ThrowIfNull(colaboradorDto);

        return new AcessoColaboradorDto
        {
            Id = acesso.Id,
            ColaboradorAcesso = colaboradorDto,
            DataHora = acesso.DataHora,
            Entrada = entrada
        };
    }

    public static string ToTempo(this TimeSpan tempo)
    {
        return $"{(int)tempo.TotalHours:00}:{tempo.Minutes:00}:{tempo.Seconds:00}";
    }
}
