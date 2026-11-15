// João Victor Cardoso Gonçalves - 155953 - DEV02

using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Application.Interfaces;
using AcademiaDoZe.Application.Mappings;
using AcademiaDoZe.Domain.Entities;
using AcademiaDoZe.Domain.Enums;
using AcademiaDoZe.Domain.Repositories;

namespace AcademiaDoZe.Application.Services;

public class AcessoColaboradorService : IAcessoColaboradorService
{
    private readonly Func<IAcessoColaboradorRepository> _acessoRepoFactory;
    private readonly Func<IColaboradorRepository> _colaboradorRepoFactory;

    public AcessoColaboradorService(Func<IAcessoColaboradorRepository> acessoRepoFactory, Func<IColaboradorRepository> colaboradorRepoFactory)
    {
        _acessoRepoFactory = acessoRepoFactory ?? throw new ArgumentNullException(nameof(acessoRepoFactory));
        _colaboradorRepoFactory = colaboradorRepoFactory ?? throw new ArgumentNullException(nameof(colaboradorRepoFactory));
    }

    // limite diário: 8 horas se for CLT, 6 horas se for estágio
    public static TimeSpan LimiteDiario(ColaboradorVinculo vinculo) => vinculo == ColaboradorVinculo.CLT ? TimeSpan.FromHours(8) : TimeSpan.FromHours(6);

    public async Task<RegistroAcessoDto> RegistrarAcessoAsync(int colaboradorId, CancellationToken cancellationToken = default)
    {
        var colaborador = await _colaboradorRepoFactory.Usar(r => r.ObterPorId(colaboradorId, cancellationToken))
            ?? throw new InvalidOperationException($"Colaborador com ID {colaboradorId} não encontrado.");

        var agora = DateTime.Now;
        agora = agora.AddTicks(-(agora.Ticks % TimeSpan.TicksPerSecond));
        var hoje = DateOnly.FromDateTime(agora);

        var acessosHoje = (await _acessoRepoFactory.Usar(r => r.ObterAcessosPorColaboradorPeriodo(colaboradorId, hoje, hoje, cancellationToken))).ToList();
        bool entrada = acessosHoje.Count % 2 == 0;

        var limite = LimiteDiario(colaborador.Vinculo);
        var horasAntes = SomarPeriodos(acessosHoje, agora);

        // regra: não deixa entrar quem já atingiu o limite de horas do dia
        if (entrada && horasAntes >= limite)
        {
            return Negado(agora, entrada, $"Acesso negado: limite diário de {limite.TotalHours:0} horas já atingido ({horasAntes.ToTempo()} hoje).");
        }

        var acessoResult = AcessoColaborador.Criar(0, colaborador, agora);
        if (acessoResult.IsFailure)
        {
            if (acessoResult.Notifications.Any(n => n.Mensagem == "DATA_HORA_INTERVALO_INVALIDO"))
                return Negado(agora, entrada, "Acesso negado: fora do horário de funcionamento (06:00 às 22:00).");

            return Negado(agora, entrada, $"Acesso negado: {string.Join(", ", acessoResult.Notifications.Select(n => n.Mensagem))}");
        }

        var adicionado = await _acessoRepoFactory.Usar(r => r.Adicionar(acessoResult.Value!, cancellationToken));
        acessosHoje.Add(adicionado);

        // na saída soma todos os registros do dia
        var horasDia = SomarPeriodos(acessosHoje, agora);

        if (entrada)
        {
            return new RegistroAcessoDto
            {
                Autorizado = true,
                Entrada = true,
                DataHora = agora,
                HorasTrabalhadasNoDia = horasDia,
                Mensagem = $"Entrada registrada. Horas trabalhadas hoje até o momento: {horasDia.ToTempo()}."
            };
        }

        var mensagem = $"Saída registrada. Tempo de permanência: {(agora - acessosHoje[^2].DataHora).ToTempo()}. Total do dia: {horasDia.ToTempo()}.";
        if (horasDia > limite)
            mensagem += $" Limite de {limite.TotalHours:0} horas ultrapassado.";

        return new RegistroAcessoDto
        {
            Autorizado = true,
            Entrada = false,
            DataHora = agora,
            TempoPermanencia = agora - acessosHoje[^2].DataHora,
            HorasTrabalhadasNoDia = horasDia,
            Mensagem = mensagem
        };
    }

    // soma os pares entrada/saída; se o último estiver aberto, conta até "agora"
    private static TimeSpan SomarPeriodos(List<AcessoColaborador> acessos, DateTime agora)
    {
        var ordenados = acessos.OrderBy(a => a.DataHora).ThenBy(a => a.Id).ToList();
        var total = TimeSpan.Zero;
        for (int i = 0; i < ordenados.Count; i += 2)
        {
            var fim = i + 1 < ordenados.Count ? ordenados[i + 1].DataHora : agora;
            total += fim - ordenados[i].DataHora;
        }
        return total;
    }

    private static RegistroAcessoDto Negado(DateTime dataHora, bool entrada, string mensagem) => new()
    {
        Autorizado = false,
        Entrada = entrada,
        DataHora = dataHora,
        Mensagem = mensagem
    };

    public async Task<bool> EstaNaAcademiaAsync(int colaboradorId, CancellationToken cancellationToken = default)
    {
        var hoje = DateOnly.FromDateTime(DateTime.Now);
        var acessos = await _acessoRepoFactory.Usar(r => r.ObterAcessosPorColaboradorPeriodo(colaboradorId, hoje, hoje, cancellationToken));
        return acessos.Count() % 2 == 1;
    }

    public async Task<IEnumerable<AcessoColaboradorDto>> ObterAcessosDoDiaAsync(DateOnly data, CancellationToken cancellationToken = default)
    {
        var lista = await ObterAcessosPorColaboradorPeriodoAsync(null, data, data, cancellationToken);
        return [.. lista.OrderByDescending(a => a.DataHora).ThenByDescending(a => a.Id)];
    }

    public async Task<IEnumerable<AcessoColaboradorDto>> ObterAcessosPorColaboradorPeriodoAsync(int? colaboradorId = null, DateOnly? inicio = null, DateOnly? fim = null, CancellationToken cancellationToken = default)
    {
        var acessos = await _acessoRepoFactory.Usar(r => r.ObterAcessosPorColaboradorPeriodo(colaboradorId, inicio, fim, cancellationToken));
        return await EnriquecerAsync(acessos, cancellationToken);
    }

    public async Task<AcessoColaboradorDto?> ObterUltimoAcessoAsync(int colaboradorId, CancellationToken cancellationToken = default)
    {
        var acesso = await _acessoRepoFactory.Usar(r => r.ObterUltimoAcesso(colaboradorId, cancellationToken));
        if (acesso == null) return null;

        var dia = DateOnly.FromDateTime(acesso.DataHora);
        var doDia = await ObterAcessosPorColaboradorPeriodoAsync(colaboradorId, dia, dia, cancellationToken);
        return doDia.FirstOrDefault(a => a.Id == acesso.Id);
    }

    public async Task<TimeSpan> ObterHorasTrabalhadasNoDiaAsync(int colaboradorId, DateOnly data, CancellationToken cancellationToken = default)
    {
        return await _acessoRepoFactory.Usar(r => r.ObterHorasTrabalhadasNoDia(colaboradorId, data, cancellationToken));
    }

    // carrega o colaborador de cada acesso e marca se o registro é entrada ou saída
    private async Task<IEnumerable<AcessoColaboradorDto>> EnriquecerAsync(IEnumerable<AcessoColaborador> acessos, CancellationToken cancellationToken)
    {
        var lista = acessos.ToList();
        if (lista.Count == 0) return [];

        var colaboradores = new Dictionary<int, ColaboradorDto>();
        foreach (var id in lista.Select(a => a.ColaboradorId).Distinct())
        {
            var colaborador = await _colaboradorRepoFactory.Usar(r => r.ObterPorId(id, cancellationToken));
            if (colaborador != null) colaboradores[id] = colaborador.ToDto();
        }

        var resultado = new List<AcessoColaboradorDto>();
        foreach (var grupo in lista.GroupBy(a => (a.ColaboradorId, Dia: a.DataHora.Date)))
        {
            if (!colaboradores.TryGetValue(grupo.Key.ColaboradorId, out var colaboradorDto)) continue;

            int i = 0;
            foreach (var acesso in grupo.OrderBy(a => a.DataHora).ThenBy(a => a.Id))
            {
                resultado.Add(acesso.ToDto(colaboradorDto, entrada: i % 2 == 0));
                i++;
            }
        }
        return [.. resultado.OrderBy(a => a.DataHora).ThenBy(a => a.Id)];
    }
}
