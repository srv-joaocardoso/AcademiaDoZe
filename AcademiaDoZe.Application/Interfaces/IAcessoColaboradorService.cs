// João Victor Cardoso Gonçalves - 155953 - DEV02

using AcademiaDoZe.Application.DTOs;

namespace AcademiaDoZe.Application.Interfaces;

/// <summary>
/// Contrato de serviço para o controle de entrada e saída (catraca) dos colaboradores.
/// </summary>
public interface IAcessoColaboradorService
{
    /// <summary>
    /// Registra a entrada ou a saída do colaborador, conforme ele esteja fora ou dentro da academia.
    /// Na entrada valida o limite diário (8h CLT, 6h estágio); na saída informa o total do dia.
    /// </summary>
    Task<RegistroAcessoDto> RegistrarAcessoAsync(int colaboradorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Indica se o colaborador entrou hoje e ainda não registrou a saída.
    /// </summary>
    Task<bool> EstaNaAcademiaAsync(int colaboradorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém os acessos de colaboradores de um dia, do mais recente para o mais antigo.
    /// </summary>
    Task<IEnumerable<AcessoColaboradorDto>> ObterAcessosDoDiaAsync(DateOnly data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém os acessos de colaboradores por colaborador e/ou período.
    /// </summary>
    Task<IEnumerable<AcessoColaboradorDto>> ObterAcessosPorColaboradorPeriodoAsync(int? colaboradorId = null, DateOnly? inicio = null, DateOnly? fim = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém o último acesso registrado do colaborador.
    /// </summary>
    Task<AcessoColaboradorDto?> ObterUltimoAcessoAsync(int colaboradorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soma dos períodos entre entrada e saída no dia informado.
    /// </summary>
    Task<TimeSpan> ObterHorasTrabalhadasNoDiaAsync(int colaboradorId, DateOnly data, CancellationToken cancellationToken = default);
}
