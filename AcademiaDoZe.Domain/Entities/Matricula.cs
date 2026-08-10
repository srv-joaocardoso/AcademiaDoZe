// João Victor Cardoso Gonçalves - 155953 - DEV02

using AcademiaDoZe.Domain.Enums;
using AcademiaDoZe.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace AcademiaDoZe.Domain.Entities;

public abstract class Matricula : Entity
{
    public Aluno AlunoMatricula { get; private set; }
    public MatriculaPlano Plano { get; private set; }
    public DateOnly DataInicio { get; private set; }
    public DateOnly DataFim { get; private set; }
    public string Objetivo { get; private set; }
    public MatriculaRestricoes RestricoesMedicas { get; private set; }
    public string ObservacoesRestricoes { get; private set; }
    public Arquivo? LaudoMedico { get; private set; }
    private Matricula(int id, Aluno alunoMatricula, MatriculaPlano plano,

    DateOnly dataInicio, DateOnly dataFim,
    string objetivo, MatriculaRestricoes restricoesMedicas,
    Arquivo? laudoMedico, string observacoesRestricoes = "") : base(id)

    {
        AlunoMatricula = alunoMatricula;
        Plano = plano;
        DataInicio = dataInicio;
        DataFim = dataFim;
        Objetivo = objetivo;
        RestricoesMedicas = restricoesMedicas;
        LaudoMedico = laudoMedico;
        ObservacoesRestricoes = observacoesRestricoes;
    }
}