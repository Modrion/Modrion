using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using Modrion.Core;
using Modrion.Entities.Utilities;
using System.Reflection;

namespace Modrion.Entities;

internal class HarmonyEventPatcher : IHarmonyEventPatcher
{
    private static IServiceProvider? _services;
    private static readonly Dictionary<string, Enum> _methodToEvent = new();
    private readonly Harmony _harmony;

    public HarmonyEventPatcher(IServiceProvider services)
    {
        _services = services;
        _harmony = _services.GetRequiredService<ModrionEnvironment>().Harmony;
    }

    public void Initialize()
    {
        Logger.Debug("Initialize HarmonyEventPatcher ...");

        var eventTypes = ScanMappedEvents();

        foreach (var (enumValue, mapping) in eventTypes)
        {
            var type = AccessTools.TypeByName(mapping.TargetClass);
            if (type == null) continue;

            var method = AccessTools.Method(type, mapping.TargetMethod);
            if (method == null) continue;

            var key = $"{type.FullName}.{mapping.TargetMethod}";
            _methodToEvent[key] = enumValue;

            var postfix = new HarmonyMethod(typeof(HarmonyEventPatcher).GetMethod(nameof(Hook), BindingFlags.NonPublic | BindingFlags.Static));
            var prefix = new HarmonyMethod(typeof(HarmonyEventPatcher).GetMethod(nameof(Hook), BindingFlags.NonPublic | BindingFlags.Static));

            switch (mapping.HookType)
            {
                case HarmonyEventHookType.Prefix:
                    _harmony.Patch(method, prefix: prefix);
                    Logger.Debug($"[HarmonyPatch:Prefix] {enumValue} -> {mapping.TargetClass}.{mapping.TargetMethod}");
                    break;

                case HarmonyEventHookType.Postfix:
                default:
                    _harmony.Patch(method, postfix: postfix);
                    Logger.Debug($"[HarmonyPatch:Postfix] {enumValue}-> {mapping.TargetClass}.{mapping.TargetMethod}");
                    break;
            }
        }
    }
    private static void Hook(MethodBase __originalMethod, object __instance)
    {
        var eventDispatcher = _services?.GetRequiredService<IEventDispatcher>();

        if (__originalMethod == null)
        {
            Logger.Debug("[EventDispatch] __originalMethod is null!");
            return;
        }

        var declaringType = __originalMethod.DeclaringType;
        if (declaringType == null)
        {
            return;
        }

        var key = $"{declaringType.FullName}.{__originalMethod.Name}";

        if (_methodToEvent.TryGetValue(key, out var evt))
        {
            Logger.Debug($"[EventDispatch] Raised: {evt}");
            eventDispatcher?.Invoke($"{evt.GetType().Name}.{evt}", __instance);
        }
        else
        {
            Logger.Debug($"[EventDispatch] No event registered for: {key}");
        }
    }

    private IEnumerable<(Enum enumValue, EventMappingAttribute mapping)> ScanMappedEvents()
    {
        return AssemblyScanner.Create()
                .IncludeModAssemblies()
                .ScanEnums()
                .SelectMany(enumType =>
                    Enum.GetValues(enumType).Cast<Enum>()
                        .Select(e =>
                        {
                            var member = enumType.GetMember(e.ToString())[0];
                            var attr = member.GetCustomAttribute<EventMappingAttribute>();
                            return attr != null ? (e, attr) : default;
                        })
                        .Where(result => result != default))
                .Select(r => (r.e, r.attr!));
    }
}