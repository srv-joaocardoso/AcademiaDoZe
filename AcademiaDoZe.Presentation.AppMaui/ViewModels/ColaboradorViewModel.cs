// João Victor Cardoso Gonçalves - 155953 - DEV02

using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Application.Enums;
using AcademiaDoZe.Application.Interfaces;
using AcademiaDoZe.Presentation.AppMaui.Helpers;
using CommunityToolkit.Mvvm.Input;

namespace AcademiaDoZe.Presentation.AppMaui.ViewModels;

[QueryProperty(nameof(ColaboradorId), "Id")]
public partial class ColaboradorViewModel : BaseViewModel
{
    // valores dos enums para os Pickers
    public IEnumerable<AppColaboradorTipo> ColaboradorTipos { get; } = Enum.GetValues<AppColaboradorTipo>();
    public IEnumerable<AppColaboradorVinculo> ColaboradorVinculos { get; } = Enum.GetValues<AppColaboradorVinculo>();

    private readonly IColaboradorService _colaboradorService;
    private readonly ILogradouroService _logradouroService;

    private ColaboradorDto _colaborador = CriarColaboradorPadrao();
    public ColaboradorDto Colaborador
    {
        get => _colaborador;
        set => SetProperty(ref _colaborador, value);
    }

    private int _colaboradorId;
    public int ColaboradorId
    {
        get => _colaboradorId;
        set => SetProperty(ref _colaboradorId, value);
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

    public bool HasEnderecoVinculado => Colaborador?.Endereco != null && Colaborador.Endereco.Id > 0;
    public bool HasNoEnderecoVinculado => !HasEnderecoVinculado;

    public ColaboradorViewModel(IColaboradorService colaboradorService, ILogradouroService logradouroService)
    {
        _colaboradorService = colaboradorService;
        _logradouroService = logradouroService;
        Title = "Detalhes do Colaborador";
    }

    public async Task InitializeAsync()
    {
        ConfirmarSenha = string.Empty;

        if (ColaboradorId > 0)
        {
            IsEditMode = true;
            Title = "Editar Colaborador";
            await LoadColaboradorAsync();
        }
        else
        {
            IsEditMode = false;
            Title = "Novo Colaborador";
            Colaborador = CriarColaboradorPadrao();
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

    private static ColaboradorDto CriarColaboradorPadrao() => new()
    {
        Nome = string.Empty,
        Cpf = string.Empty,
        DataNascimento = DateOnly.FromDateTime(DateTime.Today.AddYears(-18)),
        Telefone = string.Empty,
        Email = string.Empty,
        Senha = string.Empty,
        Endereco = EnderecoVazio(),
        Numero = string.Empty,
        Complemento = string.Empty,
        DataAdmissao = DateOnly.FromDateTime(DateTime.Today),
        Tipo = AppColaboradorTipo.Atendente,
        Vinculo = AppColaboradorVinculo.CLT
    };

    [RelayCommand]
    private async Task CancelAsync()
    {
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task LoadColaboradorAsync()
    {
        if (ColaboradorId <= 0) return;

        try
        {
            IsBusy = true;

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var colaboradorData = await _colaboradorService.ObterPorIdAsync(ColaboradorId, cts.Token);
            if (colaboradorData != null)
            {
                // garante que o endereço não seja nulo para evitar erros de bind
                colaboradorData.Endereco ??= EnderecoVazio();

                // a senha não é retornada por segurança; fica em branco para manter a atual
                colaboradorData.Senha = string.Empty;
                ConfirmarSenha = string.Empty;

                Colaborador = colaboradorData;
                NotificarStatusEndereco();
            }
        }
        catch (OperationCanceledException)
        {
            await Shell.Current.DisplayAlertAsync("Tempo Esgotado", "O carregamento do colaborador expirou. Verifique a conexão.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao carregar colaborador: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SearchByCpfAsync()
    {
        var apenasDigitos = CadastroHelper.ApenasDigitos(Colaborador.Cpf);
        if (apenasDigitos.Length != 11)
        {
            await Shell.Current.DisplayAlertAsync("Validação", "O CPF deve conter exatamente 11 dígitos numéricos.", "OK");
            return;
        }

        try
        {
            IsBusy = true;

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var colaboradorData = await _colaboradorService.ObterPorCpfAsync(apenasDigitos, cts.Token);
            if (colaboradorData != null)
            {
                // a busca por CPF não traz o endereço; recarrega pelo Id, que vem completo
                ColaboradorId = colaboradorData.Id;
                IsEditMode = true;
                Title = "Editar Colaborador";
                IsBusy = false;
                await LoadColaboradorAsync();

                await Shell.Current.DisplayAlertAsync("Aviso", "Colaborador já cadastrado! Dados carregados para edição.", "OK");
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
        var apenasDigitos = CadastroHelper.ApenasDigitos(Colaborador.Endereco?.Cep);
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
                Colaborador.Endereco = logradouroData;
                OnPropertyChanged(nameof(Colaborador));
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
                Colaborador.Foto = foto;
                OnPropertyChanged(nameof(Colaborador));
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao selecionar imagem: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private async Task SaveColaboradorAsync()
    {
        if (IsBusy) return;

        if (!await ValidateColaboradorAsync(Colaborador, IsEditMode, ConfirmarSenha)) return;

        try
        {
            IsBusy = true;

            // o hash da senha (Argon2) leva um tempo, por isso um prazo maior que nas buscas
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            // endereço digitado sem clicar em Buscar CEP: tenta vincular pelo CEP
            if (!HasEnderecoVinculado)
            {
                var cepLimpo = CadastroHelper.ApenasDigitos(Colaborador.Endereco?.Cep);
                var logradouroData = await _logradouroService.ObterPorCepAsync(cepLimpo, cts.Token);
                if (logradouroData == null)
                {
                    await Shell.Current.DisplayAlertAsync("Endereço Obrigatório", "O CEP informado não está cadastrado como Logradouro. Cadastre-o previamente.", "OK");
                    return;
                }

                Colaborador.Endereco = logradouroData;
            }

            // normaliza os dados antes de persistir
            Colaborador.Nome = Colaborador.Nome.Trim();
            Colaborador.Cpf = CadastroHelper.ApenasDigitos(Colaborador.Cpf);
            Colaborador.Telefone = CadastroHelper.ApenasDigitos(Colaborador.Telefone);
            Colaborador.Numero = Colaborador.Numero.Trim();
            Colaborador.Complemento = string.IsNullOrWhiteSpace(Colaborador.Complemento) ? null : Colaborador.Complemento.Trim();
            Colaborador.Email = Colaborador.Email?.Trim();
            Colaborador.Senha = string.IsNullOrWhiteSpace(Colaborador.Senha) ? null : Colaborador.Senha.Trim();

            if (IsEditMode)
            {
                await _colaboradorService.AtualizarAsync(Colaborador, cts.Token);
                await Shell.Current.DisplayAlertAsync("Sucesso", "Colaborador atualizado com sucesso!", "OK");
            }
            else
            {
                await _colaboradorService.AdicionarAsync(Colaborador, cts.Token);
                await Shell.Current.DisplayAlertAsync("Sucesso", "Colaborador criado com sucesso!", "OK");
            }

            await Shell.Current.GoToAsync("..");
        }
        catch (OperationCanceledException)
        {
            await Shell.Current.DisplayAlertAsync("Tempo Esgotado", "A gravação do colaborador expirou. Verifique a conexão.", "OK");
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
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao salvar colaborador: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    // Notification Pattern: junta todos os erros e mostra de uma vez
    private static async Task<bool> ValidateColaboradorAsync(ColaboradorDto colaborador, bool isEditMode, string confirmarSenha)
    {
        var errors = new List<string>();
        var hoje = DateOnly.FromDateTime(DateTime.Today);

        if (string.IsNullOrWhiteSpace(colaborador.Nome))
            errors.Add("• Nome é obrigatório.");

        if (CadastroHelper.ApenasDigitos(colaborador.Cpf).Length != 11)
            errors.Add("• CPF deve conter exatamente 11 dígitos numéricos.");

        if (colaborador.DataNascimento > DateOnly.FromDateTime(DateTime.Today.AddYears(-12)))
            errors.Add("• O colaborador deve ter no mínimo 12 anos de idade.");

        if (CadastroHelper.ApenasDigitos(colaborador.Telefone).Length != 11)
            errors.Add("• Telefone deve conter 11 dígitos numéricos com DDD.");

        if (string.IsNullOrWhiteSpace(colaborador.Email))
            errors.Add("• E-mail é obrigatório.");
        else if (!CadastroHelper.EmailValido(colaborador.Email.Trim()))
            errors.Add("• Formato de e-mail inválido.");

        // senha: obrigatória no cadastro; na edição só valida se foi preenchida
        if (!isEditMode && string.IsNullOrWhiteSpace(colaborador.Senha))
        {
            errors.Add("• Senha é obrigatória no cadastro.");
        }
        else if (!string.IsNullOrWhiteSpace(colaborador.Senha))
        {
            var senhaLimpa = colaborador.Senha.Trim();
            if (senhaLimpa.Length < 6 || !senhaLimpa.Any(char.IsUpper))
                errors.Add("• A senha deve ter no mínimo 6 caracteres e pelo menos uma letra maiúscula.");

            if (colaborador.Senha != confirmarSenha)
                errors.Add("• A confirmação de senha não confere com a senha informada.");
        }

        if (CadastroHelper.ApenasDigitos(colaborador.Endereco?.Cep).Length != 8)
            errors.Add("• CEP do endereço é obrigatório (8 dígitos).");

        if (string.IsNullOrWhiteSpace(colaborador.Numero))
            errors.Add("• Número do endereço é obrigatório.");

        if (colaborador.DataAdmissao > hoje)
            errors.Add("• Data de admissão não pode ser maior que a data atual.");

        // administrador precisa ser CLT
        if (colaborador.Tipo == AppColaboradorTipo.Administrador && colaborador.Vinculo != AppColaboradorVinculo.CLT)
            errors.Add("• Colaborador com tipo Administrador deve ter vínculo CLT.");

        if (errors.Count > 0)
        {
            var mensagem = "Por favor, corrija os seguintes campos:\n\n" + string.Join("\n", errors);
            await Shell.Current.DisplayAlertAsync("Erros de Validação", mensagem, "OK");
            return false;
        }

        return true;
    }
}
