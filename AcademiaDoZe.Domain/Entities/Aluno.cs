// João Victor Cardoso Gonçalves - 155953 - DEV02

using AcademiaDoZe.Domain.Enums;
using AcademiaDoZe.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace AcademiaDoZe.Domain.Entities;

public class Aluno : Pessoa
{
    public Matricula Matricula { get; private set; }
    private Aluno(
        int id,
        string nome,
        Cpf cpf,
        DateOnly dataNascimento,
        Telefone telefone,
        Email email,
        Endereco endereco,
        Senha senha,
        Arquivo foto
    ) : base(id, nome, cpf, dataNascimento, telefone, email, endereco, senha, foto)
    {}
}

