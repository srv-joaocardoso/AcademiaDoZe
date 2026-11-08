// João Victor Cardoso Gonçalves - 155953 - DEV02

using AcademiaDoZe.Domain.Entities;
using AcademiaDoZe.Domain.Exceptions;

namespace AcademiaDoZe.Domain.Tests.Entities;

public class EntityTests
{
    [Fact(DisplayName = "Entity: id negativo lança DomainException")]
    public void Deve_Lancar_DomainException_Quando_IdNegativo()
    {
        Assert.Throws<DomainException>(() => Logradouro.Criar(-1, "12345-678", "Rua Teste", "Bairro", "Cidade", "SP", "Brasil"));
    }

    [Theory(DisplayName = "Entity: aceita id zero ou positivo")]
    [InlineData(0)]
    [InlineData(5)]
    public void Deve_Criar_Com_IdValido(int id)
    {
        var result = Logradouro.Criar(id, "12345-678", "Rua Teste", "Bairro", "Cidade", "SP", "Brasil");

        Assert.True(result.IsSuccess);
        Assert.Equal(id, result.Value!.Id);
    }
}
