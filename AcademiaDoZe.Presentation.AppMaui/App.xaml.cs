// João Victor Cardoso Gonçalves - 155953 - DEV02

using AcademiaDoZe.Presentation.AppMaui.Message;
using CommunityToolkit.Mvvm.Messaging;

namespace AcademiaDoZe.Presentation.AppMaui;

// Application conflita com o nome da nossa camada de aplicação
// Incluir o namespace completo, Microsoft.Maui.Controls.Application, para evitar conflito
public partial class App : Microsoft.Maui.Controls.Application
{
    public App()
    {
        InitializeComponent();

        // aplicar o tema salvo nas preferências
        AplicarTema();

        // toda vez que o usuário alterar o tema, essa mensagem será enviada e o tema atualizado
        WeakReferenceMessenger.Default.Register<TemaPreferencesUpdatedMessage>(this, (r, m) =>
        {
            AplicarTema();
        });
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // barra de título própria, para o texto acompanhar o tema (sem isso fica branco no tema claro)
        var titleBar = new TitleBar
        {
            Title = "Academia do Zé",
            Icon = "academiadoze.png",
            HeightRequest = 40
        };
        titleBar.SetAppThemeColor(TitleBar.BackgroundColorProperty, Color.FromArgb("#EFEDDC"), Color.FromArgb("#21222C"));
        titleBar.SetAppThemeColor(TitleBar.ForegroundColorProperty, Color.FromArgb("#1F1F1F"), Color.FromArgb("#F8F8F2"));

        return new Window(new AppShell())
        {
            Title = "Academia do Zé",
            TitleBar = titleBar,
            MinimumWidth = 480,
            MinimumHeight = 640
        };
    }

    // dark = Dracula, light = Alucard, system = segue o Windows
    // sem preferência salva, abre no Dracula
    private void AplicarTema()
    {
        UserAppTheme = Preferences.Get("Tema", "dark") switch
        {
            "light" => AppTheme.Light,
            "system" => AppTheme.Unspecified,
            _ => AppTheme.Dark,
        };
    }
}
