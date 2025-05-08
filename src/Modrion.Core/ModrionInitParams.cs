using HarmonyLib;
using System.Runtime.InteropServices;

namespace Modrion.Core;

[StructLayout(LayoutKind.Sequential)]
public ref struct ModrionInitParams
{
    public Harmony Harmony { get; set; }
}
