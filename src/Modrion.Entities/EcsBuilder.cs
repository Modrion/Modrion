namespace Modrion.Entities;

public class EcsBuilder : IEcsBuilder
{
    internal EcsBuilder(IServiceProvider services)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
    }

    public IServiceProvider Services { get; }
}