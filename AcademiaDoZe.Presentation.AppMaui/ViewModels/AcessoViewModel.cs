// João Victor Cardoso Gonçalves - 155953 - DEV02

using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Application.Enums;
using AcademiaDoZe.Application.Interfaces;
using AcademiaDoZe.Application.Mappings;
using AcademiaDoZe.Presentation.AppMaui.Helpers;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AcademiaDoZe.Presentation.AppMaui.ViewModels;

// pessoa exibida no seletor da catraca (aluno ou colaborador)
public class PessoaAcessoItem
{
    public int Id { get; init; }
    public required string Nome { get; init; }
    public required string Cpf { get; init; }
    public ArquivoDto? Foto { get; init; }
    public string Detalhe { get; init; } = string.Empty;
}

// linha da lista de acessos registrados hoje
public class AcessoRegistradoItem
{
    public required string Nome { get; init; }
    public required string Descricao { get; init; }
    public required DateTime DataHora { get; init; }
    public ArquivoDto? Foto { get; init; }
    public bool Entrada { get; init; }
    public string Icone => Entrada ? "" : "";
}

public partial class AcessoViewModel : BaseViewModel
{
    private readonly IAcessoAlunoService _acessoAlunoService;
    private readonly IAcessoColaboradorService _acessoColaboradorService;
    private readonly IAlunoService _alunoService;
    private readonly IColaboradorService _colaboradorService;
    private readonly IMatriculaService _matriculaService;

    public ObservableCollection<PessoaAcessoItem> Pessoas { get; } = [];
    public ObservableCollection<AcessoRegistradoItem> AcessosHoje { get; } = [];

    private bool _isModoAluno = true;
    public bool IsModoAluno
    {
        get => _isModoAluno;
        set
        {
            if (SetProperty(ref _isModoAluno, value))
            {
                OnPropertyChanged(nameof(IsModoColaborador));
                OnPropertyChanged(nameof(ModoTexto));
                OnPropertyChanged(nameof(SelecioneTexto));
            }
        }
    }
    public bool IsModoColaborador => !IsModoAluno;
    public string ModoTexto => IsModoAluno ? "Modo atual selecionado: Aluno" : "Modo atual selecionado: Colaborador";
    public string SelecioneTexto => IsModoAluno ? "Selecione o Aluno:" : "Selecione o Colaborador:";

    private string _textoBusca = string.Empty;
    public string TextoBusca
    {
        get => _textoBusca;
        set => SetProperty(ref _textoBusca, value);
    }

    private PessoaAcessoItem? _pessoaSelecionada;
    public PessoaAcessoItem? PessoaSelecionada
    {
        get => _pessoaSelecionada;
        set
        {
            if (SetProperty(ref _pessoaSelecionada, value))
            {
                OnPropertyChanged(nameof(HasPessoa));
                OnPropertyChanged(nameof(HasNoPessoa));
                _ = AtualizarSituacaoAsync();
            }
        }
    }
    public bool HasPessoa => PessoaSelecionada != null;
    public bool HasNoPessoa => PessoaSelecionada == null;

    private bool _isDentro;
    public bool IsDentro
    {
        get => _isDentro;
        set
        {
            if (SetProperty(ref _isDentro, value))
            {
                OnPropertyChanged(nameof(SituacaoTexto));
                OnPropertyChanged(nameof(BotaoTexto));
            }
        }
    }
    public string SituacaoTexto => IsDentro ? "DENTRO DA ACADEMIA" : "FORA DA ACADEMIA";
    public string BotaoTexto => IsDentro ? "Registrar Saída" : "Registrar Entrada";

    // informação extra da pessoa selecionada (plano ou horas do dia)
    private string _infoPessoa = string.Empty;
    public string InfoPessoa
    {
        get => _infoPessoa;
        set => SetProperty(ref _infoPessoa, value);
    }

    private bool _hasResultado;
    public bool HasResultado
    {
        get => _hasResultado;
        set => SetProperty(ref _hasResultado, value);
    }

    private bool _resultadoAutorizado;
    public bool ResultadoAutorizado
    {
        get => _resultadoAutorizado;
        set
        {
            if (SetProperty(ref _resultadoAutorizado, value))
                OnPropertyChanged(nameof(ResultadoNegado));
        }
    }
    public bool ResultadoNegado => !ResultadoAutorizado;

    private string _resultadoTitulo = string.Empty;
    public string ResultadoTitulo
    {
        get => _resultadoTitulo;
        set => SetProperty(ref _resultadoTitulo, value);
    }

    private string _resultadoMensagem = string.Empty;
    public string ResultadoMensagem
    {
        get => _resultadoMensagem;
        set => SetProperty(ref _resultadoMensagem, value);
    }

    private bool _hasNoAcessos = true;
    public bool HasNoAcessos
    {
        get => _hasNoAcessos;
        set => SetProperty(ref _hasNoAcessos, value);
    }

    public AcessoViewModel(IAcessoAlunoService acessoAlunoService, IAcessoColaboradorService acessoColaboradorService,
        IAlunoService alunoService, IColaboradorService colaboradorService, IMatriculaService matriculaService)
    {
        _acessoAlunoService = acessoAlunoService;
        _acessoColaboradorService = acessoColaboradorService;
        _alunoService = alunoService;
        _colaboradorService = colaboradorService;
        _matriculaService = matriculaService;
        Title = "Controle de Acesso";
    }

    public async Task InitializeAsync()
    {
        await BuscarAsync();
        await CarregarAcessosHojeAsync();
    }

    [RelayCommand]
    private async Task SelecionarAlunoAsync()
    {
        if (IsModoAluno) return;
        IsModoAluno = true;
        await TrocarModoAsync();
    }

    [RelayCommand]
    private async Task SelecionarColaboradorAsync()
    {
        if (IsModoColaborador) return;
        IsModoAluno = false;
        await TrocarModoAsync();
    }

    private async Task TrocarModoAsync()
    {
        TextoBusca = string.Empty;
        HasResultado = false;
        await BuscarAsync();
    }

    // filtra por nome ou CPF (só os dígitos)
    [RelayCommand]
    private async Task BuscarAsync()
    {
        try
        {
            IsBusy = true;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            var termo = (TextoBusca ?? string.Empty).Trim();
            var digitos = CadastroHelper.ApenasDigitos(termo);

            bool Combina(string nome, string cpf) =>
                termo.Length == 0
                || nome.Contains(termo, StringComparison.CurrentCultureIgnoreCase)
                || (digitos.Length > 0 && cpf.Contains(digitos));

            List<PessoaAcessoItem> encontrados;
            if (IsModoAluno)
            {
                var alunos = await _alunoService.ObterTodosAsync(cts.Token);
                encontrados = [.. alunos.Where(a => Combina(a.Nome, a.Cpf))
                    .Select(a => new PessoaAcessoItem { Id = a.Id, Nome = a.Nome, Cpf = a.Cpf, Foto = a.Foto, Detalhe = "Aluno" })];
            }
            else
            {
                var colaboradores = await _colaboradorService.ObterTodosAsync(cts.Token);
                encontrados = [.. colaboradores.Where(c => Combina(c.Nome, c.Cpf))
                    .Select(c => new PessoaAcessoItem { Id = c.Id, Nome = c.Nome, Cpf = c.Cpf, Foto = c.Foto, Detalhe = $"Colaborador • {c.Tipo.GetDisplayName()} • {c.Vinculo.GetDisplayName()}" })];
            }

            Pessoas.Clear();
            foreach (var p in encontrados) Pessoas.Add(p);

            // seleciona automaticamente o primeiro resultado
            PessoaSelecionada = Pessoas.FirstOrDefault();
        }
        catch (OperationCanceledException)
        {
            await Shell.Current.DisplayAlertAsync("Tempo Esgotado", "A busca expirou. Verifique a conexão com o banco de dados.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao buscar: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    // dentro/fora da academia e informações do plano ou das horas
    private async Task AtualizarSituacaoAsync()
    {
        var pessoa = PessoaSelecionada;
        if (pessoa == null)
        {
            IsDentro = false;
            InfoPessoa = string.Empty;
            return;
        }

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            if (IsModoAluno)
            {
                IsDentro = await _acessoAlunoService.EstaNaAcademiaAsync(pessoa.Id, cts.Token);
                var matricula = await _matriculaService.ObterMatriculaAtivaPorAlunoAsync(pessoa.Id, cts.Token);
                InfoPessoa = matricula == null
                    ? "Sem matrícula ativa"
                    : $"Plano {matricula.Plano.GetDisplayName()} até {matricula.DataFim:dd/MM/yyyy}";
            }
            else
            {
                IsDentro = await _acessoColaboradorService.EstaNaAcademiaAsync(pessoa.Id, cts.Token);
                var horas = await _acessoColaboradorService.ObterHorasTrabalhadasNoDiaAsync(pessoa.Id, DateOnly.FromDateTime(DateTime.Now), cts.Token);
                InfoPessoa = $"Horas hoje: {horas.ToTempo()}";
            }
        }
        catch (Exception ex)
        {
            InfoPessoa = $"Erro ao consultar situação: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task RegistrarAcessoAsync()
    {
        if (IsBusy) return;

        var pessoa = PessoaSelecionada;
        if (pessoa == null)
        {
            await Shell.Current.DisplayAlertAsync("Validação", IsModoAluno ? "Selecione um aluno." : "Selecione um colaborador.", "OK");
            return;
        }

        try
        {
            IsBusy = true;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var resultado = IsModoAluno
                ? await _acessoAlunoService.RegistrarAcessoAsync(pessoa.Id, cts.Token)
                : await _acessoColaboradorService.RegistrarAcessoAsync(pessoa.Id, cts.Token);

            ResultadoAutorizado = resultado.Autorizado;
            ResultadoTitulo = !resultado.Autorizado ? "Acesso Negado" : resultado.Entrada ? "Entrada Autorizada" : "Saída Registrada";
            ResultadoMensagem = resultado.Mensagem;
            HasResultado = true;
        }
        catch (OperationCanceledException)
        {
            await Shell.Current.DisplayAlertAsync("Tempo Esgotado", "O registro expirou. Verifique a conexão com o banco de dados.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao registrar acesso: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }

        await AtualizarSituacaoAsync();
        await CarregarAcessosHojeAsync();
    }

    [RelayCommand]
    private async Task CarregarAcessosHojeAsync()
    {
        try
        {
            IsRefreshing = true;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var hoje = DateOnly.FromDateTime(DateTime.Now);

            var alunos = (await _acessoAlunoService.ObterAcessosDoDiaAsync(hoje, cts.Token)).ToList();
            var colaboradores = (await _acessoColaboradorService.ObterAcessosDoDiaAsync(hoje, cts.Token)).ToList();

            var itens = new List<AcessoRegistradoItem>();

            // permanência de cada saída = diferença para a entrada anterior da mesma pessoa
            foreach (var grupo in alunos.GroupBy(a => a.AlunoAcesso.Id))
            {
                DateTime? ultimaEntrada = null;
                foreach (var a in grupo.OrderBy(x => x.DataHora).ThenBy(x => x.Id))
                {
                    string descricao = a.Entrada
                        ? "Aluno • Entrada"
                        : $"Aluno • Saída • Permanência: {(a.DataHora - (ultimaEntrada ?? a.DataHora)).ToTempo()}";
                    if (a.Entrada) ultimaEntrada = a.DataHora;
                    itens.Add(new AcessoRegistradoItem { Nome = a.AlunoAcesso.Nome, Descricao = descricao, DataHora = a.DataHora, Foto = a.AlunoAcesso.Foto, Entrada = a.Entrada });
                }
            }

            foreach (var grupo in colaboradores.GroupBy(c => c.ColaboradorAcesso.Id))
            {
                DateTime? ultimaEntrada = null;
                var total = TimeSpan.Zero;
                foreach (var c in grupo.OrderBy(x => x.DataHora).ThenBy(x => x.Id))
                {
                    string descricao;
                    if (c.Entrada)
                    {
                        ultimaEntrada = c.DataHora;
                        descricao = $"Colaborador • Entrada • Horas no dia até o momento: {total.ToTempo()}";
                    }
                    else
                    {
                        total += c.DataHora - (ultimaEntrada ?? c.DataHora);
                        descricao = $"Colaborador • Saída • Total do dia: {total.ToTempo()}";
                    }
                    itens.Add(new AcessoRegistradoItem { Nome = c.ColaboradorAcesso.Nome, Descricao = descricao, DataHora = c.DataHora, Foto = c.ColaboradorAcesso.Foto, Entrada = c.Entrada });
                }
            }

            AcessosHoje.Clear();
            foreach (var item in itens.OrderByDescending(i => i.DataHora))
                AcessosHoje.Add(item);

            HasNoAcessos = AcessosHoje.Count == 0;
        }
        catch (OperationCanceledException)
        {
            await Shell.Current.DisplayAlertAsync("Tempo Esgotado", "O carregamento dos acessos expirou. Verifique a conexão.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao carregar acessos de hoje: {ex.Message}", "OK");
        }
        finally
        {
            IsRefreshing = false;
        }
    }
}
