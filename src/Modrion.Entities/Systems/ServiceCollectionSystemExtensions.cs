using Microsoft.Extensions.DependencyInjection;
using Modrion.Entities.Utilities;

namespace Modrion.Entities;

public static class ServiceCollectionSystemExtensions
{
    public static IServiceCollection AddSystem(this IServiceCollection services, Type type)
    {
        return services
            .AddSingleton(type)
            .AddSingleton(new SystemEntry(type));
    }

    public static IServiceCollection AddSystem<T>(this IServiceCollection services) where T : class, ISystem
    {
        return services.AddSystem(typeof(T));
    }

    public static IServiceCollection AddSystemsInAssembly(this IServiceCollection services)
    {
        var types = AssemblyScanner.Create()
            .IncludeModAssemblies()
            .Implements<ISystem>()
            .ScanTypes();

        foreach (var type in types)
            services.AddSystem(type);

        return services;
    }
}