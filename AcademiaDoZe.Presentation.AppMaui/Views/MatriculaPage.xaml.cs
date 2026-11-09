// João Victor Cardoso Gonçalves - 155953 - DEV02

using AcademiaDoZe.Application.Enums;
using AcademiaDoZe.Presentation.AppMaui.ViewModels;

namespace AcademiaDoZe.Presentation.AppMaui.Views;

public partial class MatriculaPage : ContentPage
{
    public MatriculaPage(MatriculaViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is MatriculaViewModel viewModel)
        {
            await viewModel.InitializeAsync();
            viewModel.RecalcularDataFim();
        }

        // assina os eventos só depois de carregar, para não disparar durante a inicialização dos controles
        PlanoPicker.SelectedIndexChanged += OnPlanoOuInicioAlterado;
        InicioDatePicker.DateSelected += OnPlanoOuInicioAlterado;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        PlanoPicker.SelectedIndexChanged -= OnPlanoOuInicioAlterado;
        InicioDatePicker.DateSelected -= OnPlanoOuInicioAlterado;
    }

    // a data fim é sempre calculada a partir do plano e do início
    private void OnPlanoOuInicioAlterado(object? sender, EventArgs e)
    {
        if (BindingContext is not MatriculaViewModel viewModel) return;

        // lê direto dos controles, sem depender da ordem em que o binding atualiza o DTO
        if (PlanoPicker.SelectedItem is AppMatriculaPlano plano)
            viewModel.Matricula.Plano = plano;

        if (InicioDatePicker.Date is DateTime inicio)
            viewModel.Matricula.DataInicio = DateOnly.FromDateTime(inicio);

        viewModel.RecalcularDataFim();
    }
}
