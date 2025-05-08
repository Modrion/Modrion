using HarmonyLib;
using System.Reflection;

namespace Modrion.Core;

public class ModrionEnvironment
{
    public Assembly EntryAssembly { get; }

    public Harmony Harmony { get; }

    public ModrionEnvironment(Assembly entryAssembly, Harmony harmony)
    {
        EntryAssembly = entryAssembly;
        Harmony = harmony;
    }
}
