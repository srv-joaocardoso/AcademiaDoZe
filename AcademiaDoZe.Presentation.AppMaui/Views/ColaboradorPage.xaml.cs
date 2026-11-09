// João Victor Cardoso Gonçalves - 155953 - DEV02

using AcademiaDoZe.Presentation.AppMaui.ViewModels;

namespace AcademiaDoZe.Presentation.AppMaui.Views;

public partial class ColaboradorPage : ContentPage
{
    public ColaboradorPage(ColaboradorViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is ColaboradorViewModel viewModel)
        {
            await viewModel.InitializeAsync();
        }
    }
}
