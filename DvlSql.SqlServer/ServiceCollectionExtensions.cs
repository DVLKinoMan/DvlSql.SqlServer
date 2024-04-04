using DvlSql;
using DvlSql.SqlServer;
using System;
using Microsoft.Extensions.Options;

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

    public static IServiceCollection AddDvlSqlMS(this IServiceCollection services, Action<DvlSqlOptions> options)
    {
        services.Configure(options);

        services.AddSingleton<IDvlSql>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<DvlSqlOptions>>().Value;

            return new DvlSqlMs(options.ConnectionString);
        });
        return services;
    }
}
