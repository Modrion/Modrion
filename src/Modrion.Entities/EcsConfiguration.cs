namespace Modrion.Entities;

public sealed class EcsConfiguration
{
    private EcsConfiguration()
    {

    }

    internal static EcsConfiguration Create(Action<EcsConfiguration>? configure)
    {
        var result = new EcsConfiguration();

        configure?.Invoke(result);

        return result;
    }
}