// João Victor Cardoso Gonçalves - 155953 - DEV02

using AcademiaDoZe.Application.Enums;
using AcademiaDoZe.Presentation.AppMaui.Configuration;
using AcademiaDoZe.Presentation.AppMaui.Message;
using CommunityToolkit.Mvvm.Messaging;

namespace AcademiaDoZe.Presentation.AppMaui.Views;

public partial class ConfigPage : ContentPage
{
    public ConfigPage()
    {
        InitializeComponent();
    }

    // recarrega sempre que a página aparece, assim um cancelamento anterior não deixa dados velhos na tela
    protected override void OnAppearing()
    {
        base.OnAppearing();
        CarregarTema();
        CarregarBanco();
    }

    #region Tema

    private void CarregarTema()
    {
        // uso de expressão switch para carregar o índice selecionado
        TemaPicker.SelectedIndex = Preferences.Get("Tema", "dark") switch
        {
            "light" => 1,
            "system" => 2,
            _ => 0,
        };
    }

    private async void OnSalvarTemaClicked(object? sender, EventArgs e)
    {
        string selectedTheme = TemaPicker.SelectedIndex switch
        {
            1 => "light",
            2 => "system",
            _ => "dark"
        };
        Preferences.Set("Tema", selectedTheme);

        // Disparar mensagem para uso na recarga dinâmica
        WeakReferenceMessenger.Default.Send(new TemaPreferencesUpdatedMessage("TemaAlterado"));

        await DisplayAlertAsync("Sucesso", "Tema salvo com sucesso!", "OK");

        // Navegar para dashboard
        await Shell.Current.GoToAsync("//dashboard");
    }

    #endregion

    #region Banco de Dados

    private void CarregarBanco()
    {
        DatabaseTypePicker.Items.Clear();
        foreach (var tipo in Enum.GetValues<AppDatabaseType>())
        {
            DatabaseTypePicker.Items.Add(tipo.ToString());
        }

        var bancoAtual = Preferences.Get("DatabaseType", AppDatabaseType.Sqlite.ToString());
        DatabaseTypePicker.SelectedItem = bancoAtual;

        AtualizarInterfacePorTipoBanco();
    }

    private void OnDatabaseTypeChanged(object? sender, EventArgs? e)
    {
        AtualizarInterfacePorTipoBanco();
    }

    private void AtualizarInterfacePorTipoBanco()
    {
        if (DatabaseTypePicker.SelectedItem is not string selectedTypeStr || !Enum.TryParse<AppDatabaseType>(selectedTypeStr, out var selectedType))
        {
            return;
        }

        bool sqlite = selectedType == AppDatabaseType.Sqlite;
        SqliteInfoCard.IsVisible = sqlite;
        SqliteContainer.IsVisible = sqlite;
        ServidorBancoGrid.IsVisible = !sqlite;
        CredenciaisGrid.IsVisible = !sqlite;

        var complementoPadrao = ConfigurationHelper.PadraoComplemento(selectedType);
        ComplementoEntry.Placeholder = complementoPadrao;

        if (sqlite)
        {
            ComplementoLabel.Text = "Complemento (ex: Default Timeout=5;)";
            SqliteCaminhoEntry.Text = Preferences.Get("Sqlite_Caminho", ConfigurationHelper.SqliteCaminhoPadrao);
            ComplementoEntry.Text = Preferences.Get("Sqlite_Complemento", complementoPadrao);
            return;
        }

        var prefix = selectedType.ToString();
        ComplementoLabel.Text = selectedType == AppDatabaseType.SqlServer
            ? "Complemento (SSL / Timeout / Criptografia)"
            : "Complemento (Timeout / SSL)";
        UsuarioEntry.Placeholder = $"Ex: {ConfigurationHelper.PadraoUsuario(selectedType)}";

        ServidorEntry.Text = Preferences.Get($"{prefix}_Servidor", "localhost");
        BancoEntry.Text = Preferences.Get($"{prefix}_Banco", "db_academia_do_ze");
        UsuarioEntry.Text = Preferences.Get($"{prefix}_Usuario", ConfigurationHelper.PadraoUsuario(selectedType));
        SenhaEntry.Text = Preferences.Get($"{prefix}_Senha", "abcBolinhas12345");
        ComplementoEntry.Text = Preferences.Get($"{prefix}_Complemento", complementoPadrao);
    }

    private async void OnSalvarBdClicked(object? sender, EventArgs e)
    {
        if (DatabaseTypePicker.SelectedItem is not string selectedTypeStr || !Enum.TryParse<AppDatabaseType>(selectedTypeStr, out var selectedType))
        {
            await DisplayAlertAsync("Aviso", "Selecione um tipo de banco de dados válido.", "OK");
            return;
        }

        if (selectedType == AppDatabaseType.Sqlite)
        {
            if (string.IsNullOrWhiteSpace(SqliteCaminhoEntry.Text))
            {
                await DisplayAlertAsync("Validação", "Informe o caminho do arquivo do banco SQLite.", "OK");
                return;
            }

            Preferences.Set("Sqlite_Caminho", SqliteCaminhoEntry.Text.Trim());
            Preferences.Set("Sqlite_Complemento", ComplementoEntry.Text?.Trim() ?? string.Empty);
        }
        else
        {
            var erros = new List<string>();
            if (string.IsNullOrWhiteSpace(ServidorEntry.Text)) erros.Add("• Informe o servidor.");
            if (string.IsNullOrWhiteSpace(BancoEntry.Text)) erros.Add("• Informe o nome do banco.");
            if (string.IsNullOrWhiteSpace(UsuarioEntry.Text)) erros.Add("• Informe o usuário.");

            if (erros.Count > 0)
            {
                await DisplayAlertAsync("Validação", string.Join("\n", erros), "OK");
                return;
            }

            var prefix = selectedType.ToString();
            Preferences.Set($"{prefix}_Servidor", ServidorEntry.Text!.Trim());
            Preferences.Set($"{prefix}_Banco", BancoEntry.Text!.Trim());
            Preferences.Set($"{prefix}_Usuario", UsuarioEntry.Text!.Trim());
            Preferences.Set($"{prefix}_Senha", SenhaEntry.Text ?? string.Empty);
            Preferences.Set($"{prefix}_Complemento", ComplementoEntry.Text?.Trim() ?? string.Empty);
        }

        Preferences.Set("DatabaseType", selectedType.ToString());

        // Disparar a mensagem para recarga dinâmica (processada pelo ConfigurationHelper)
        WeakReferenceMessenger.Default.Send(new BancoPreferencesUpdatedMessage("BancoAlterado"));

        await DisplayAlertAsync("Sucesso", $"Configurações do banco de dados ({selectedType}) salvas com sucesso!", "OK");

        // Navegar para dashboard
        await Shell.Current.GoToAsync("//dashboard");
    }

    #endregion

    private async void OnCancelarClicked(object? sender, EventArgs e)
    {
        // retornar para dashboard
        await Shell.Current.GoToAsync("//dashboard");
    }

    // Ao fechar a página, desinscreve o mensageiro para evitar vazamentos de memória - memory leaks
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }
}
