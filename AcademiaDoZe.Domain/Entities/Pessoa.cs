// João Victor Cardoso Gonçalves - 155953 - DEV02

using AcademiaDoZe.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace AcademiaDoZe.Domain.Entities;

public abstract class Pessoa : Entity
{
    public string Nome { get; protected set; }
    public Cpf Cpf { get; protected set; }
    public DateOnly DataNascimento { get; protected set; }
    public Email Email { get; protected set; }
    public Endereco Endereco { get; protected set; }
    public Senha Senha { get; protected set; }
    public Arquivo Foto { get; protected set; }
    protected Pessoa(
        int id, 
        string nome, 
        Cpf cpf, 
        DateOnly dataNascimento, 
        Telefone telefone, 
        Email email, 
        Endereco endereco, 
        Senha senha, 
        Arquivo foto
    ) : base(id)
    {
        Nome = nome;
        Cpf = cpf;
        DataNascimento = dataNascimento;
        Email = email;
        Endereco = endereco;
        Senha = senha;
        Foto = foto;
    }
}