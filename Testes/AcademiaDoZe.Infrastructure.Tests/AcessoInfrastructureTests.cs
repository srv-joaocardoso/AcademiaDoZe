// João Victor Cardoso Gonçalves - 155953 - DEV02

using AcademiaDoZe.Domain.Entities;
using AcademiaDoZe.Infrastructure.Repositories;

namespace AcademiaDoZe.Infrastructure.Tests;

public class AcessoInfrastructureTests : TestBase
{
    private readonly LogradouroRepository _logradouroRepo;
    private readonly AlunoRepository _alunoRepo;
    private readonly ColaboradorRepository _colaboradorRepo;
    private readonly AcessoAlunoRepository _acessoAlunoRepo;
    private readonly AcessoColaboradorRepository _acessoColaboradorRepo;

    // dia fixo no passado, para não misturar com os acessos de hoje
    private static readonly DateOnly Dia = new(2026, 1, 5);

    public AcessoInfrastructureTests()
    {
        _logradouroRepo = new LogradouroRepository(ConnectionString, DatabaseType);
        _alunoRepo = new AlunoRepository(ConnectionString, DatabaseType);
        _colaboradorRepo = new ColaboradorRepository(ConnectionString, DatabaseType);
        _acessoAlunoRepo = new AcessoAlunoRepository(ConnectionString, DatabaseType);
        _acessoColaboradorRepo = new AcessoColaboradorRepository(ConnectionString, DatabaseType);
    }

    private async Task<AcessoAluno> InserirAcessoAlunoAsync(Aluno aluno, DateTime dataHora)
    {
        var result = AcessoAluno.Criar(0, aluno, dataHora);
        Assert.True(result.IsSuccess);
        return await _acessoAlunoRepo.Adicionar(result.Value!);
    }

    private async Task<AcessoColaborador> InserirAcessoColaboradorAsync(Colaborador colaborador, DateTime dataHora)
    {
        var result = AcessoColaborador.Criar(0, colaborador, dataHora);
        Assert.True(result.IsSuccess);
        return await _acessoColaboradorRepo.Adicionar(result.Value!);
    }

    [Fact]
    public async Task AcessoAluno_Adicionar_E_ObterPorId_Sucesso()
    {
        var aluno = await AlunoInfrastructureTests.CriarEInserirAlunoAsync(_alunoRepo, _logradouroRepo);
        var dataHora = Dia.ToDateTime(new TimeOnly(8, 15, 30));

        var inserido = await InserirAcessoAlunoAsync(aluno, dataHora);
        Assert.True(inserido.Id > 0);

        var obtido = await _acessoAlunoRepo.ObterPorId(inserido.Id);
        Assert.NotNull(obtido);
        Assert.Equal(aluno.Id, obtido.AlunoId);
        Assert.Equal(dataHora, obtido.DataHora);

        Assert.True(await _acessoAlunoRepo.Remover(inserido.Id));
        Assert.Null(await _acessoAlunoRepo.ObterPorId(inserido.Id));
    }

    [Fact]
    public async Task AcessoAluno_ObterPorPeriodo_E_UltimoAcesso_Sucesso()
    {
        var aluno = await AlunoInfrastructureTests.CriarEInserirAlunoAsync(_alunoRepo, _logradouroRepo);
        var entrada = await InserirAcessoAlunoAsync(aluno, Dia.ToDateTime(new TimeOnly(7, 0)));
        var saida = await InserirAcessoAlunoAsync(aluno, Dia.ToDateTime(new TimeOnly(8, 30)));

        var doDia = (await _acessoAlunoRepo.ObterAcessosPorAlunoPeriodo(aluno.Id, Dia, Dia)).ToList();
        Assert.Equal(2, doDia.Count);
        Assert.Equal(entrada.Id, doDia[0].Id);
        Assert.Equal(saida.Id, doDia[1].Id);

        var outroDia = await _acessoAlunoRepo.ObterAcessosPorAlunoPeriodo(aluno.Id, Dia.AddDays(1), Dia.AddDays(1));
        Assert.Empty(outroDia);

        var ultimo = await _acessoAlunoRepo.ObterUltimoAcesso(aluno.Id);
        Assert.NotNull(ultimo);
        Assert.Equal(saida.Id, ultimo.Id);

        var permanencia = await _acessoAlunoRepo.ObterPermanenciaMediaPorMes(Dia.Month);
        Assert.True(permanencia[Dia.Month] > TimeSpan.Zero);

        var horarios = await _acessoAlunoRepo.ObterHorarioMaisProcuradoPorMes(Dia.Month);
        Assert.True(horarios.ContainsKey(new TimeOnly(7, 0)));

        await _acessoAlunoRepo.Remover(entrada.Id);
        await _acessoAlunoRepo.Remover(saida.Id);
    }

    [Fact]
    public async Task AcessoAluno_EstaNaAcademia_SomenteComEntradaHoje()
    {
        var aluno = await AlunoInfrastructureTests.CriarEInserirAlunoAsync(_alunoRepo, _logradouroRepo);
        Assert.False(await _acessoAlunoRepo.EstaNaAcademia(aluno.Id));

        var entrada = await InserirAcessoAlunoAsync(aluno, DateTime.Today.AddHours(7));
        Assert.True(await _acessoAlunoRepo.EstaNaAcademia(aluno.Id));

        var saida = await InserirAcessoAlunoAsync(aluno, DateTime.Today.AddHours(8));
        Assert.False(await _acessoAlunoRepo.EstaNaAcademia(aluno.Id));

        await _acessoAlunoRepo.Remover(entrada.Id);
        await _acessoAlunoRepo.Remover(saida.Id);
    }

    [Fact]
    public async Task AcessoAluno_ObterAlunosSemAcesso_IncluiAlunoSemRegistro()
    {
        var aluno = await AlunoInfrastructureTests.CriarEInserirAlunoAsync(_alunoRepo, _logradouroRepo);
        var semAcesso = await _acessoAlunoRepo.ObterAlunosSemAcessoNosUltimosDias(30);
        Assert.Contains(semAcesso, a => a.Id == aluno.Id);
    }

    [Fact]
    public async Task AcessoColaborador_Adicionar_E_HorasTrabalhadas_Sucesso()
    {
        var colaborador = await ColaboradorInfrastructureTests.CriarEInserirColaboradorAsync(_colaboradorRepo, _logradouroRepo);

        // dois períodos no mesmo dia: 2h + 1h30
        var a1 = await InserirAcessoColaboradorAsync(colaborador, Dia.ToDateTime(new TimeOnly(8, 0)));
        var a2 = await InserirAcessoColaboradorAsync(colaborador, Dia.ToDateTime(new TimeOnly(10, 0)));
        var a3 = await InserirAcessoColaboradorAsync(colaborador, Dia.ToDateTime(new TimeOnly(13, 0)));
        var a4 = await InserirAcessoColaboradorAsync(colaborador, Dia.ToDateTime(new TimeOnly(14, 30)));

        var obtido = await _acessoColaboradorRepo.ObterPorId(a1.Id);
        Assert.NotNull(obtido);
        Assert.Equal(colaborador.Id, obtido.ColaboradorId);

        var horas = await _acessoColaboradorRepo.ObterHorasTrabalhadasNoDia(colaborador.Id, Dia);
        Assert.Equal(TimeSpan.FromMinutes(210), horas);

        var ultimo = await _acessoColaboradorRepo.ObterUltimoAcesso(colaborador.Id);
        Assert.Equal(a4.Id, ultimo!.Id);

        foreach (var id in new[] { a1.Id, a2.Id, a3.Id, a4.Id })
            Assert.True(await _acessoColaboradorRepo.Remover(id));
    }
}
