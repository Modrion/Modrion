namespace Modrion.Entities;

public interface IEventDispatcher
{
    object? Invoke(string name, params ReadOnlySpan<object> arguments);
}
