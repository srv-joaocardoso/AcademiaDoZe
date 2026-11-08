// João Victor Cardoso Gonçalves - 155953 - DEV02

using AcademiaDoZe.Domain.Entities;

namespace AcademiaDoZe.Domain.Tests.Entities;

public class LogradouroTests
{
    [Theory(DisplayName = "Logradouro: nome vazio -> NOME_OBRIGATORIO")]
    [InlineData("")]
    [InlineData(" ")]
    public void Deve_Falhar_Criacao_Quando_NomeVazio(string nome)
    {
        var result = Logradouro.Criar(1, "12345-678", nome, "Bairro", "Cidade", "SP", "Brasil");

        Assert.True(result.IsFailure);
        Assert.Contains(result.Notifications, n => n.Mensagem == "NOME_OBRIGATORIO");
    }

    [Theory(DisplayName = "Logradouro: normaliza estado removendo espaços e upper")]
    [InlineData(" s p ", "SP")]
    [InlineData(" sp ", "SP")]
    public void Deve_Normalizar_Estado_Quando_InputContemEspacos(string inputEstado, string expected)
    {
        var result = Logradouro.Criar(1, "12345-678", "Rua", "Bairro", "Cidade", inputEstado, "Brasil");

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value!.Estado);
    }

    [Theory(DisplayName = "Logradouro: campos obrigatórios vazios -> mensagens específicas")]
    [InlineData("", "Cidade", "SP", "Brasil", "BAIRRO_OBRIGATORIO")]
    [InlineData("Bairro", "", "SP", "Brasil", "CIDADE_OBRIGATORIO")]
    [InlineData("Bairro", "Cidade", "", "Brasil", "ESTADO_OBRIGATORIO")]
    [InlineData("Bairro", "Cidade", "SP", "", "PAIS_OBRIGATORIO")]
    public void Deve_Falhar_Criacao_Quando_CamposObrigatoriosVazios(string bairro, string cidade, string estado, string pais, string expected)
    {
        var result = Logradouro.Criar(1, "12345-678", "Rua Teste", bairro, cidade, estado, pais);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Notifications, n => n.Mensagem == expected);
    }

    [Fact(DisplayName = "Logradouro: criação bem-sucedida com todos os campos válidos")]
    public void Deve_Criar_Com_Sucesso_Quando_TodosCamposValidos()
    {
        var result = Logradouro.Criar(1, "12345-678", "Rua Teste", "Bairro", "Cidade", "SP", "Brasil");

        Assert.True(result.IsSuccess);
        Assert.Equal("12345678", result.Value!.Cep.Valor);
        Assert.IsAssignableFrom<Common.IAggregateRoot>(result.Value);
    }
}
