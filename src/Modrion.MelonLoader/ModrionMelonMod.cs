using MelonLoader;
using Modrion.Core;

namespace Modrion.MelonLoader;

public abstract class ModrionMelonMod<TStartup> : MelonMod
    where TStartup : class, IStartup, new()
{
    private StartupContext? _context;

    public override void OnInitializeMelon()
    {
        var initParams = new ModrionInitParams()
        {
            Harmony = this.HarmonyInstance
        };

        Logger.SetLogger(new MelonLoggerAdapter(this.LoggerInstance));

        _context = new StartupContext(initParams);
        _context.InitializeUsing(new TStartup());
    }
}
