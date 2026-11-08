// João Victor Cardoso Gonçalves - 155953 - DEV02

namespace AcademiaDoZe.Presentation.AppMaui;

// Application conflita com o nome da nossa camada de aplicação
// Incluir o namespace completo, Microsoft.Maui.Controls.Application, para evitar conflito
public partial class App : Microsoft.Maui.Controls.Application
{
    public App()
    {
        InitializeComponent();

        // tema Dracula é escuro, então forço o modo escuro independente do Windows
        UserAppTheme = AppTheme.Dark;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell())
        {
            Title = "Academia do Zé",
            MinimumWidth = 480,
            MinimumHeight = 640
        };
    }
}
