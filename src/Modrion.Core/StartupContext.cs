using HarmonyLib;
using System;

namespace Modrion.Core;

public sealed class StartupContext : IStartupContext
{
    public event EventHandler? Initialized;

    private IStartup? _configurator;

    public StartupContext(ModrionInitParams init)
    {
        Harmony = init.Harmony;
    }

    public Harmony Harmony { get; }

    public IStartup Configurator => _configurator ?? throw new InvalidOperationException("The configurator has not been set.");

    public void InitializeUsing(IStartup configurator)
    {
        _configurator = configurator;

        configurator.Initialize(this);
        Initialized?.Invoke(this, EventArgs.Empty);
    }
}