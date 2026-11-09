// João Victor Cardoso Gonçalves - 155953 - DEV02

using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Application.Enums;
using AcademiaDoZe.Application.Interfaces;
using AcademiaDoZe.Presentation.AppMaui.Helpers;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace AcademiaDoZe.Presentation.AppMaui.ViewModels;

[QueryProperty(nameof(MatriculaId), "Id")]
public partial class MatriculaViewModel : BaseViewModel
{
    public IEnumerable<AppMatriculaPlano> PlanoTipos { get; } = Enum.GetValues<AppMatriculaPlano>();

    // itens para a seleção múltipla das restrições médicas (enum [Flags])
    public ObservableCollection<RestricaoSelectable> RestricoesMulti { get; } = [];

    // evita que a sincronização vinda do DTO dispare o recálculo dos flags
    private bool _suspendSelectionSync;

    private readonly IMatriculaService _matriculaService;
    private readonly IAlunoService _alunoService;

    private MatriculaDto _matricula = CriarMatriculaPadrao();
    public MatriculaDto Matricula
    {
        get => _matricula;
        set
        {
            if (SetProperty(ref _matricula, value))
            {
                SyncSelectionFromFlags();
                OnPropertyChanged(nameof(DataFim));
            }
        }
    }

    // CPF digitado para buscar o aluno (fica separado do aluno já vinculado)
    private string _cpfBusca = string.Empty;
    public string CpfBusca
    {
        get => _cpfBusca;
        set => SetProperty(ref _cpfBusca, value);
    }

    private int _matriculaId;
    public int MatriculaId
    {
        get => _matriculaId;
        set => SetProperty(ref _matriculaId, value);
    }

    private bool _isEditMode;
    public bool IsEditMode
    {
        get => _isEditMode;
        set => SetProperty(ref _isEditMode, value);
    }

    // data fim exibida na tela, sempre calculada a partir do plano e do início
    public DateOnly DataFim => Matricula.DataFim;

    public bool HasAlunoVinculado => Matricula.AlunoMatricula != null && Matricula.AlunoMatricula.Id > 0;
    public bool HasNoAlunoVinculado => !HasAlunoVinculado;
    public bool IsMenor16 => HasAlunoVinculado && Matricula.AlunoMatricula.DataNascimento > DateOnly.FromDateTime(DateTime.Today.AddYears(-16));
    public bool HasRestricoes => Matricula.RestricoesMedicas != AppMatriculaRestricoes.None;
    public bool ExigeLaudo => IsMenor16 || HasRestricoes;
    public bool HasLaudo => Matricula.LaudoMedico?.Conteudo != null && Matricula.LaudoMedico.Conteudo.Length > 0;
    public bool LaudoPendente => ExigeLaudo && !HasLaudo;
    public bool LaudoOpcional => !ExigeLaudo && !HasLaudo;

    public MatriculaViewModel(IMatriculaService matriculaService, IAlunoService alunoService)
    {
        _matriculaService = matriculaService;
        _alunoService = alunoService;
        Title = "Detalhes da Matrícula";

        BuildRestricoesMulti();
    }

    private static MatriculaDto CriarMatriculaPadrao()
    {
        var inicio = DateOnly.FromDateTime(DateTime.Today);
        return new MatriculaDto
        {
            AlunoMatricula = new AlunoDto
            {
                Nome = string.Empty,
                Cpf = string.Empty,
                DataNascimento = DateOnly.FromDateTime(DateTime.Today.AddYears(-18)),
                Telefone = string.Empty,
                Numero = string.Empty
            },
            Plano = AppMatriculaPlano.Mensal,
            DataInicio = inicio,
            DataFim = CalcularDataFim(AppMatriculaPlano.Mensal, inicio),
            Objetivo = string.Empty,
            RestricoesMedicas = AppMatriculaRestricoes.None,
            ObservacoesRestricoes = string.Empty,
            LaudoMedico = null
        };
    }

    public async Task InitializeAsync()
    {
        CpfBusca = string.Empty;

        if (MatriculaId > 0)
        {
            IsEditMode = true;
            Title = "Editar Matrícula";
            await LoadMatriculaAsync();
        }
        else
        {
            IsEditMode = false;
            Title = "Nova Matrícula";
            Matricula = CriarMatriculaPadrao();
        }

        NotificarPropriedadesVisuais();
    }

    public void NotificarPropriedadesVisuais()
    {
        OnPropertyChanged(nameof(HasAlunoVinculado));
        OnPropertyChanged(nameof(HasNoAlunoVinculado));
        OnPropertyChanged(nameof(IsMenor16));
        OnPropertyChanged(nameof(HasRestricoes));
        OnPropertyChanged(nameof(ExigeLaudo));
        OnPropertyChanged(nameof(HasLaudo));
        OnPropertyChanged(nameof(LaudoPendente));
        OnPropertyChanged(nameof(LaudoOpcional));
    }

    #region data fim

    // mesma regra do domínio (Matricula.Criar)
    public static DateOnly CalcularDataFim(AppMatriculaPlano plano, DateOnly inicio) => plano switch
    {
        AppMatriculaPlano.Mensal => inicio.AddMonths(1),
        AppMatriculaPlano.Trimestral => inicio.AddMonths(3),
        AppMatriculaPlano.Semestral => inicio.AddMonths(6),
        AppMatriculaPlano.Anual => inicio.AddMonths(12),
        _ => inicio
    };

    // chamado pela página quando o plano ou a data de início mudam
    public void RecalcularDataFim()
    {
        Matricula.DataFim = CalcularDataFim(Matricula.Plano, Matricula.DataInicio);
        OnPropertyChanged(nameof(DataFim));
    }

    #endregion

    #region enum Flags <-> seleção múltipla (CheckBoxes)

    // monta a lista de checkboxes a partir do enum (ignora None)
    private void BuildRestricoesMulti()
    {
        RestricoesMulti.Clear();
        foreach (var value in Enum.GetValues<AppMatriculaRestricoes>())
        {
            if (value == AppMatriculaRestricoes.None) continue;

            var item = new RestricaoSelectable
            {
                Value = value,
                IsSelected = Matricula.RestricoesMedicas.HasFlag(value)
            };
            item.PropertyChanged += RestricaoItem_PropertyChanged;
            RestricoesMulti.Add(item);
        }
    }

    // atualiza o DTO quando o usuário marca/desmarca
    private void RestricaoItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(RestricaoSelectable.IsSelected) && !_suspendSelectionSync)
        {
            RecomputeRestricoesFlags();
        }
    }

    // recalcula o bitmask a partir das seleções
    private void RecomputeRestricoesFlags()
    {
        var flags = AppMatriculaRestricoes.None;
        foreach (var item in RestricoesMulti)
        {
            if (item.IsSelected) flags |= item.Value;
        }

        Matricula.RestricoesMedicas = flags;
        NotificarPropriedadesVisuais();
    }

    // sincroniza os checkboxes a partir do DTO (ao carregar ou trocar a matrícula)
    private void SyncSelectionFromFlags()
    {
        _suspendSelectionSync = true;
        try
        {
            foreach (var item in RestricoesMulti)
            {
                item.IsSelected = Matricula.RestricoesMedicas.HasFlag(item.Value);
            }
        }
        finally
        {
            _suspendSelectionSync = false;
        }

        NotificarPropriedadesVisuais();
    }

    #endregion

    [RelayCommand]
    private async Task CancelAsync()
    {
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task LoadMatriculaAsync()
    {
        if (MatriculaId <= 0) return;

        try
        {
            IsBusy = true;

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var matriculaData = await _matriculaService.ObterPorIdAsync(MatriculaId, cts.Token);
            if (matriculaData != null)
            {
                Matricula = matriculaData;
                CpfBusca = matriculaData.AlunoMatricula.Cpf;
            }
        }
        catch (OperationCanceledException)
        {
            await Shell.Current.DisplayAlertAsync("Tempo Esgotado", "O carregamento da matrícula expirou. Verifique a conexão.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao carregar matrícula: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SearchByCpfAsync()
    {
        if (IsEditMode)
        {
            await Shell.Current.DisplayAlertAsync("Aviso", "O aluno de uma matrícula já cadastrada não pode ser trocado.", "OK");
            return;
        }

        var cpfLimpo = CadastroHelper.ApenasDigitos(CpfBusca);
        if (cpfLimpo.Length != 11)
        {
            await Shell.Current.DisplayAlertAsync("Validação", "O CPF deve conter exatamente 11 dígitos numéricos.", "OK");
            return;
        }

        try
        {
            IsBusy = true;

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var alunoData = await _alunoService.ObterPorCpfAsync(cpfLimpo, cts.Token);

            if (alunoData == null)
            {
                await Shell.Current.DisplayAlertAsync("Aviso", "Aluno não encontrado.", "OK");
                return;
            }

            // regra: não permitir nova matrícula se ainda tiver matrícula ativa
            if (await _matriculaService.PossuiMatriculaAtivaAsync(alunoData.Id, cts.Token))
            {
                await Shell.Current.DisplayAlertAsync("Matrícula Ativa", $"{alunoData.Nome} já possui uma matrícula ativa. Não é possível criar outra.", "OK");
                return;
            }

            Matricula.AlunoMatricula = alunoData;
            OnPropertyChanged(nameof(Matricula));
            NotificarPropriedadesVisuais();
        }
        catch (OperationCanceledException)
        {
            await Shell.Current.DisplayAlertAsync("Tempo Esgotado", "A busca do aluno expirou. Verifique a conexão.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao buscar CPF: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SelecionarLaudoAsync()
    {
        try
        {
            var laudo = await CadastroHelper.SelecionarFotoAsync();
            if (laudo != null)
            {
                Matricula.LaudoMedico = laudo;
                OnPropertyChanged(nameof(Matricula));
                NotificarPropriedadesVisuais();
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao selecionar imagem: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private void RemoverLaudo()
    {
        Matricula.LaudoMedico = null;
        OnPropertyChanged(nameof(Matricula));
        NotificarPropriedadesVisuais();
    }

    [RelayCommand]
    private async Task SaveMatriculaAsync()
    {
        if (IsBusy) return;

        // garante que o DTO está com os flags e a data fim corretos
        RecomputeRestricoesFlags();
        RecalcularDataFim();

        if (!await ValidateMatriculaAsync()) return;

        try
        {
            IsBusy = true;

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            Matricula.Objetivo = Matricula.Objetivo.Trim();
            Matricula.ObservacoesRestricoes = Matricula.ObservacoesRestricoes?.Trim();

            if (IsEditMode)
            {
                await _matriculaService.AtualizarAsync(Matricula, cts.Token);
                await Shell.Current.DisplayAlertAsync("Sucesso", "Matrícula atualizada com sucesso!", "OK");
            }
            else
            {
                await _matriculaService.AdicionarAsync(Matricula, cts.Token);
                await Shell.Current.DisplayAlertAsync("Sucesso", "Matrícula criada com sucesso!", "OK");
            }

            await Shell.Current.GoToAsync("..");
        }
        catch (OperationCanceledException)
        {
            await Shell.Current.DisplayAlertAsync("Tempo Esgotado", "A gravação da matrícula expirou. Verifique a conexão.", "OK");
        }
        catch (InvalidOperationException ex)
        {
            await Shell.Current.DisplayAlertAsync("Regra de Negócio", ex.Message, "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao salvar matrícula: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    // Notification Pattern: junta todos os erros e mostra de uma vez
    private async Task<bool> ValidateMatriculaAsync()
    {
        var errors = new List<string>();

        if (!HasAlunoVinculado)
            errors.Add("• Aluno é obrigatório. Informe o CPF e clique em Buscar.");

        if (string.IsNullOrWhiteSpace(Matricula.Objetivo))
            errors.Add("• Objetivo da matrícula é obrigatório.");

        if (!Enum.IsDefined(Matricula.Plano))
            errors.Add("• Plano da matrícula é inválido.");

        if (IsMenor16 && !HasLaudo)
            errors.Add("• Alunos menores de 16 anos devem obrigatoriamente apresentar laudo médico.");

        if (HasRestricoes && !HasLaudo)
            errors.Add("• Alunos com restrições médicas devem obrigatoriamente apresentar laudo médico.");

        if (errors.Count > 0)
        {
            var mensagem = "Por favor, corrija os seguintes campos:\n\n" + string.Join("\n", errors);
            await Shell.Current.DisplayAlertAsync("Erros de Validação", mensagem, "OK");
            return false;
        }

        return true;
    }
}
