using System;

namespace Modrion.Entities;

public interface ISystemRegistry
{
    ReadOnlyMemory<ISystem> Get(Type type);

    ReadOnlyMemory<ISystem> Get<TSystem>() where TSystem : ISystem;

    ReadOnlyMemory<Type> GetSystemTypes();

    void Register(Action handler);
}
