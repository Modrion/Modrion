using HarmonyLib;
using System;

namespace Modrion.Core;

public interface IStartupContext
{
    IStartup Configurator { get; }

    Harmony Harmony { get; }

    event EventHandler? Initialized;
}
