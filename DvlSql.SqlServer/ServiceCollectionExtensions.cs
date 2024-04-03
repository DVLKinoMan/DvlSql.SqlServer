using DvlSql;
using DvlSql.SqlServer;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDvlSqlMS(this IServiceCollection services, DvlSqlOptions options)
    {
        services.AddSingleton<IDvlSql>(provider =>
        {
            return new DvlSqlMs(options.ConnectionString);
        });
        return services;
    }
}

public class DvlSqlOptions
{
    public string ConnectionString { get; set; }

    public DvlSqlOptions(string connectionString)
    {
        ConnectionString = connectionString;
    }
}
