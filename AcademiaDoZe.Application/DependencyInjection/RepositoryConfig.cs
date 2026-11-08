// João Victor Cardoso Gonçalves - 155953 - DEV02

using AcademiaDoZe.Infrastructure.Data;

namespace AcademiaDoZe.Application.DependencyInjection;

public class RepositoryConfig
{
    public required string ConnectionString { get; set; }
    public required DatabaseType DatabaseType { get; set; }
}
