// João Victor Cardoso Gonçalves - 155953 - DEV02

using AcademiaDoZe.Application.DependencyInjection;
using AcademiaDoZe.Application.Enums;
using AcademiaDoZe.Application.Mappings;
using AcademiaDoZe.Presentation.AppMaui.Message;
using CommunityToolkit.Mvvm.Messaging;

namespace AcademiaDoZe.Presentation.AppMaui.Configuration;

public static class ConfigurationHelper
{
    // caminho padrão do SQLite no Windows, o mesmo usado nos testes de infraestrutura
    public const string SqliteCaminhoPadraoWindows = @"C:\Users\timdf\Pessoal\Faculdade\Desenvolvimento de Sistemas II\AcademiaDoZe\db_academia_do_ze.db";

    public static string SqliteCaminhoPadrao => DeviceInfo.Platform == DevicePlatform.WinUI
        ? SqliteCaminhoPadraoWindows
        : Path.Combine(FileSystem.AppDataDirectory, "db_academia_do_ze.db");

    public static void ConfigureServices(IServiceCollection services)
    {
        var (connectionString, databaseType) = ObterConfiguracaoAtual();

        var repoConfig = new RepositoryConfig
        {
            ConnectionString = connectionString,
            DatabaseType = databaseType.ToInfrastructure()
        };

        // Configura a fábrica de repositórios com a string de conexão e tipo de banco
        services.AddSingleton(repoConfig);

        // Assina mensagens de alteração de banco de dados para atualizar o RepositoryConfig diretamente
        WeakReferenceMessenger.Default.Register<RepositoryConfig, BancoPreferencesUpdatedMessage>(repoConfig, (r, m) =>
        {
            var (novaConnStr, novoDbType) = ObterConfiguracaoAtual();
            r.ConnectionString = novaConnStr;
            r.DatabaseType = novoDbType.ToInfrastructure();
        });

        // Configura os serviços da camada de aplicação
        services.AddApplicationServices();
    }

    // Obtém a Connection String e o AppDatabaseType ativos a partir das Preferences do usuário,
    // com valores padrão para cada um dos 3 gerenciadores (Sqlite, MySql e SqlServer).
    public static (string ConnectionString, AppDatabaseType DatabaseType) ObterConfiguracaoAtual()
    {
        var databaseTypeStr = Preferences.Get("DatabaseType", AppDatabaseType.Sqlite.ToString());
        if (!Enum.TryParse<AppDatabaseType>(databaseTypeStr, out var databaseType))
        {
            databaseType = AppDatabaseType.Sqlite;
        }

        string connectionString;
        if (databaseType == AppDatabaseType.Sqlite)
        {
            var dbPath = Preferences.Get("Sqlite_Caminho", SqliteCaminhoPadrao);
            if (string.IsNullOrWhiteSpace(dbPath))
                dbPath = SqliteCaminhoPadrao;

            var complemento = Preferences.Get("Sqlite_Complemento", PadraoComplemento(AppDatabaseType.Sqlite));
            connectionString = $"Data Source={dbPath};{complemento}";
        }
        else
        {
            var prefix = databaseType.ToString();

            var dbServer = Preferences.Get($"{prefix}_Servidor", "localhost");
            var dbDatabase = Preferences.Get($"{prefix}_Banco", "db_academia_do_ze");
            var dbUser = Preferences.Get($"{prefix}_Usuario", PadraoUsuario(databaseType));
            var dbPassword = Preferences.Get($"{prefix}_Senha", "abcBolinhas12345");
            var dbComplemento = Preferences.Get($"{prefix}_Complemento", PadraoComplemento(databaseType));

            connectionString = $"Server={dbServer};Database={dbDatabase};User Id={dbUser};Password={dbPassword};{dbComplemento}";
        }

        return (connectionString, databaseType);
    }

    public static string PadraoUsuario(AppDatabaseType tipo) => tipo == AppDatabaseType.SqlServer ? "sa" : "root";

    public static string PadraoComplemento(AppDatabaseType tipo) => tipo switch
    {
        AppDatabaseType.SqlServer => "TrustServerCertificate=True;Encrypt=True;Connect Timeout=5;Connection Timeout=5;",
        AppDatabaseType.MySql => "Connection Timeout=5;Default Command Timeout=30;AllowPublicKeyRetrieval=True;SslMode=Disabled;",
        _ => "Default Timeout=5;"
    };
}
