using Modrion.Core;
using Modrion.Entities.Utilities;
using System.Collections.Concurrent;
using System.Reflection;

namespace Modrion.Entities;

internal class EventDispatcher : IEventDispatcher
{
    private static readonly Type[] _defaultParameterTypes = [
        typeof(string)
    ];

    private readonly Dictionary<string, Event> _events = new();
    private readonly IServiceProvider _serviceProvider;

    public EventDispatcher(IServiceProvider serviceProvider, ISystemRegistry systemRegistry)
    {
        _serviceProvider = serviceProvider;

        systemRegistry.Register(() => LoadTargetSites(systemRegistry));
    }

    public object? Invoke(string name, params ReadOnlySpan<object> arguments)
    {
        Console.WriteLine($"[EventDispatcher] Invoke called with event: {name}");

        if (!_events.TryGetValue(name, out var @event))
        {
            Console.WriteLine($"[EventDispatcher] Event {name} not registered!");
            Console.WriteLine($"[EventDispatcher] Available keys: {string.Join(", ", _events.Keys)}");
            return null;
        }

        if (!@event.Cache.TryGetValue(NullValue.Instance, out var invoke))
        {
            invoke = CreateEventInvoke(@event, null);
            @event.Cache.TryAdd(NullValue.Instance, invoke);
        }

        Console.WriteLine($"[EventDispatcher] Invoke arguments = {arguments.Length}");

        return invoke(arguments);
    }

    /*private void LoadTargetSites(ISystemRegistry systemRegistry)
    {
        var events = AssemblyScanner.Create()
            .IncludeModAssemblies()
            .IncludeTypes(systemRegistry.GetSystemTypes().ToArray())
            .IncludeNonPublicMembers()
            .ScanMethods<EventAttribute>();

        foreach (var (target, method, attribute) in events)
        {
            var name = attribute.Type.ToString() ?? method.Name;

            Console.WriteLine($"[EventDispatcher] Found event method: {method.DeclaringType}.{method.Name} for event {name}");

            var @event = GetOrCreateEvent(name);

            var (paramCount, paramSources) = GetParameterSources(method);

            Console.WriteLine($"paramCount = {paramCount}");
            Console.WriteLine($"paramSources = {paramSources}");

            var targetSite = CreateTargetSite(target, method, paramSources, paramCount);
            @event.TargetSites.Add(targetSite);
        }
    }*/

    private void LoadTargetSites(ISystemRegistry systemRegistry)
    {
        var events = AssemblyScanner.Create()
            .IncludeModAssemblies()
            .IncludeTypes(systemRegistry.GetSystemTypes().ToArray())
            .IncludeNonPublicMembers()
            .ScanMethods<EventAttribute>();

        foreach (var (target, method, attribute) in events)
        {
            var eventEnum = attribute.Event;
            var eventName = $"{eventEnum.GetType().Name}.{eventEnum}"; // ex: "AskaEvent.MainMenuOpened"

            Console.WriteLine($"[EventDispatcher] Found event method: {method.DeclaringType}.{method.Name} for event {eventName}");

            var @event = GetOrCreateEvent(eventEnum); // idéalement, stocke par Type + valeur enum

            var (paramCount, paramSources) = GetParameterSources(method);

            Console.WriteLine($"paramCount = {paramCount}");
            Console.WriteLine($"paramSources = {paramSources}");

            var targetSite = CreateTargetSite(target, method, paramSources, paramCount);
            @event.TargetSites.Add(targetSite);
        }
    }

    private static (int paramCount, MethodParameterSource[] paramSources) GetParameterSources(MethodInfo method)
    {
        var paramIndex = 0;
        var parameterSources = method.GetParameters()
            .Select(info => new MethodParameterSource(info))
            .ToArray();

        foreach (var source in parameterSources)
        {
            var type = source.Info.ParameterType;

            // Si le paramètre est potentiellement passé via args[] (donc pas besoin de DI)
            if (!type.IsAbstract && !type.IsInterface)
            {
                source.ParameterIndex = paramIndex++;
            }
            else
            {
                // Sinon on essaie de le résoudre via les services
                source.IsService = true;
            }
        }

        return (paramIndex, parameterSources);
    }

    private TargetSiteData CreateTargetSite(Type target, MethodInfo method, MethodParameterSource[] parameterInfos, int _)
    {
        var compiled = MethodInvokerFactory.Compile(method, parameterInfos);
        var targetSiteName = $"{method.DeclaringType?.FullName}.{method.Name}";

        return new TargetSiteData(target, (instance, eventContext) =>
        {
            try
            {
                var args = eventContext.Arguments;

                Console.WriteLine($"[EventDispatcher] Invoking compiled method for {targetSiteName} with {args.Length} argument(s)");
                return compiled.Invoke(instance, args, eventContext.EventServices);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EventDispatcher] Exception in {targetSiteName}:\n{ex}");
                return null;
            }
        });
    }

    /*private Event GetOrCreateEvent(string name)
    {
        if (!_events.TryGetValue(name, out var @event))
        {
            _events[name] = @event = new Event(name);
        }

        return @event;
    }*/

    private Event GetOrCreateEvent(Enum gameEvent)
    {
        var key = $"{gameEvent.GetType().Name}.{gameEvent}";

        if (!_events.TryGetValue(key, out var @event))
        {
            _events[key] = @event = new Event(key);
        }

        return @event;
    }

    private object? InnerInvoke(EventContext context, Event @event, object? defaultResult)
    {
        object? result = null;

        foreach (var targetSite in @event.TargetSites)
        {
            Console.WriteLine($"[EventDispatcher] Trying to resolve target: {targetSite.TargetType.Name}");

            targetSite.Target ??= _serviceProvider.GetService(targetSite.TargetType);

            if (targetSite.Target == null)
            {
                Console.WriteLine($"[EventDispatcher] Target {targetSite.TargetType.Name} not resolved!");
                continue;
            }

            Console.WriteLine($"[EventDispatcher] Target {targetSite.TargetType.Name} resolved, invoking...");

            var targetResult = targetSite.Invoke(targetSite.Target, context);

            Console.WriteLine($"[EventDispatcher] Target Result {targetResult}");

            if (targetResult is null || targetResult == defaultResult)
                continue;

            result = targetResult;
        }

        return result ?? defaultResult;
    }

    private EventInvokeDelegate CreateEventInvoke(Event @event, object? defaultResult)
    {
        var context = new EventContextImpl(@event.Name, _serviceProvider);

        // In order to chain the middleware from first to last, the middleware must be nested from last to first
        EventDelegate invoke = ctx => InnerInvoke(ctx, @event, defaultResult);
        for (var i = @event.Middleware.Count - 1; i >= 0; i--)
        {
            invoke = @event.Middleware[i](invoke);
        }

        return args =>
        {
            try
            {
                context.SetArguments(args);

                var result = invoke(context);
                return result switch
                {
                    Task<bool> task => !task.IsCompleted ? null : (task.Result ? MethodResult.True : MethodResult.False),
                    Task<int> task => !task.IsCompleted ? null : task.Result,
                    Task => null,
                    _ => result
                };
            }
            catch (Exception ex)
            {
                // TODO
                Console.WriteLine(ex.ToString());
                return null;
            }
        };
    }

    private delegate object? EventInvokeDelegate(ReadOnlySpan<object> args);

    private sealed record Event(string Name)
    {
        public List<TargetSiteData> TargetSites { get; } = [];

        public List<Func<EventDelegate, EventDelegate>> Middleware { get; } = [];

        public ConcurrentDictionary<object, EventInvokeDelegate> Cache { get; } = new();

        public void ClearCache()
        {
            Cache.Clear();
        }
    }

    private sealed record NullValue
    {
        public static NullValue Instance { get; } = new();
    }

    private sealed record TargetSiteData(Type TargetType, Func<object, EventContext, object?> Invoke)
    {
        public object? Target { get; set; }
    }
}