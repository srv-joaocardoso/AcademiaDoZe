// João Victor Cardoso Gonçalves - 155953 - DEV02

using AcademiaDoZe.Domain.Entities;
using AcademiaDoZe.Domain.Enums;
using AcademiaDoZe.Domain.ValueObjects;

namespace AcademiaDoZe.Domain.Tests.Entities;

public class ColaboradorTests
{
    private static Logradouro GetValidLogradouro() => Logradouro.Criar(1, "12345-678", "Rua Teste", "Bairro", "Cidade", "SP", "Brasil").Value!;
    private static Arquivo GetValidArquivo() => Arquivo.Criar([1, 2, 3]).Value!;

    private static AcademiaDoZe.Domain.Common.Result<Colaborador> CriarColaborador(DateOnly dataAdmissao, ColaboradorTipo tipo, ColaboradorVinculo vinculo) =>
        Colaborador.Criar(1, "Fulano", "529.982.247-25", DateOnly.FromDateTime(DateTime.Today.AddYears(-30)), "(11) 91234-5678", "user@example.com",
            GetValidLogradouro(), "123", "", "Abcdef", GetValidArquivo(), dataAdmissao, tipo, vinculo);

    [Theory(DisplayName = "Colaborador: data admissao obrigatória -> DATA_ADMISSAO_OBRIGATORIO")]
    [InlineData(true)]
    [InlineData(false)]
    public void Deve_Falhar_Criacao_Quando_DataAdmissaoPadrao(bool useDefault)
    {
        var dataAdmissao = useDefault ? default : DateOnly.FromDateTime(DateTime.Today.AddYears(-1));

        var result = CriarColaborador(dataAdmissao, ColaboradorTipo.Atendente, ColaboradorVinculo.CLT);

        if (useDefault)
        {
            Assert.True(result.IsFailure);
            Assert.Contains(result.Notifications, n => n.Mensagem == "DATA_ADMISSAO_OBRIGATORIO");
        }
        else
        {
            Assert.True(result.IsSuccess);
        }
    }

    [Theory(DisplayName = "Colaborador: Administrador com vínculo inválido -> ADMINISTRADOR_CLT_INVALIDO")]
    [InlineData(ColaboradorTipo.Administrador, ColaboradorVinculo.Estagio)]
    [InlineData(ColaboradorTipo.Administrador, ColaboradorVinculo.CLT)]
    public void Deve_Falhar_Criacao_Quando_AdminComVinculoInvalido(ColaboradorTipo tipo, ColaboradorVinculo vinculo)
    {
        var result = CriarColaborador(DateOnly.FromDateTime(DateTime.Today.AddYears(-1)), tipo, vinculo);

        if (tipo == ColaboradorTipo.Administrador && vinculo == ColaboradorVinculo.Estagio)
        {
            Assert.True(result.IsFailure);
            Assert.Contains(result.Notifications, n => n.Mensagem == "ADMINISTRADOR_CLT_INVALIDO");
        }
        else
        {
            Assert.True(result.IsSuccess);
        }
    }

    [Theory(DisplayName = "Colaborador: data admissao futura -> DATA_ADMISSAO_MAIOR_QUE_ATUAL")]
    [InlineData(1)]
    [InlineData(-1)]
    public void Deve_Falhar_Criacao_Quando_DataAdmissaoFutura(int daysOffset)
    {
        var data = DateOnly.FromDateTime(DateTime.Today.AddDays(daysOffset));

        var result = CriarColaborador(data, ColaboradorTipo.Atendente, ColaboradorVinculo.CLT);

        if (daysOffset > 0)
        {
            Assert.True(result.IsFailure);
            Assert.Contains(result.Notifications, n => n.Mensagem == "DATA_ADMISSAO_MAIOR_QUE_ATUAL");
        }
        else
        {
            Assert.True(result.IsSuccess);
        }
    }

    [Theory(DisplayName = "Colaborador: tipo ou vínculo inválido -> valida enum inválido")]
    [InlineData(999, 1)]
    [InlineData(1, 999)]
    public void Deve_Falhar_Criacao_Quando_TipoOuVinculoInvalido(int tipoValue, int vinculoValue)
    {
        var result = CriarColaborador(DateOnly.FromDateTime(DateTime.Today.AddYears(-1)), (ColaboradorTipo)tipoValue, (ColaboradorVinculo)vinculoValue);

        Assert.True(result.IsFailure);
        Assert.NotEmpty(result.Notifications);
    }

    [Theory(DisplayName = "Colaborador: criação bem-sucedida com combinações válidas de tipo/vínculo")]
    [InlineData(ColaboradorTipo.Atendente, ColaboradorVinculo.CLT)]
    [InlineData(ColaboradorTipo.Instrutor, ColaboradorVinculo.Estagio)]
    public void Deve_Criar_Com_Sucesso_Quando_DadosValidos(ColaboradorTipo tipo, ColaboradorVinculo vinculo)
    {
        var result = CriarColaborador(DateOnly.FromDateTime(DateTime.Today.AddYears(-1)), tipo, vinculo);

        Assert.True(result.IsSuccess);
        Assert.Equal(tipo, result.Value!.Tipo);
        Assert.Equal(vinculo, result.Value.Vinculo);
        Assert.IsAssignableFrom<Common.IAggregateRoot>(result.Value);
    }
}
