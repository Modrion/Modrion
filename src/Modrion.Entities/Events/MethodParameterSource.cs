using System.Reflection;

namespace Modrion.Entities;

public class MethodParameterSource
{
    public MethodParameterSource(ParameterInfo info)
    {
        Info = info;
    }

    public ParameterInfo Info { get; }

    public int ParameterIndex { get; set; } = -1;

    public bool IsService { get; set; }

    public bool IsComponent { get; set; }
}
