// João Victor Cardoso Gonçalves - 155953 - DEV02

using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Application.Enums;
using AcademiaDoZe.Application.Interfaces;
using AcademiaDoZe.Presentation.AppMaui.Helpers;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AcademiaDoZe.Presentation.AppMaui.ViewModels;

public partial class ColaboradorListViewModel : BaseViewModel
{
    private readonly IColaboradorService _colaboradorService;

    public ObservableCollection<string> FilterTypes { get; } = ["Nome", "CPF", "Id", "Email", "Tipo", "Vínculo"];

    private ObservableCollection<ColaboradorDto> _colaboradores = [];
    public ObservableCollection<ColaboradorDto> Colaboradores
    {
        get => _colaboradores;
        set => SetProperty(ref _colaboradores, value);
    }

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    private string _selectedFilterType = "Nome";
    public string SelectedFilterType
    {
        get => _selectedFilterType;
        set => SetProperty(ref _selectedFilterType, value);
    }

    public ColaboradorListViewModel(IColaboradorService colaboradorService)
    {
        _colaboradorService = colaboradorService;
        Title = "Colaboradores";
    }

    [RelayCommand]
    private async Task AddColaboradorAsync()
    {
        try
        {
            await Shell.Current.GoToAsync("colaborador");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao navegar para tela de cadastro: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private async Task EditColaboradorAsync(ColaboradorDto colaborador)
    {
        try
        {
            if (colaborador == null) return;

            await Shell.Current.GoToAsync($"colaborador?Id={colaborador.Id}");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao navegar para tela de edição: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsRefreshing = true;
        await LoadColaboradoresAsync();
    }

    [RelayCommand]
    private async Task SearchColaboradoresAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;

            await MainThread.InvokeOnMainThreadAsync(() => Colaboradores.Clear());

            IEnumerable<ColaboradorDto> resultados = [];
            var termo = SearchText.Trim();

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            if (string.IsNullOrWhiteSpace(termo))
            {
                resultados = await _colaboradorService.ObterTodosAsync(cts.Token) ?? [];
            }
            else if (SelectedFilterType == "Nome")
            {
                var todos = await _colaboradorService.ObterTodosAsync(cts.Token) ?? [];
                resultados = todos.Where(c => c.Nome.Contains(termo, StringComparison.OrdinalIgnoreCase));
            }
            else if (SelectedFilterType == "CPF")
            {
                var cpfLimpo = CadastroHelper.ApenasDigitos(termo);
                if (cpfLimpo.Length != 11)
                {
                    await Shell.Current.DisplayAlertAsync("Validação", "Para buscar por CPF, informe os 11 dígitos numéricos.", "OK");
                    return;
                }

                var colaborador = await _colaboradorService.ObterPorCpfAsync(cpfLimpo, cts.Token);
                if (colaborador != null)
                    resultados = [colaborador];
            }
            else if (SelectedFilterType == "Id")
            {
                if (!int.TryParse(termo, out int id) || id <= 0)
                {
                    await Shell.Current.DisplayAlertAsync("Validação", "Para buscar por ID, informe um número inteiro positivo válido.", "OK");
                    return;
                }

                var colaborador = await _colaboradorService.ObterPorIdAsync(id, cts.Token);
                if (colaborador != null)
                    resultados = [colaborador];
            }
            else if (SelectedFilterType == "Email")
            {
                var colaborador = await _colaboradorService.ObterPorEmailAsync(termo, cts.Token);
                if (colaborador != null)
                    resultados = [colaborador];
            }
            else if (SelectedFilterType == "Tipo")
            {
                var tipos = Enum.GetValues<AppColaboradorTipo>()
                    .Where(t => t.ToString().Contains(termo, StringComparison.OrdinalIgnoreCase) || t.GetDisplayName().Contains(termo, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (tipos.Count == 0)
                {
                    await Shell.Current.DisplayAlertAsync("Validação", "Informe um tipo válido para filtrar (ex: Administrador, Atendente, Instrutor).", "OK");
                    return;
                }

                var lista = new List<ColaboradorDto>();
                foreach (var t in tipos)
                {
                    lista.AddRange(await _colaboradorService.ObterPorTipoAsync(t, cts.Token) ?? []);
                }
                resultados = lista.DistinctBy(c => c.Id);
            }
            else if (SelectedFilterType == "Vínculo")
            {
                var vinculos = Enum.GetValues<AppColaboradorVinculo>()
                    .Where(v => v.ToString().Contains(termo, StringComparison.OrdinalIgnoreCase) || v.GetDisplayName().Contains(termo, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (vinculos.Count == 0)
                {
                    await Shell.Current.DisplayAlertAsync("Validação", "Informe um vínculo válido para filtrar (ex: CLT, Estágio).", "OK");
                    return;
                }

                var lista = new List<ColaboradorDto>();
                foreach (var v in vinculos)
                {
                    lista.AddRange(await _colaboradorService.ObterPorVinculoAsync(v, cts.Token) ?? []);
                }
                resultados = lista.DistinctBy(c => c.Id);
            }

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                foreach (var item in resultados)
                {
                    Colaboradores.Add(item);
                }
                OnPropertyChanged(nameof(Colaboradores));
            });
        }
        catch (OperationCanceledException)
        {
            await Shell.Current.DisplayAlertAsync("Tempo Esgotado", "A busca de colaboradores expirou. Verifique a conexão com o banco.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao buscar colaboradores: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task LoadColaboradoresAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Colaboradores.Clear();
                OnPropertyChanged(nameof(Colaboradores));
            });

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var colaboradoresList = await _colaboradorService.ObterTodosAsync(cts.Token);

            if (colaboradoresList != null)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    foreach (var colaborador in colaboradoresList)
                    {
                        Colaboradores.Add(colaborador);
                    }
                    OnPropertyChanged(nameof(Colaboradores));
                });
            }
        }
        catch (OperationCanceledException)
        {
            await Shell.Current.DisplayAlertAsync("Tempo Esgotado", "O carregamento dos colaboradores expirou. Verifique a conexão.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao carregar colaboradores: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private async Task DeleteColaboradorAsync(ColaboradorDto colaborador)
    {
        if (colaborador == null) return;

        bool confirm = await Shell.Current.DisplayAlertAsync("Confirmar Exclusão", $"Deseja realmente excluir o colaborador {colaborador.Nome}?", "Sim", "Não");
        if (!confirm) return;

        try
        {
            IsBusy = true;

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            bool success = await _colaboradorService.RemoverAsync(colaborador.Id, cts.Token);

            if (success)
            {
                Colaboradores.Remove(colaborador);
                await Shell.Current.DisplayAlertAsync("Sucesso", "Colaborador excluído com sucesso!", "OK");
            }
            else
            {
                await Shell.Current.DisplayAlertAsync("Erro", "Não foi possível excluir o colaborador.", "OK");
            }
        }
        catch (OperationCanceledException)
        {
            await Shell.Current.DisplayAlertAsync("Tempo Esgotado", "A exclusão do colaborador expirou. Verifique a conexão.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao excluir colaborador: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
