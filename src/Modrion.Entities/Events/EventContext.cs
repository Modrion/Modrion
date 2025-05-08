namespace Modrion.Entities;

public abstract class EventContext
{
    public abstract string Name { get; }

    public abstract object[] Arguments { get; }

    public abstract IServiceProvider EventServices { get; }
}
