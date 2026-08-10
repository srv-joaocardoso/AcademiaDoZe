// João Victor Cardoso Gonçalves - 155953 - DEV02

using System;
using System.Collections.Generic;
using System.Text;

namespace AcademiaDoZe.Domain.ValueObjects;

public record Senha
{
    public string Valor { get; }
    private Senha(string valor)
    {
        Valor = valor;
    }
}
