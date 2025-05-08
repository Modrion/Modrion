using System.Reflection;

namespace Modrion.Entities.Utilities;

public sealed class AssemblyScanner
{
    private List<Assembly> _assemblies = [];
    private List<Type> _classAttributes = [];
    private List<Type> _classImplements = [];
    private List<Type> _classTypes = [];
    private bool _includeNonPublicMembers;
    private List<Type> _memberAttributes = [];
    private bool _includeAbstract;

    private BindingFlags MemberBindingFlags =>
        BindingFlags.Instance |
        BindingFlags.Public |
        (_includeNonPublicMembers ? BindingFlags.NonPublic : BindingFlags.Default);

    private AssemblyScanner()
    {
    }

    public static AssemblyScanner Create()
    {
        return new AssemblyScanner();
    }

    public AssemblyScanner IncludeAssembly(Assembly assembly)
    {
        if (_assemblies.Contains(assembly))
        {
            return this;
        }

        var result = Clone();
        result._assemblies.Add(assembly);
        return result;
    }

    public AssemblyScanner IncludeModAssemblies()
    {
        // TODO: Path if BepInEx or MelonLoader

        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a =>
                !a.IsDynamic &&
                !string.IsNullOrEmpty(a.Location) &&
                a.Location.IndexOf(@"\Mods\", StringComparison.OrdinalIgnoreCase) >= 0)
            .ToList();

        var result = Clone();
        result._assemblies.AddRange(assemblies);
        return result;
    }

    public AssemblyScanner IncludeTypes(IEnumerable<Type> types)
    {
        var result = Clone();
        result._classTypes.AddRange(types);
        return result;
    }

    public AssemblyScanner IncludeNonPublicMembers()
    {
        var result = Clone();
        result._includeNonPublicMembers = true;
        return result;
    }

    public AssemblyScanner IncludeAbstractClasses()
    {
        var result = Clone();
        result._includeAbstract = true;
        return result;
    }

    public AssemblyScanner Implements<T>()
    {
        var result = Clone();
        result._classImplements.Add(typeof(T));
        return result;
    }

    public AssemblyScanner HasClassAttribute<T>() where T : Attribute
    {
        var result = Clone();
        result._classAttributes.Add(typeof(T));
        return result;
    }

    public AssemblyScanner HasMemberAttribute<T>() where T : Attribute
    {
        var result = Clone();
        result._memberAttributes.Add(typeof(T));
        return result;
    }

    private bool ApplyTypeFilter(Type type)
    {
        return type.IsClass &&
               (_includeAbstract || !type.IsAbstract) &&
               _classImplements.All(i => i.IsAssignableFrom(type)) &&
               _classAttributes.All(a => type.GetCustomAttribute(a) != null);
    }

    private bool ApplyMemberFilter(MemberInfo memberInfo)
    {
        return _memberAttributes.All(a => memberInfo.GetCustomAttribute(a) != null);
    }

    public IEnumerable<Type> ScanEnums()
    {
        return _assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsEnum)
            .Distinct();
    }

    public IEnumerable<Type> ScanTypes()
    {
        return _assemblies.SelectMany(a => a.GetTypes())
            .Concat(_classTypes)
            .Where(ApplyTypeFilter)
            .Distinct();
    }

    public IEnumerable<(Type target, MethodInfo method, TAttribute attribute)> ScanMethods<TAttribute>() where TAttribute : Attribute
    {
        return HasMemberAttribute<TAttribute>()
            .ScanMethods()
            .Select(x => (x.target, x.method, attribute: x.method.GetCustomAttribute<TAttribute>()!));
    }

    public IEnumerable<(Type target, MethodInfo method)> ScanMethods()
    {
        return ScanTypes()
            .SelectMany(t => t.GetMethods(MemberBindingFlags).Select(m => (t, m)))
            .Where(x => ApplyMemberFilter(x.m));
    }

    private AssemblyScanner Clone()
    {
        return new AssemblyScanner
        {
            _assemblies = [.. _assemblies],
            _classTypes = [.. _classTypes],
            _includeNonPublicMembers = _includeNonPublicMembers,
            _classImplements = [.. _classImplements],
            _classAttributes = [.. _classAttributes],
            _memberAttributes = [.. _memberAttributes],
            _includeAbstract = _includeAbstract
        };
    }
}
