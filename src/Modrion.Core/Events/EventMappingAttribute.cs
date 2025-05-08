using System;

namespace Modrion.Core;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class EventMappingAttribute : Attribute
{
    public string? TargetClass { get; set; }

    public string? TargetMethod { get; set; }

    public HarmonyEventHookType HookType { get; set; } = HarmonyEventHookType.Postfix;
}

