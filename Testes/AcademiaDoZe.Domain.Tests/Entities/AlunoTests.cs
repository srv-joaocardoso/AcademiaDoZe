// João Victor Cardoso Gonçalves - 155953 - DEV02

using AcademiaDoZe.Domain.Entities;
using AcademiaDoZe.Domain.ValueObjects;

namespace AcademiaDoZe.Domain.Tests.Entities;

public class AlunoTests
{
    private static Logradouro GetValidLogradouro() => Logradouro.Criar(1, "12345-678", "Rua Teste", "Bairro", "Cidade", "SP", "Brasil").Value!;
    private static Arquivo GetValidArquivo() => Arquivo.Criar([1, 2, 3]).Value!;

    private static AcademiaDoZe.Domain.Common.Result<Aluno> CriarAluno(
        string nome = "João da Silva",
        string cpf = "529.982.247-25",
        DateOnly? dataNascimento = null,
        string telefone = "(11) 91234-5678",
        string email = "user@example.com",
        Logradouro? logradouro = null,
        string senha = "Abcdef")
    {
        return Aluno.Criar(
            1,
            nome,
            cpf,
            dataNascimento ?? DateOnly.FromDateTime(DateTime.Today.AddYears(-25)),
            telefone,
            email,
            logradouro ?? GetValidLogradouro(),
            "123",
            "",
            senha,
            GetValidArquivo()
        );
    }

    [Theory(DisplayName = "Aluno: criação bem-sucedida com nomes válidos (trim aplicado)")]
    [InlineData(" João da Silva ")]
    [InlineData("Maria")]
    public void Deve_Criar_Com_Sucesso_Quando_NomeValido(string nome)
    {
        var result = CriarAluno(nome: nome);

        Assert.True(result.IsSuccess);
        Assert.Equal(nome.Trim(), result.Value!.Nome);
        Assert.IsAssignableFrom<Common.IAggregateRoot>(result.Value);
    }

    [Theory(DisplayName = "Aluno: nome vazio -> NOME_OBRIGATORIO")]
    [InlineData("")]
    [InlineData(" ")]
    public void Deve_Falhar_Criacao_Quando_NomeVazio(string nome)
    {
        var result = CriarAluno(nome: nome);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Notifications, n => n.Mensagem == "NOME_OBRIGATORIO");
    }

    [Theory(DisplayName = "Aluno: data nascimento -> obrigatoriedade e idade mínima")]
    [InlineData("default", "DATA_NASCIMENTO_OBRIGATORIO")]
    [InlineData("under12", "DATA_NASCIMENTO_MINIMA_INVALIDA")]
    public void Deve_Falhar_Criacao_Quando_DataNascimentoInvalida(string cenario, string esperado)
    {
        var data = cenario == "default" ? default : DateOnly.FromDateTime(DateTime.Today.AddYears(-10));

        var result = CriarAluno(dataNascimento: data);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Notifications, n => n.Mensagem == esperado);
    }

    [Theory(DisplayName = "Aluno: cpf com dígitos inválidos -> CPF_DIGITOS")]
    [InlineData("123")]
    [InlineData("abc")]
    public void Deve_Falhar_Criacao_Quando_CpfInvalido(string cpf)
    {
        var result = CriarAluno(cpf: cpf);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Notifications, n => n.Mensagem == "CPF_DIGITOS");
    }

    [Theory(DisplayName = "Aluno: telefone com dígitos inválidos -> TELEFONE_DIGITOS")]
    [InlineData("123")]
    [InlineData("999")]
    public void Deve_Falhar_Criacao_Quando_TelefoneInvalido(string telefone)
    {
        var result = CriarAluno(telefone: telefone);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Notifications, n => n.Mensagem == "TELEFONE_DIGITOS");
    }

    [Theory(DisplayName = "Aluno: email com formato inválido -> EMAIL_FORMATO")]
    [InlineData("semarroba")]
    [InlineData("a@")]
    public void Deve_Falhar_Criacao_Quando_EmailInvalido(string email)
    {
        var result = CriarAluno(email: email);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Notifications, n => n.Mensagem == "EMAIL_FORMATO");
    }

    [Theory(DisplayName = "Aluno: senha inválida -> SENHA_FORMATO")]
    [InlineData("abc")]
    [InlineData("semmaiuscula")]
    public void Deve_Falhar_Criacao_Quando_SenhaInvalida(string senha)
    {
        var result = CriarAluno(senha: senha);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Notifications, n => n.Mensagem == "SENHA_FORMATO");
    }

    [Fact(DisplayName = "Aluno: logradouro nulo -> LOGRADOURO_OBRIGATORIO")]
    public void Deve_Falhar_Criacao_Quando_LogradouroNulo()
    {
        var result = Aluno.Criar(
            1,
            "João da Silva",
            "529.982.247-25",
            DateOnly.FromDateTime(DateTime.Today.AddYears(-25)),
            "(11) 91234-5678",
            "user@example.com",
            null!,
            "123",
            "",
            "Abcdef",
            GetValidArquivo()
        );

        Assert.True(result.IsFailure);
        Assert.Contains(result.Notifications, n => n.Mensagem == "LOGRADOURO_OBRIGATORIO");
    }
}
