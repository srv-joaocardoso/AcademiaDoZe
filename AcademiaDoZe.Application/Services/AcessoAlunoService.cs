// João Victor Cardoso Gonçalves - 155953 - DEV02

using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Application.Interfaces;
using AcademiaDoZe.Application.Mappings;
using AcademiaDoZe.Domain.Entities;
using AcademiaDoZe.Domain.Repositories;

namespace AcademiaDoZe.Application.Services;

public class AcessoAlunoService : IAcessoAlunoService
{
    private readonly Func<IAcessoAlunoRepository> _acessoRepoFactory;
    private readonly Func<IAlunoRepository> _alunoRepoFactory;
    private readonly Func<IMatriculaRepository> _matriculaRepoFactory;

    public AcessoAlunoService(Func<IAcessoAlunoRepository> acessoRepoFactory, Func<IAlunoRepository> alunoRepoFactory, Func<IMatriculaRepository> matriculaRepoFactory)
    {
        _acessoRepoFactory = acessoRepoFactory ?? throw new ArgumentNullException(nameof(acessoRepoFactory));
        _alunoRepoFactory = alunoRepoFactory ?? throw new ArgumentNullException(nameof(alunoRepoFactory));
        _matriculaRepoFactory = matriculaRepoFactory ?? throw new ArgumentNullException(nameof(matriculaRepoFactory));
    }

    public async Task<RegistroAcessoDto> RegistrarAcessoAsync(int alunoId, CancellationToken cancellationToken = default)
    {
        var aluno = await _alunoRepoFactory.Usar(r => r.ObterPorId(alunoId, cancellationToken))
            ?? throw new InvalidOperationException($"Aluno com ID {alunoId} não encontrado.");

        // hora cheia em segundos, sem frações
        var agora = DateTime.Now;
        agora = agora.AddTicks(-(agora.Ticks % TimeSpan.TicksPerSecond));
        var hoje = DateOnly.FromDateTime(agora);

        var acessosHoje = (await _acessoRepoFactory.Usar(r => r.ObterAcessosPorAlunoPeriodo(alunoId, hoje, hoje, cancellationToken))).ToList();
        bool entrada = acessosHoje.Count % 2 == 0;

        int? diasRestantes = null;
        if (entrada)
        {
            // regra: só entra quem tem matrícula ativa
            var matricula = await _matriculaRepoFactory.Usar(r => r.ObterMatriculaAtivaPorAluno(alunoId, cancellationToken));
            if (matricula == null)
            {
                return Negado(agora, entrada, "Acesso negado: o aluno não possui matrícula ativa.");
            }
            diasRestantes = matricula.DataFim.DayNumber - hoje.DayNumber;
        }

        var acessoResult = AcessoAluno.Criar(0, aluno, agora);
        if (acessoResult.IsFailure)
        {
            if (acessoResult.Notifications.Any(n => n.Mensagem == "DATA_HORA_INTERVALO_INVALIDO"))
                return Negado(agora, entrada, "Acesso negado: fora do horário de funcionamento (06:00 às 22:00).");

            return Negado(agora, entrada, $"Acesso negado: {string.Join(", ", acessoResult.Notifications.Select(n => n.Mensagem))}");
        }

        await _acessoRepoFactory.Usar(r => r.Adicionar(acessoResult.Value!, cancellationToken));

        if (entrada)
        {
            return new RegistroAcessoDto
            {
                Autorizado = true,
                Entrada = true,
                DataHora = agora,
                DiasRestantesPlano = diasRestantes,
                Mensagem = $"Entrada autorizada. Dias restantes de plano: {diasRestantes} dia(s)."
            };
        }

        var permanencia = agora - acessosHoje[^1].DataHora;
        return new RegistroAcessoDto
        {
            Autorizado = true,
            Entrada = false,
            DataHora = agora,
            TempoPermanencia = permanencia,
            Mensagem = $"Saída registrada. Tempo de permanência: {permanencia.ToTempo()}."
        };
    }

    private static RegistroAcessoDto Negado(DateTime dataHora, bool entrada, string mensagem) => new()
    {
        Autorizado = false,
        Entrada = entrada,
        DataHora = dataHora,
        Mensagem = mensagem
    };

    public async Task<bool> EstaNaAcademiaAsync(int alunoId, CancellationToken cancellationToken = default)
    {
        return await _acessoRepoFactory.Usar(r => r.EstaNaAcademia(alunoId, cancellationToken));
    }

    public async Task<IEnumerable<AcessoAlunoDto>> ObterAcessosDoDiaAsync(DateOnly data, CancellationToken cancellationToken = default)
    {
        var lista = await ObterAcessosPorAlunoPeriodoAsync(null, data, data, cancellationToken);
        return [.. lista.OrderByDescending(a => a.DataHora).ThenByDescending(a => a.Id)];
    }

    public async Task<IEnumerable<AcessoAlunoDto>> ObterAcessosPorAlunoPeriodoAsync(int? alunoId = null, DateOnly? inicio = null, DateOnly? fim = null, CancellationToken cancellationToken = default)
    {
        var acessos = await _acessoRepoFactory.Usar(r => r.ObterAcessosPorAlunoPeriodo(alunoId, inicio, fim, cancellationToken));
        return await EnriquecerAsync(acessos, cancellationToken);
    }

    public async Task<AcessoAlunoDto?> ObterUltimoAcessoAsync(int alunoId, CancellationToken cancellationToken = default)
    {
        var acesso = await _acessoRepoFactory.Usar(r => r.ObterUltimoAcesso(alunoId, cancellationToken));
        if (acesso == null) return null;

        var dia = DateOnly.FromDateTime(acesso.DataHora);
        var doDia = await ObterAcessosPorAlunoPeriodoAsync(alunoId, dia, dia, cancellationToken);
        return doDia.FirstOrDefault(a => a.Id == acesso.Id);
    }

    public async Task<Dictionary<TimeOnly, int>> ObterHorarioMaisProcuradoPorMesAsync(int mes, CancellationToken cancellationToken = default)
    {
        return await _acessoRepoFactory.Usar(r => r.ObterHorarioMaisProcuradoPorMes(mes, cancellationToken));
    }

    public async Task<Dictionary<int, TimeSpan>> ObterPermanenciaMediaPorMesAsync(int mes, CancellationToken cancellationToken = default)
    {
        return await _acessoRepoFactory.Usar(r => r.ObterPermanenciaMediaPorMes(mes, cancellationToken));
    }

    public async Task<IEnumerable<AlunoDto>> ObterAlunosSemAcessoNosUltimosDiasAsync(int dias, CancellationToken cancellationToken = default)
    {
        var alunos = await _acessoRepoFactory.Usar(r => r.ObterAlunosSemAcessoNosUltimosDias(dias, cancellationToken));
        return [.. alunos.Select(a => a.ToDto())];
    }

    // carrega o aluno de cada acesso e marca se o registro é entrada ou saída
    private async Task<IEnumerable<AcessoAlunoDto>> EnriquecerAsync(IEnumerable<AcessoAluno> acessos, CancellationToken cancellationToken)
    {
        var lista = acessos.ToList();
        if (lista.Count == 0) return [];

        var alunos = new Dictionary<int, AlunoDto>();
        foreach (var id in lista.Select(a => a.AlunoId).Distinct())
        {
            var aluno = await _alunoRepoFactory.Usar(r => r.ObterPorId(id, cancellationToken));
            if (aluno != null) alunos[id] = aluno.ToDto();
        }

        var resultado = new List<AcessoAlunoDto>();
        foreach (var grupo in lista.GroupBy(a => (a.AlunoId, Dia: a.DataHora.Date)))
        {
            if (!alunos.TryGetValue(grupo.Key.AlunoId, out var alunoDto)) continue;

            int i = 0;
            foreach (var acesso in grupo.OrderBy(a => a.DataHora).ThenBy(a => a.Id))
            {
                resultado.Add(acesso.ToDto(alunoDto, entrada: i % 2 == 0));
                i++;
            }
        }
        return [.. resultado.OrderBy(a => a.DataHora).ThenBy(a => a.Id)];
    }
}
