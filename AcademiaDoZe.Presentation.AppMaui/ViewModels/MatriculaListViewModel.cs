// João Victor Cardoso Gonçalves - 155953 - DEV02

using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Application.Interfaces;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AcademiaDoZe.Presentation.AppMaui.ViewModels;

public partial class MatriculaListViewModel : BaseViewModel
{
    private readonly IMatriculaService _matriculaService;

    public ObservableCollection<string> FilterTypes { get; } = ["Nome do Aluno", "Id", "Id Aluno", "Ativas", "Vencendo em dias"];

    private ObservableCollection<MatriculaDto> _matriculas = [];
    public ObservableCollection<MatriculaDto> Matriculas
    {
        get => _matriculas;
        set => SetProperty(ref _matriculas, value);
    }

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    private string _selectedFilterType = "Nome do Aluno";
    public string SelectedFilterType
    {
        get => _selectedFilterType;
        set => SetProperty(ref _selectedFilterType, value);
    }

    public MatriculaListViewModel(IMatriculaService matriculaService)
    {
        _matriculaService = matriculaService;
        Title = "Matrículas";
    }

    [RelayCommand]
    private async Task AddMatriculaAsync()
    {
        try
        {
            await Shell.Current.GoToAsync("matricula");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao navegar para tela de cadastro: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private async Task EditMatriculaAsync(MatriculaDto matricula)
    {
        try
        {
            if (matricula == null) return;

            await Shell.Current.GoToAsync($"matricula?Id={matricula.Id}");
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
        await LoadMatriculasAsync();
    }

    [RelayCommand]
    private async Task SearchMatriculasAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;

            await MainThread.InvokeOnMainThreadAsync(() => Matriculas.Clear());

            IEnumerable<MatriculaDto> resultados = [];
            var termo = SearchText.Trim();

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            if (SelectedFilterType == "Ativas")
            {
                // com um número no campo, filtra as ativas daquele aluno
                int.TryParse(termo, out int alunoId);
                resultados = await _matriculaService.ObterAtivasAsync(alunoId, cts.Token) ?? [];
            }
            else if (string.IsNullOrWhiteSpace(termo))
            {
                resultados = await _matriculaService.ObterTodasAsync(cts.Token) ?? [];
            }
            else if (SelectedFilterType == "Nome do Aluno")
            {
                var todas = await _matriculaService.ObterTodasAsync(cts.Token) ?? [];
                resultados = todas.Where(m => m.AlunoMatricula.Nome.Contains(termo, StringComparison.OrdinalIgnoreCase));
            }
            else if (SelectedFilterType == "Id")
            {
                if (!int.TryParse(termo, out int id) || id <= 0)
                {
                    await Shell.Current.DisplayAlertAsync("Validação", "Para buscar por ID, informe um número inteiro positivo válido.", "OK");
                    return;
                }

                var matricula = await _matriculaService.ObterPorIdAsync(id, cts.Token);
                if (matricula != null)
                    resultados = [matricula];
            }
            else if (SelectedFilterType == "Id Aluno")
            {
                if (!int.TryParse(termo, out int alunoId) || alunoId <= 0)
                {
                    await Shell.Current.DisplayAlertAsync("Validação", "Para buscar por ID do aluno, informe um número inteiro positivo válido.", "OK");
                    return;
                }

                resultados = await _matriculaService.ObterPorAlunoIdAsync(alunoId, cts.Token) ?? [];
            }
            else if (SelectedFilterType == "Vencendo em dias")
            {
                if (!int.TryParse(termo, out int dias) || dias < 0)
                {
                    await Shell.Current.DisplayAlertAsync("Validação", "Informe a quantidade de dias (número inteiro).", "OK");
                    return;
                }

                resultados = await _matriculaService.ObterVencendoEmDiasAsync(dias, cts.Token) ?? [];
            }

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                foreach (var item in resultados)
                {
                    Matriculas.Add(item);
                }
                OnPropertyChanged(nameof(Matriculas));
            });
        }
        catch (OperationCanceledException)
        {
            await Shell.Current.DisplayAlertAsync("Tempo Esgotado", "A busca de matrículas expirou. Verifique a conexão com o banco.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao buscar matrículas: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task LoadMatriculasAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Matriculas.Clear();
                OnPropertyChanged(nameof(Matriculas));
            });

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var matriculasList = await _matriculaService.ObterTodasAsync(cts.Token);

            if (matriculasList != null)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    foreach (var matricula in matriculasList)
                    {
                        Matriculas.Add(matricula);
                    }
                    OnPropertyChanged(nameof(Matriculas));
                });
            }
        }
        catch (OperationCanceledException)
        {
            await Shell.Current.DisplayAlertAsync("Tempo Esgotado", "O carregamento das matrículas expirou. Verifique a conexão.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao carregar matrículas: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private async Task DeleteMatriculaAsync(MatriculaDto matricula)
    {
        if (matricula == null) return;

        bool confirm = await Shell.Current.DisplayAlertAsync("Confirmar Exclusão", $"Deseja realmente excluir a matrícula {matricula.Id} de {matricula.AlunoMatricula.Nome}?", "Sim", "Não");
        if (!confirm) return;

        try
        {
            IsBusy = true;

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            bool success = await _matriculaService.RemoverAsync(matricula.Id, cts.Token);

            if (success)
            {
                Matriculas.Remove(matricula);
                await Shell.Current.DisplayAlertAsync("Sucesso", "Matrícula excluída com sucesso!", "OK");
            }
            else
            {
                await Shell.Current.DisplayAlertAsync("Erro", "Não foi possível excluir a matrícula.", "OK");
            }
        }
        catch (OperationCanceledException)
        {
            await Shell.Current.DisplayAlertAsync("Tempo Esgotado", "A exclusão da matrícula expirou. Verifique a conexão.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao excluir matrícula: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
