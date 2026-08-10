// João Victor Cardoso Gonçalves - 155953 - DEV02

using System;
using System.Collections.Generic;
using System.Text;

namespace AcademiaDoZe.Domain.Entities;

internal class AcessoColaborador : Entity
{
    public Colaborador Colaborador { get; private set; }
    public DateTime DataHora { get; private set; }

    private AcessoColaborador(int id, Colaborador colaborador, DateTime dataHora) : base(id)
    {
        Colaborador = colaborador;
        DataHora = dataHora;
    }
}
