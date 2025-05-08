using Microsoft.Extensions.DependencyInjection;
using Modrion.Core;

namespace Modrion.Entities;

public interface IEcsStartup : IStartup
{
    void Configure(IServiceCollection services);

    void Configure(IEcsBuilder builder);
}