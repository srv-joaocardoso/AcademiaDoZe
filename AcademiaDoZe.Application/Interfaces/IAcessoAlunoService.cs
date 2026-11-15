// João Victor Cardoso Gonçalves - 155953 - DEV02

using AcademiaDoZe.Application.DTOs;

namespace AcademiaDoZe.Application.Interfaces;

/// <summary>
/// Contrato de serviço para o controle de entrada e saída (catraca) dos alunos.
/// </summary>
public interface IAcessoAlunoService
{
    /// <summary>
    /// Registra a entrada ou a saída do aluno, conforme ele esteja fora ou dentro da academia.
    /// Na entrada valida a matrícula ativa e informa os dias restantes do plano; na saída informa o tempo de permanência.
    /// </summary>
    Task<RegistroAcessoDto> RegistrarAcessoAsync(int alunoId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Indica se o aluno entrou hoje e ainda não registrou a saída.
    /// </summary>
    Task<bool> EstaNaAcademiaAsync(int alunoId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém os acessos de alunos de um dia, do mais recente para o mais antigo.
    /// </summary>
    Task<IEnumerable<AcessoAlunoDto>> ObterAcessosDoDiaAsync(DateOnly data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém os acessos de alunos por aluno e/ou período.
    /// </summary>
    Task<IEnumerable<AcessoAlunoDto>> ObterAcessosPorAlunoPeriodoAsync(int? alunoId = null, DateOnly? inicio = null, DateOnly? fim = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém o último acesso registrado do aluno.
    /// </summary>
    Task<AcessoAlunoDto?> ObterUltimoAcessoAsync(int alunoId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Quantidade de entradas por horário no mês informado.
    /// </summary>
    Task<Dictionary<TimeOnly, int>> ObterHorarioMaisProcuradoPorMesAsync(int mes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Permanência média dos alunos no mês informado.
    /// </summary>
    Task<Dictionary<int, TimeSpan>> ObterPermanenciaMediaPorMesAsync(int mes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Alunos que não registraram acesso nos últimos dias.
    /// </summary>
    Task<IEnumerable<AlunoDto>> ObterAlunosSemAcessoNosUltimosDiasAsync(int dias, CancellationToken cancellationToken = default);
}
