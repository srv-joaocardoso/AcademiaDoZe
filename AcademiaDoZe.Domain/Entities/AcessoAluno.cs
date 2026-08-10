// João Victor Cardoso Gonçalves - 155953 - DEV02

using System;
using System.Collections.Generic;
using System.Text;

namespace AcademiaDoZe.Domain.Entities;

internal class AcessoAluno
{
    public Aluno Aluno { get; private set; }
    public DateTime DataHora { get; private set; }

    private AcessoAluno(int id, Aluno aluno, DateTime dataHora) : base(id)
    {
        Aluno = aluno;
        DataHora = dataHora;
    }
}

