// João Victor Cardoso Gonçalves - 155953 - DEV02

using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Application.Interfaces;
using AcademiaDoZe.Presentation.AppMaui.Helpers;
using CommunityToolkit.Mvvm.Input;

namespace AcademiaDoZe.Presentation.AppMaui.ViewModels;

[QueryProperty(nameof(AlunoId), "Id")]
public partial class AlunoViewModel : BaseViewModel
{
    private readonly IAlunoService _alunoService;
    private readonly ILogradouroService _logradouroService;

    private AlunoDto _aluno = CriarAlunoPadrao();
    public AlunoDto Aluno
    {
        get => _aluno;
        set => SetProperty(ref _aluno, value);
    }

    private int _alunoId;
    public int AlunoId
    {
        get => _alunoId;
        set => SetProperty(ref _alunoId, value);
    }

    private bool _isEditMode;
    public bool IsEditMode
    {
        get => _isEditMode;
        set => SetProperty(ref _isEditMode, value);
    }

    private string _confirmarSenha = string.Empty;
    public string ConfirmarSenha
    {
        get => _confirmarSenha;
        set => SetProperty(ref _confirmarSenha, value);
    }

    public bool HasEnderecoVinculado => Aluno?.Endereco != null && Aluno.Endereco.Id > 0;
    public bool HasNoEnderecoVinculado => !HasEnderecoVinculado;

    public AlunoViewModel(IAlunoService alunoService, ILogradouroService logradouroService)
    {
        _alunoService = alunoService;
        _logradouroService = logradouroService;
        Title = "Detalhes do Aluno";
    }

    public async Task InitializeAsync()
    {
        ConfirmarSenha = string.Empty;

        if (AlunoId > 0)
        {
            IsEditMode = true;
            Title = "Editar Aluno";
            await LoadAlunoAsync();
        }
        else
        {
            IsEditMode = false;
            Title = "Novo Aluno";
            Aluno = CriarAlunoPadrao();
            NotificarStatusEndereco();
        }
    }

    private void NotificarStatusEndereco()
    {
        OnPropertyChanged(nameof(HasEnderecoVinculado));
        OnPropertyChanged(nameof(HasNoEnderecoVinculado));
    }

    private static LogradouroDto EnderecoVazio() => new()
    {
        Cep = string.Empty,
        Nome = string.Empty,
        Bairro = string.Empty,
        Cidade = string.Empty,
        Estado = string.Empty,
        Pais = "Brasil"
    };

    private static AlunoDto CriarAlunoPadrao() => new()
    {
        Nome = string.Empty,
        Cpf = string.Empty,
        DataNascimento = DateOnly.FromDateTime(DateTime.Today.AddYears(-18)),
        Telefone = string.Empty,
        Email = string.Empty,
        Senha = string.Empty,
        Endereco = EnderecoVazio(),
        Numero = string.Empty,
        Complemento = string.Empty
    };

    [RelayCommand]
    private async Task CancelAsync()
    {
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task LoadAlunoAsync()
    {
        if (AlunoId <= 0) return;

        try
        {
            IsBusy = true;

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var alunoData = await _alunoService.ObterPorIdAsync(AlunoId, cts.Token);
            if (alunoData != null)
            {
                // garante que o endereço não seja nulo para evitar erros de bind
                alunoData.Endereco ??= EnderecoVazio();

                // a senha não é retornada por segurança; fica em branco para manter a atual
                alunoData.Senha = string.Empty;
                ConfirmarSenha = string.Empty;

                Aluno = alunoData;
                NotificarStatusEndereco();
            }
        }
        catch (OperationCanceledException)
        {
            await Shell.Current.DisplayAlertAsync("Tempo Esgotado", "O carregamento do aluno expirou. Verifique a conexão.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao carregar aluno: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SearchByCpfAsync()
    {
        var apenasDigitos = CadastroHelper.ApenasDigitos(Aluno.Cpf);
        if (apenasDigitos.Length != 11)
        {
            await Shell.Current.DisplayAlertAsync("Validação", "O CPF deve conter exatamente 11 dígitos numéricos.", "OK");
            return;
        }

        try
        {
            IsBusy = true;

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var alunoData = await _alunoService.ObterPorCpfAsync(apenasDigitos, cts.Token);
            if (alunoData != null)
            {
                // a busca por CPF não traz o endereço; recarrega pelo Id, que vem completo
                AlunoId = alunoData.Id;
                IsEditMode = true;
                Title = "Editar Aluno";
                IsBusy = false;
                await LoadAlunoAsync();

                await Shell.Current.DisplayAlertAsync("Aviso", "Aluno já cadastrado! Dados carregados para edição.", "OK");
            }
            else
            {
                await Shell.Current.DisplayAlertAsync("Aviso", "CPF não encontrado. Pode seguir com o cadastro.", "OK");
            }
        }
        catch (OperationCanceledException)
        {
            await Shell.Current.DisplayAlertAsync("Tempo Esgotado", "A busca por CPF expirou. Verifique a conexão.", "OK");
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
    private async Task SearchByCepAsync()
    {
        var apenasDigitos = CadastroHelper.ApenasDigitos(Aluno.Endereco?.Cep);
        if (apenasDigitos.Length != 8)
        {
            await Shell.Current.DisplayAlertAsync("Validação", "O CEP deve conter exatamente 8 dígitos numéricos.", "OK");
            return;
        }

        try
        {
            IsBusy = true;

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var logradouroData = await _logradouroService.ObterPorCepAsync(apenasDigitos, cts.Token);
            if (logradouroData != null)
            {
                Aluno.Endereco = logradouroData;
                OnPropertyChanged(nameof(Aluno));
                NotificarStatusEndereco();
            }
            else
            {
                await Shell.Current.DisplayAlertAsync("Aviso", "CEP não encontrado no cadastro de logradouros. Cadastre o logradouro primeiro.", "OK");
            }
        }
        catch (OperationCanceledException)
        {
            await Shell.Current.DisplayAlertAsync("Tempo Esgotado", "A busca de CEP expirou. Verifique a conexão com o banco.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao buscar CEP: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SelecionarFotoAsync()
    {
        try
        {
            var foto = await CadastroHelper.SelecionarFotoAsync();
            if (foto != null)
            {
                Aluno.Foto = foto;
                OnPropertyChanged(nameof(Aluno));
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao selecionar imagem: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private async Task SaveAlunoAsync()
    {
        if (IsBusy) return;

        if (!await ValidateAlunoAsync(Aluno, IsEditMode, ConfirmarSenha)) return;

        try
        {
            IsBusy = true;

            // o hash da senha (Argon2) leva um tempo, por isso um prazo maior que nas buscas
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            // endereço digitado sem clicar em Buscar CEP: tenta vincular pelo CEP
            if (!HasEnderecoVinculado)
            {
                var cepLimpo = CadastroHelper.ApenasDigitos(Aluno.Endereco?.Cep);
                var logradouroData = await _logradouroService.ObterPorCepAsync(cepLimpo, cts.Token);
                if (logradouroData == null)
                {
                    await Shell.Current.DisplayAlertAsync("Endereço Obrigatório", "O CEP informado não está cadastrado como Logradouro. Cadastre-o previamente.", "OK");
                    return;
                }

                Aluno.Endereco = logradouroData;
            }

            // normaliza os dados antes de persistir
            Aluno.Nome = Aluno.Nome.Trim();
            Aluno.Cpf = CadastroHelper.ApenasDigitos(Aluno.Cpf);
            Aluno.Telefone = CadastroHelper.ApenasDigitos(Aluno.Telefone);
            Aluno.Numero = Aluno.Numero.Trim();
            Aluno.Complemento = string.IsNullOrWhiteSpace(Aluno.Complemento) ? null : Aluno.Complemento.Trim();
            Aluno.Email = Aluno.Email?.Trim();
            Aluno.Senha = string.IsNullOrWhiteSpace(Aluno.Senha) ? null : Aluno.Senha.Trim();

            if (IsEditMode)
            {
                await _alunoService.AtualizarAsync(Aluno, cts.Token);
                await Shell.Current.DisplayAlertAsync("Sucesso", "Aluno atualizado com sucesso!", "OK");
            }
            else
            {
                await _alunoService.AdicionarAsync(Aluno, cts.Token);
                await Shell.Current.DisplayAlertAsync("Sucesso", "Aluno criado com sucesso!", "OK");
            }

            await Shell.Current.GoToAsync("..");
        }
        catch (OperationCanceledException)
        {
            await Shell.Current.DisplayAlertAsync("Tempo Esgotado", "A gravação do aluno expirou. Verifique a conexão.", "OK");
        }
        catch (InvalidOperationException ex)
        {
            await Shell.Current.DisplayAlertAsync("Regra de Negócio", ex.Message, "OK");
        }
        catch (ArgumentException ex)
        {
            await Shell.Current.DisplayAlertAsync("Validação", ex.Message, "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao salvar aluno: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    // Notification Pattern: junta todos os erros e mostra de uma vez
    private static async Task<bool> ValidateAlunoAsync(AlunoDto aluno, bool isEditMode, string confirmarSenha)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(aluno.Nome))
            errors.Add("• Nome é obrigatório.");

        if (CadastroHelper.ApenasDigitos(aluno.Cpf).Length != 11)
            errors.Add("• CPF deve conter exatamente 11 dígitos numéricos.");

        if (aluno.DataNascimento > DateOnly.FromDateTime(DateTime.Today.AddYears(-12)))
            errors.Add("• O aluno deve ter no mínimo 12 anos de idade.");

        if (CadastroHelper.ApenasDigitos(aluno.Telefone).Length != 11)
            errors.Add("• Telefone deve conter 11 dígitos numéricos com DDD.");

        if (string.IsNullOrWhiteSpace(aluno.Email))
            errors.Add("• E-mail é obrigatório.");
        else if (!CadastroHelper.EmailValido(aluno.Email.Trim()))
            errors.Add("• Formato de e-mail inválido.");

        // senha: obrigatória no cadastro; na edição só valida se foi preenchida
        if (!isEditMode && string.IsNullOrWhiteSpace(aluno.Senha))
        {
            errors.Add("• Senha é obrigatória no cadastro.");
        }
        else if (!string.IsNullOrWhiteSpace(aluno.Senha))
        {
            var senhaLimpa = aluno.Senha.Trim();
            if (senhaLimpa.Length < 6 || !senhaLimpa.Any(char.IsUpper))
                errors.Add("• A senha deve ter no mínimo 6 caracteres e pelo menos uma letra maiúscula.");

            if (aluno.Senha != confirmarSenha)
                errors.Add("• A confirmação de senha não confere com a senha informada.");
        }

        if (CadastroHelper.ApenasDigitos(aluno.Endereco?.Cep).Length != 8)
            errors.Add("• CEP do endereço é obrigatório (8 dígitos).");

        if (string.IsNullOrWhiteSpace(aluno.Numero))
            errors.Add("• Número do endereço é obrigatório.");

        if (errors.Count > 0)
        {
            var mensagem = "Por favor, corrija os seguintes campos:\n\n" + string.Join("\n", errors);
            await Shell.Current.DisplayAlertAsync("Erros de Validação", mensagem, "OK");
            return false;
        }

        return true;
    }
}
