// João Victor Cardoso Gonçalves - 155953 - DEV02

namespace AcademiaDoZe.Application.Services;

internal static class RepositoryFactoryExtensions
{
    // Cada repositório abre a própria conexão. Sem o Dispose ela fica presa até o GC,
    // e no SQL Server/MySQL o pool (100 conexões) esgota rápido, gerando timeout.
    // Aqui o repositório é criado, usado em uma única operação e liberado logo em seguida.
    public static async Task<TResult> Usar<TRepo, TResult>(this Func<TRepo> factory, Func<TRepo, Task<TResult>> operacao)
    {
        var repo = factory();
        try
        {
            return await operacao(repo);
        }
        finally
        {
            if (repo is IAsyncDisposable asyncDisposable)
                await asyncDisposable.DisposeAsync();
            else if (repo is IDisposable disposable)
                disposable.Dispose();
        }
    }
}
