using Microsoft.Extensions.DependencyInjection;

namespace Mapify.NET; 
public interface INamedMapify {
    string Name { get; }

    IMapify Mapper { get; }
}

internal sealed class NamedMapify(string name, IMapify mapper) : INamedMapify {
    public string Name { get; } = name;

    public IMapify Mapper { get; } = mapper;
}

internal sealed class MapifyProfileTypeRegistration(string? mapperName, Type profileType) {
    public string? MapperName { get; } = mapperName;

    public Type ProfileType { get; } = profileType;
}

internal sealed class NamedMapifyRegistration(string name, ServiceLifetime lifecycle) {
    public string Name { get; } = name;

    public ServiceLifetime Lifecycle { get; } = lifecycle;
}