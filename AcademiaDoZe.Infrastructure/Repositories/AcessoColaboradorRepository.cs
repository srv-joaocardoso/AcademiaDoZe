// João Victor Cardoso Gonçalves - 155953 - DEV02

using AcademiaDoZe.Domain.Entities;
using AcademiaDoZe.Domain.Repositories;
using AcademiaDoZe.Infrastructure.Data;
using AcademiaDoZe.Infrastructure.Exceptions;
using System.Data;
using System.Data.Common;

namespace AcademiaDoZe.Infrastructure.Repositories;

// cada linha da tb_acesso é um registro de catraca; no mesmo dia, os registros se alternam entre entrada e saída
public class AcessoColaboradorRepository : BaseRepository, IAcessoColaboradorRepository
{
    private const int PessoaTipoColaborador = 1;

    public AcessoColaboradorRepository(string connectionString, DatabaseType databaseType) : base(connectionString, databaseType)
    {
    }

    private static string BaseSelectQuery => $"""
SELECT
ac.id_acesso, ac.pessoa_id, ac.data_hora,
c.id_colaborador, c.cpf, c.nome, c.nascimento, c.telefone, c.email,
c.logradouro_id, c.numero, c.complemento, c.senha, c.foto, c.admissao, c.tipo, c.vinculo,
l.id_logradouro, l.cep, l.nome AS logradouro_nome, l.bairro, l.cidade, l.estado, l.pais
FROM tb_acesso ac
INNER JOIN tb_colaborador c ON ac.pessoa_id = c.id_colaborador
INNER JOIN tb_logradouro l ON c.logradouro_id = l.id_logradouro
WHERE ac.pessoa_tipo = {PessoaTipoColaborador}
""";

    public static AcessoColaborador Map(DbDataReader reader)
    {
        try
        {
            int id = reader.GetInt32Value("id_acesso");
            DateTime dataHora = reader.GetDateTimeValue("data_hora");
            var colaborador = ColaboradorRepository.Map(reader);
            var result = AcessoColaborador.Criar(id, colaborador, dataHora);
            if (result.IsFailure)
            {
                throw new InfrastructureException("ERRO_DOMINIO_MAPEAMENTO", $"Erro de domínio ao mapear acesso ID {id}: {string.Join(", ", result.Notifications.Select(n => n.Mensagem))}");
            }
            return result.Value!;
        }
        catch (Exception ex) when (ex is not InfrastructureException)
        {
            throw new InfrastructureException("ERRO_MAPEAMENTO_ACESSO_COLABORADOR", $"Erro ao mapear dados do acesso do colaborador: {ex.Message}", ex);
        }
    }

    private async Task<List<AcessoColaborador>> ListarAsync(string query, Action<DbCommand>? parametros, CancellationToken cancellationToken)
    {
        await using var command = await CreateCommandAsync(query, cancellationToken);
        parametros?.Invoke(command);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var acessos = new List<AcessoColaborador>();
        while (await reader.ReadAsync(cancellationToken))
        {
            acessos.Add(Map(reader));
        }
        return acessos;
    }

    public async Task<AcessoColaborador?> ObterPorId(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var acessos = await ListarAsync($"{BaseSelectQuery} AND ac.id_acesso = @Id", c => c.AddParameter("@Id", id, DbType.Int32), cancellationToken);
            return acessos.FirstOrDefault();
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_OBTER_POR_ID", $"Erro ao obter acesso de colaborador por ID {id}: {ex.Message}", ex);
        }
    }

    public async Task<IEnumerable<AcessoColaborador>> ObterTodos(CancellationToken cancellationToken = default)
    {
        try
        {
            return await ListarAsync($"{BaseSelectQuery} ORDER BY ac.data_hora DESC", null, cancellationToken);
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_OBTER_TODOS", $"Erro ao obter todos os acessos de colaboradores: {ex.Message}", ex);
        }
    }

    public async Task<AcessoColaborador> Adicionar(AcessoColaborador entity, CancellationToken cancellationToken = default)
    {
        try
        {
            string query = FormatInsertQuery("INSERT INTO tb_acesso (pessoa_tipo, pessoa_id, data_hora) VALUES (@PessoaTipo, @PessoaId, @DataHora)");
            await using var command = await CreateCommandAsync(query, cancellationToken);
            command.AddParameter("@PessoaTipo", PessoaTipoColaborador, DbType.Int32);
            command.AddParameter("@PessoaId", entity.ColaboradorId, DbType.Int32);
            command.AddParameter("@DataHora", entity.DataHora, DbType.DateTime);
            int id = await command.ExecuteScalarIdAsync("ERRO_ADICIONAR_ACESSO_COLABORADOR", "Falha ao obter ID inserido para o acesso do colaborador.", cancellationToken);
            var idProperty = typeof(Entity).GetProperty("Id");
            idProperty?.SetValue(entity, id);
            return entity;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_ADICIONAR_ACESSO_COLABORADOR", $"Erro ao adicionar acesso do colaborador ID {entity.ColaboradorId}: {ex.Message}", ex);
        }
    }

    public async Task<AcessoColaborador> Atualizar(AcessoColaborador entity, CancellationToken cancellationToken = default)
    {
        try
        {
            string query = "UPDATE tb_acesso SET pessoa_id = @PessoaId, data_hora = @DataHora WHERE id_acesso = @Id AND pessoa_tipo = @PessoaTipo";
            await using var command = await CreateCommandAsync(query, cancellationToken);
            command.AddParameter("@Id", entity.Id, DbType.Int32);
            command.AddParameter("@PessoaTipo", PessoaTipoColaborador, DbType.Int32);
            command.AddParameter("@PessoaId", entity.ColaboradorId, DbType.Int32);
            command.AddParameter("@DataHora", entity.DataHora, DbType.DateTime);
            int rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
            if (rowsAffected == 0)
            {
                throw new InfrastructureException("REGISTRO_NAO_ENCONTRADO", $"Nenhum acesso de colaborador encontrado com o ID {entity.Id} para atualização.");
            }
            return entity;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_ATUALIZAR_ACESSO_COLABORADOR", $"Erro ao atualizar acesso ID {entity.Id}: {ex.Message}", ex);
        }
    }

    public async Task<bool> Remover(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            string query = "DELETE FROM tb_acesso WHERE id_acesso = @Id AND pessoa_tipo = @PessoaTipo";
            await using var command = await CreateCommandAsync(query, cancellationToken);
            command.AddParameter("@Id", id, DbType.Int32);
            command.AddParameter("@PessoaTipo", PessoaTipoColaborador, DbType.Int32);
            return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_REMOVER_ACESSO_COLABORADOR", $"Erro ao remover acesso ID {id}: {ex.Message}", ex);
        }
    }

    public async Task<IEnumerable<AcessoColaborador>> ObterAcessosPorColaboradorPeriodo(int? colaboradorId = null, DateOnly? inicio = null, DateOnly? fim = null, CancellationToken cancellationToken = default)
    {
        try
        {
            string query = BaseSelectQuery;
            if (colaboradorId.HasValue) query += " AND ac.pessoa_id = @ColaboradorId";
            if (inicio.HasValue) query += " AND ac.data_hora >= @Inicio";
            if (fim.HasValue) query += " AND ac.data_hora < @Fim";
            query += " ORDER BY ac.data_hora, ac.id_acesso";

            return await ListarAsync(query, c =>
            {
                if (colaboradorId.HasValue) c.AddParameter("@ColaboradorId", colaboradorId.Value, DbType.Int32);
                if (inicio.HasValue) c.AddParameter("@Inicio", inicio.Value.ToDateTime(TimeOnly.MinValue), DbType.DateTime);
                // fim é inclusivo: pega até o fim do dia informado
                if (fim.HasValue) c.AddParameter("@Fim", fim.Value.AddDays(1).ToDateTime(TimeOnly.MinValue), DbType.DateTime);
            }, cancellationToken);
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_OBTER_ACESSOS_PERIODO", $"Erro ao obter acessos de colaboradores no período: {ex.Message}", ex);
        }
    }

    public async Task<AcessoColaborador?> ObterUltimoAcesso(int colaboradorId, CancellationToken cancellationToken = default)
    {
        try
        {
            var acessos = await ListarAsync($"{BaseSelectQuery} AND ac.pessoa_id = @ColaboradorId ORDER BY ac.data_hora DESC, ac.id_acesso DESC",
                c => c.AddParameter("@ColaboradorId", colaboradorId, DbType.Int32), cancellationToken);
            return acessos.FirstOrDefault();
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_OBTER_ULTIMO_ACESSO", $"Erro ao obter último acesso do colaborador ID {colaboradorId}: {ex.Message}", ex);
        }
    }

    // soma os pares entrada/saída do dia; se ainda estiver dentro (sem saída), conta até agora
    public async Task<TimeSpan> ObterHorasTrabalhadasNoDia(int colaboradorId, DateOnly data, CancellationToken cancellationToken = default)
    {
        var acessos = (await ObterAcessosPorColaboradorPeriodo(colaboradorId, data, data, cancellationToken)).ToList();

        var total = TimeSpan.Zero;
        for (int i = 0; i < acessos.Count; i += 2)
        {
            if (i + 1 < acessos.Count)
            {
                total += acessos[i + 1].DataHora - acessos[i].DataHora;
            }
            else if (data == DateOnly.FromDateTime(DateTime.Now))
            {
                total += DateTime.Now - acessos[i].DataHora;
            }
        }
        return total;
    }
}
