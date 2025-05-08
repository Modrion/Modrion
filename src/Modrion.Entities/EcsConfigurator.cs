using Microsoft.Extensions.DependencyInjection;
using Modrion.Core;
using System.Diagnostics.CodeAnalysis;

namespace Modrion.Entities;

internal class EcsConfigurator
{
    private readonly EcsConfiguration _configuration;
    private IServiceProvider? _serviceProvider;

    public EcsConfigurator(EcsConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void Bind(IStartupContext context)
    {
        context.Initialized += OnContextInitialized;
    }

    private void Run(IStartupContext context)
    {
        var configurator = (IEcsStartup)context.Configurator;
        var environment = new ModrionEnvironment(configurator.GetType().Assembly, context.Harmony);

        // Build the service provider
        BuildServiceProvider(environment, configurator);

        // Configure EcsBuilder
        configurator.Configure(new EcsBuilder(_serviceProvider));

        // Load systems
        LoadSystems();

        // Load harmony patches
        LoadPatches();
    }

    [MemberNotNull(nameof(_serviceProvider))]
    private void BuildServiceProvider(ModrionEnvironment environment, IEcsStartup configurator)
    {
        var services = new ServiceCollection();

        services.AddSingleton(environment);

        ConfigureDefaultServices(services);
        configurator.Configure(services);

        _serviceProvider = services.BuildServiceProvider();
    }

    private void LoadSystems()
    {
        _serviceProvider!.GetRequiredService<SystemRegistry>().LoadSystems();
    }

    private void LoadPatches()
    {
        _serviceProvider!.GetRequiredService<HarmonyEventPatcher>().Initialize();
    }

    private static void ConfigureDefaultServices(IServiceCollection services)
    {
        services
            .AddSingleton<SystemRegistry>()
            .AddSingleton<ISystemRegistry>(x => x.GetRequiredService<SystemRegistry>())
            .AddSingleton<EventDispatcher>()
            .AddSingleton<IEventDispatcher>(sp => sp.GetRequiredService<EventDispatcher>())
            .AddSingleton<HarmonyEventPatcher>()
            .AddSingleton<IHarmonyEventPatcher>(sp => sp.GetRequiredService<HarmonyEventPatcher>());
    }

    private void OnContextInitialized(object? sender, EventArgs e)
    {
        var context = (IStartupContext)sender!;
        Run(context);
    }
}
