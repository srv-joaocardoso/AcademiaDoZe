// João Victor Cardoso Gonçalves - 155953 - DEV02

using AcademiaDoZe.Domain.Entities;
using AcademiaDoZe.Domain.Repositories;
using AcademiaDoZe.Infrastructure.Data;
using AcademiaDoZe.Infrastructure.Exceptions;
using System.Data;
using System.Data.Common;

namespace AcademiaDoZe.Infrastructure.Repositories;

// cada linha da tb_acesso é um registro de catraca; no mesmo dia, os registros se alternam entre entrada e saída
public class AcessoAlunoRepository : BaseRepository, IAcessoAlunoRepository
{
    private const int PessoaTipoAluno = 0;

    public AcessoAlunoRepository(string connectionString, DatabaseType databaseType) : base(connectionString, databaseType)
    {
    }

    private static string BaseSelectQuery => $"""
SELECT
ac.id_acesso, ac.pessoa_id, ac.data_hora,
a.id_aluno, a.cpf, a.nome, a.nascimento, a.telefone, a.email, a.logradouro_id, a.numero, a.complemento, a.senha, a.foto,
l.id_logradouro, l.cep, l.nome AS nome_logradouro, l.bairro, l.cidade, l.estado, l.pais
FROM tb_acesso ac
INNER JOIN tb_aluno a ON ac.pessoa_id = a.id_aluno
INNER JOIN tb_logradouro l ON a.logradouro_id = l.id_logradouro
WHERE ac.pessoa_tipo = {PessoaTipoAluno}
""";

    public static AcessoAluno Map(DbDataReader reader)
    {
        try
        {
            int id = reader.GetInt32Value("id_acesso");
            DateTime dataHora = reader.GetDateTimeValue("data_hora");
            var aluno = AlunoRepository.Map(reader);
            var result = AcessoAluno.Criar(id, aluno, dataHora);
            if (result.IsFailure)
            {
                throw new InfrastructureException("ERRO_DOMINIO_MAPEAMENTO", $"Erro de domínio ao mapear acesso ID {id}: {string.Join(", ", result.Notifications.Select(n => n.Mensagem))}");
            }
            return result.Value!;
        }
        catch (Exception ex) when (ex is not InfrastructureException)
        {
            throw new InfrastructureException("ERRO_MAPEAMENTO_ACESSO_ALUNO", $"Erro ao mapear dados do acesso do aluno: {ex.Message}", ex);
        }
    }

    private async Task<List<AcessoAluno>> ListarAsync(string query, Action<DbCommand>? parametros, CancellationToken cancellationToken)
    {
        await using var command = await CreateCommandAsync(query, cancellationToken);
        parametros?.Invoke(command);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var acessos = new List<AcessoAluno>();
        while (await reader.ReadAsync(cancellationToken))
        {
            acessos.Add(Map(reader));
        }
        return acessos;
    }

    public async Task<AcessoAluno?> ObterPorId(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var acessos = await ListarAsync($"{BaseSelectQuery} AND ac.id_acesso = @Id", c => c.AddParameter("@Id", id, DbType.Int32), cancellationToken);
            return acessos.FirstOrDefault();
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_OBTER_POR_ID", $"Erro ao obter acesso de aluno por ID {id}: {ex.Message}", ex);
        }
    }

    public async Task<IEnumerable<AcessoAluno>> ObterTodos(CancellationToken cancellationToken = default)
    {
        try
        {
            return await ListarAsync($"{BaseSelectQuery} ORDER BY ac.data_hora DESC", null, cancellationToken);
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_OBTER_TODOS", $"Erro ao obter todos os acessos de alunos: {ex.Message}", ex);
        }
    }

    public async Task<AcessoAluno> Adicionar(AcessoAluno entity, CancellationToken cancellationToken = default)
    {
        try
        {
            string query = FormatInsertQuery("INSERT INTO tb_acesso (pessoa_tipo, pessoa_id, data_hora) VALUES (@PessoaTipo, @PessoaId, @DataHora)");
            await using var command = await CreateCommandAsync(query, cancellationToken);
            command.AddParameter("@PessoaTipo", PessoaTipoAluno, DbType.Int32);
            command.AddParameter("@PessoaId", entity.AlunoId, DbType.Int32);
            command.AddParameter("@DataHora", entity.DataHora, DbType.DateTime);
            int id = await command.ExecuteScalarIdAsync("ERRO_ADICIONAR_ACESSO_ALUNO", "Falha ao obter ID inserido para o acesso do aluno.", cancellationToken);
            var idProperty = typeof(Entity).GetProperty("Id");
            idProperty?.SetValue(entity, id);
            return entity;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_ADICIONAR_ACESSO_ALUNO", $"Erro ao adicionar acesso do aluno ID {entity.AlunoId}: {ex.Message}", ex);
        }
    }

    public async Task<AcessoAluno> Atualizar(AcessoAluno entity, CancellationToken cancellationToken = default)
    {
        try
        {
            string query = "UPDATE tb_acesso SET pessoa_id = @PessoaId, data_hora = @DataHora WHERE id_acesso = @Id AND pessoa_tipo = @PessoaTipo";
            await using var command = await CreateCommandAsync(query, cancellationToken);
            command.AddParameter("@Id", entity.Id, DbType.Int32);
            command.AddParameter("@PessoaTipo", PessoaTipoAluno, DbType.Int32);
            command.AddParameter("@PessoaId", entity.AlunoId, DbType.Int32);
            command.AddParameter("@DataHora", entity.DataHora, DbType.DateTime);
            int rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
            if (rowsAffected == 0)
            {
                throw new InfrastructureException("REGISTRO_NAO_ENCONTRADO", $"Nenhum acesso de aluno encontrado com o ID {entity.Id} para atualização.");
            }
            return entity;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_ATUALIZAR_ACESSO_ALUNO", $"Erro ao atualizar acesso ID {entity.Id}: {ex.Message}", ex);
        }
    }

    public async Task<bool> Remover(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            string query = "DELETE FROM tb_acesso WHERE id_acesso = @Id AND pessoa_tipo = @PessoaTipo";
            await using var command = await CreateCommandAsync(query, cancellationToken);
            command.AddParameter("@Id", id, DbType.Int32);
            command.AddParameter("@PessoaTipo", PessoaTipoAluno, DbType.Int32);
            return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_REMOVER_ACESSO_ALUNO", $"Erro ao remover acesso ID {id}: {ex.Message}", ex);
        }
    }

    public async Task<IEnumerable<AcessoAluno>> ObterAcessosPorAlunoPeriodo(int? alunoId = null, DateOnly? inicio = null, DateOnly? fim = null, CancellationToken cancellationToken = default)
    {
        try
        {
            string query = BaseSelectQuery;
            if (alunoId.HasValue) query += " AND ac.pessoa_id = @AlunoId";
            if (inicio.HasValue) query += " AND ac.data_hora >= @Inicio";
            if (fim.HasValue) query += " AND ac.data_hora < @Fim";
            query += " ORDER BY ac.data_hora, ac.id_acesso";

            return await ListarAsync(query, c =>
            {
                if (alunoId.HasValue) c.AddParameter("@AlunoId", alunoId.Value, DbType.Int32);
                if (inicio.HasValue) c.AddParameter("@Inicio", inicio.Value.ToDateTime(TimeOnly.MinValue), DbType.DateTime);
                // fim é inclusivo: pega até o fim do dia informado
                if (fim.HasValue) c.AddParameter("@Fim", fim.Value.AddDays(1).ToDateTime(TimeOnly.MinValue), DbType.DateTime);
            }, cancellationToken);
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_OBTER_ACESSOS_PERIODO", $"Erro ao obter acessos de alunos no período: {ex.Message}", ex);
        }
    }

    public async Task<AcessoAluno?> ObterUltimoAcesso(int alunoId, CancellationToken cancellationToken = default)
    {
        try
        {
            var acessos = await ListarAsync($"{BaseSelectQuery} AND ac.pessoa_id = @AlunoId ORDER BY ac.data_hora DESC, ac.id_acesso DESC",
                c => c.AddParameter("@AlunoId", alunoId, DbType.Int32), cancellationToken);
            return acessos.FirstOrDefault();
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_OBTER_ULTIMO_ACESSO", $"Erro ao obter último acesso do aluno ID {alunoId}: {ex.Message}", ex);
        }
    }

    // número ímpar de registros no dia = entrou e ainda não saiu
    public async Task<bool> EstaNaAcademia(int alunoId, CancellationToken cancellationToken = default)
    {
        var hoje = DateOnly.FromDateTime(DateTime.Now);
        var acessos = await ObterAcessosPorAlunoPeriodo(alunoId, hoje, hoje, cancellationToken);
        return acessos.Count() % 2 == 1;
    }

    public async Task<Dictionary<TimeOnly, int>> ObterHorarioMaisProcuradoPorMes(int mes, CancellationToken cancellationToken = default)
    {
        try
        {
            var acessos = await ListarAsync($"{BaseSelectQuery} AND {GetDateMonthExpression("ac.data_hora")} = @Mes ORDER BY ac.data_hora",
                c => c.AddParameter("@Mes", mes, DbType.Int32), cancellationToken);

            // só as entradas contam para o horário de procura
            return Entradas(acessos)
                .GroupBy(a => new TimeOnly(a.DataHora.Hour, 0))
                .OrderByDescending(g => g.Count())
                .ToDictionary(g => g.Key, g => g.Count());
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_OBTER_HORARIO_PROCURA", $"Erro ao obter horário mais procurado do mês {mes}: {ex.Message}", ex);
        }
    }

    public async Task<Dictionary<int, TimeSpan>> ObterPermanenciaMediaPorMes(int mes, CancellationToken cancellationToken = default)
    {
        try
        {
            var acessos = await ListarAsync($"{BaseSelectQuery} AND {GetDateMonthExpression("ac.data_hora")} = @Mes ORDER BY ac.data_hora",
                c => c.AddParameter("@Mes", mes, DbType.Int32), cancellationToken);

            var permanencias = new List<TimeSpan>();
            foreach (var grupo in acessos.GroupBy(a => (a.AlunoId, Dia: a.DataHora.Date)))
            {
                var lista = grupo.OrderBy(a => a.DataHora).ToList();
                for (int i = 0; i + 1 < lista.Count; i += 2)
                {
                    permanencias.Add(lista[i + 1].DataHora - lista[i].DataHora);
                }
            }

            var media = permanencias.Count == 0 ? TimeSpan.Zero : TimeSpan.FromTicks((long)permanencias.Average(p => p.Ticks));
            return new Dictionary<int, TimeSpan> { [mes] = media };
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_OBTER_PERMANENCIA_MEDIA", $"Erro ao obter permanência média do mês {mes}: {ex.Message}", ex);
        }
    }

    public async Task<IEnumerable<Aluno>> ObterAlunosSemAcessoNosUltimosDias(int dias, CancellationToken cancellationToken = default)
    {
        try
        {
            string query = """
SELECT a.id_aluno, a.cpf, a.nome, a.nascimento, a.telefone, a.email, a.logradouro_id, a.numero, a.complemento, a.senha, a.foto,
l.id_logradouro, l.cep, l.nome AS nome_logradouro, l.bairro, l.cidade, l.estado, l.pais
FROM tb_aluno a
INNER JOIN tb_logradouro l ON a.logradouro_id = l.id_logradouro
WHERE NOT EXISTS (SELECT 1 FROM tb_acesso ac WHERE ac.pessoa_tipo = @PessoaTipo AND ac.pessoa_id = a.id_aluno AND ac.data_hora >= @Limite)
ORDER BY a.nome
""";
            await using var command = await CreateCommandAsync(query, cancellationToken);
            command.AddParameter("@PessoaTipo", PessoaTipoAluno, DbType.Int32);
            command.AddParameter("@Limite", DateTime.Today.AddDays(-dias), DbType.DateTime);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var alunos = new List<Aluno>();
            while (await reader.ReadAsync(cancellationToken))
            {
                alunos.Add(AlunoRepository.Map(reader));
            }
            return alunos;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_OBTER_ALUNOS_SEM_ACESSO", $"Erro ao obter alunos sem acesso nos últimos {dias} dias: {ex.Message}", ex);
        }
    }

    // primeiro registro do dia de cada aluno é entrada, o segundo é saída, e assim por diante
    private static IEnumerable<AcessoAluno> Entradas(IEnumerable<AcessoAluno> acessos)
    {
        return acessos
            .GroupBy(a => (a.AlunoId, Dia: a.DataHora.Date))
            .SelectMany(g => g.OrderBy(a => a.DataHora).Where((_, i) => i % 2 == 0));
    }
}
