using System;

namespace Modrion.Core;

[AttributeUsage(AttributeTargets.Method)]
public class EventAttribute : Attribute
{
    public Enum Event { get; }

    public EventAttribute(object @event)
    {
        if (!(@event is Enum))
            throw new ArgumentException("Must be an enum", nameof(@event));

        Event = (Enum)@event;
    }
}