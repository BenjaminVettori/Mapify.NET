using Microsoft.Extensions.DependencyInjection;

namespace Mapify.NET.Tests.NetFx;

public partial class MapifyNetFrameworkEf6ProjectionTests {
    [Fact]
    public void ServiceCollectionExtensions_ShouldRegisterMapperInNetFrameworkProject() {
        var services = new ServiceCollection();
        services.AddMapifyProfiles(typeof(Ef6DiProfile).Assembly);
        services.AddMapify();

        using var provider = services.BuildServiceProvider();
        var mapify = provider.GetRequiredService<IMapify>();

        var mapped = mapify.Map<Ef6DiSource, Ef6DiTarget>(new Ef6DiSource { Value = 5 });

        Assert.Equal(5, mapped.Value);
    }
}
