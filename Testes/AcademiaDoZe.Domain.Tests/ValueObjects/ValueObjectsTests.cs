// João Victor Cardoso Gonçalves - 155953 - DEV02

using AcademiaDoZe.Domain.Entities;
using AcademiaDoZe.Domain.ValueObjects;

namespace AcademiaDoZe.Domain.Tests.ValueObjects;

public class ValueObjectsTests
{
    [Theory(DisplayName = "Cep: dígitos inválidos -> CEP_DIGITOS")]
    [InlineData("123")]
    [InlineData("12-345")]
    public void Deve_Falhar_Criacao_Quando_CepDigitosInvalidos(string input)
    {
        var result = Cep.Criar(input);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Notifications, n => n.Mensagem == "CEP_DIGITOS");
    }

    [Theory(DisplayName = "Cep: formatos válidos (com e sem hífen)")]
    [InlineData("12345-678")]
    [InlineData("12345678")]
    public void Deve_Criar_Cep_Quando_Valido(string input)
    {
        var result = Cep.Criar(input);

        Assert.True(result.IsSuccess);
        Assert.Equal("12345678", result.Value!.Valor);
    }

    [Theory(DisplayName = "Cep: obrigatório -> CEP_OBRIGATORIO")]
    [InlineData(null)]
    [InlineData("")]
    public void Deve_Falhar_Criacao_Quando_CepNuloOuVazio(string? input)
    {
        var result = Cep.Criar(input!);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Notifications, n => n.Mensagem == "CEP_OBRIGATORIO");
    }

    private static Logradouro GetValidLogradouro() => Logradouro.Criar(1, "12345-678", "Rua Teste", "Bairro", "Cidade", "SP", "Brasil").Value!;

    [Theory(DisplayName = "Endereco: criação válida com número e complemento")]
    [InlineData("10", "Bloco A")]
    [InlineData("1", "")]
    public void Deve_Criar_Endereco_Quando_Valido(string numero, string complemento)
    {
        var logradouro = GetValidLogradouro();
        var result = Endereco.Criar(logradouro, numero, complemento);

        Assert.True(result.IsSuccess);
        Assert.Equal(logradouro.Id, result.Value!.LogradouroId);
        Assert.Equal(numero, result.Value.Numero);
        Assert.Equal(complemento, result.Value.Complemento);
    }

    [Theory(DisplayName = "Endereco: valida obrigatoriedade do logradouro e número")]
    [InlineData(null, "1", "LOGRADOURO_OBRIGATORIO")]
    [InlineData("valid", "", "NUMERO_OBRIGATORIO")]
    public void Deve_Falhar_Criacao_Quando_EnderecoInvalido(string? logradouroCase, string numero, string expected)
    {
        Logradouro? logradouro = logradouroCase == "valid" ? GetValidLogradouro() : null;

        var result = Endereco.Criar(logradouro!, numero, "");

        Assert.True(result.IsFailure);
        Assert.Contains(result.Notifications, n => n.Mensagem == expected);
    }

    [Theory(DisplayName = "Cpf: nulo/vazio/espaços -> CPF_OBRIGATORIO")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Deve_Falhar_Criacao_Quando_CpfNuloOuVazio(string? input)
    {
        var result = Cpf.Criar(input!);

        Assert.True(result.IsFailure);
        Assert.Single(result.Notifications);
        Assert.Equal("CPF_OBRIGATORIO", result.Notifications.First().Mensagem);
    }

    [Theory(DisplayName = "Cpf: formatos válidos (com e sem pontuação)")]
    [InlineData("529.982.247-25")]
    [InlineData("52998224725")]
    public void Deve_Criar_Cpf_Quando_ValorValido(string input)
    {
        var result = Cpf.Criar(input);

        Assert.True(result.IsSuccess);
        Assert.Equal("52998224725", result.Value!.Valor);
    }

    [Theory(DisplayName = "Cpf: sem dígitos suficientes -> CPF_DIGITOS")]
    [InlineData(" dfgdf ")]
    [InlineData("abc")]
    public void Deve_Falhar_Criacao_Quando_CpfSemDigitos(string input)
    {
        var result = Cpf.Criar(input);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Notifications, n => n.Mensagem == "CPF_DIGITOS");
    }

    [Theory(DisplayName = "Telefone: dígitos inválidos -> TELEFONE_DIGITOS")]
    [InlineData("1234")]
    [InlineData("(1)2345")]
    public void Deve_Falhar_Criacao_Quando_TelefoneDigitosInvalidos(string input)
    {
        var result = Telefone.Criar(input);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Notifications, n => n.Mensagem == "TELEFONE_DIGITOS");
    }

    [Theory(DisplayName = "Telefone: formatos válidos (com e sem formatação)")]
    [InlineData("(11) 91234-5678")]
    [InlineData("11912345678")]
    public void Deve_Criar_Telefone_Quando_Valido(string input)
    {
        var result = Telefone.Criar(input);

        Assert.True(result.IsSuccess);
        Assert.Equal("11912345678", result.Value!.Valor);
    }

    [Theory(DisplayName = "Telefone: obrigatório -> TELEFONE_OBRIGATORIO")]
    [InlineData(null)]
    [InlineData("")]
    public void Deve_Falhar_Criacao_Quando_TelefoneNuloOuVazio(string? input)
    {
        var result = Telefone.Criar(input!);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Notifications, n => n.Mensagem == "TELEFONE_OBRIGATORIO");
    }

    [Theory(DisplayName = "Senha: valida requisito de uppercase")]
    [InlineData("abcdef", false)]
    [InlineData("Abcdef", true)]
    public void Deve_Validar_RequisitoUppercase_Senha(string senha, bool isSuccess)
    {
        var result = Senha.Criar(senha);

        Assert.Equal(isSuccess, result.IsSuccess);
    }

    [Theory(DisplayName = "Senha: obrigatório -> SENHA_OBRIGATORIO")]
    [InlineData(null)]
    [InlineData("")]
    public void Deve_Falhar_Criacao_Quando_SenhaNulaOuVazia(string? input)
    {
        var result = Senha.Criar(input!);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Notifications, n => n.Mensagem == "SENHA_OBRIGATORIO");
    }

    [Theory(DisplayName = "Senha: menos de 6 caracteres -> SENHA_FORMATO")]
    [InlineData("Ab1")]
    [InlineData("Abc")]
    public void Deve_Falhar_Criacao_Quando_SenhaCurta(string input)
    {
        var result = Senha.Criar(input);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Notifications, n => n.Mensagem == "SENHA_FORMATO");
    }

    [Theory(DisplayName = "Email: formatos válidos, com remoção de espaços")]
    [InlineData("user@example.com", "user@example.com")]
    [InlineData(" user@example.com ", "user@example.com")]
    public void Deve_Criar_Email_Quando_Valido(string input, string expected)
    {
        var result = Email.Criar(input);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value!.Valor);
    }

    [Theory(DisplayName = "Email: formato inválido -> EMAIL_FORMATO")]
    [InlineData("invalido")]
    [InlineData("a@b")]
    [InlineData("@dominio.com")]
    [InlineData("a@.com")]
    public void Deve_Falhar_Criacao_Quando_FormatoEmailInvalido(string input)
    {
        var result = Email.Criar(input);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Notifications, n => n.Mensagem == "EMAIL_FORMATO");
    }

    [Theory(DisplayName = "Email: nulo/vazio -> EMAIL_FORMATO")]
    [InlineData(null)]
    [InlineData("")]
    public void Deve_Falhar_Criacao_Quando_EmailNuloOuVazio(string? input)
    {
        var result = Email.Criar(input!);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Notifications, n => n.Mensagem == "EMAIL_FORMATO");
    }

    [Theory(DisplayName = "Arquivo: criação válida")]
    [InlineData(true)]
    [InlineData(false)]
    public void Deve_Criar_Arquivo_Quando_Valido(bool vazio)
    {
        var conteudo = vazio ? [] : new byte[] { 1, 2, 3 };
        var result = Arquivo.Criar(conteudo);

        Assert.True(result.IsSuccess);
        Assert.Equal(conteudo, result.Value!.Conteudo);
    }

    [Theory(DisplayName = "Arquivo: nulo ou tamanho excedido -> falha")]
    [InlineData(true)]
    [InlineData(false)]
    public void Deve_Falhar_Criacao_Quando_ArquivoInvalido(bool nulo)
    {
        byte[]? conteudo = nulo ? null : new byte[16 * 1024 * 1024];
        var result = Arquivo.Criar(conteudo!);

        Assert.True(result.IsFailure);
        var esperado = nulo ? "ARQUIVO_OBRIGATORIO" : "ARQUIVO_TIPO_TAMANHO";
        Assert.Contains(result.Notifications, n => n.Mensagem == esperado);
    }
}
