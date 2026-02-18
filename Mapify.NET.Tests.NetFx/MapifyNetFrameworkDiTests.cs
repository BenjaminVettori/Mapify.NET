using Microsoft.Extensions.DependencyInjection;

namespace Mapify.NET.Tests.NetFx;

public class MapifyNetFrameworkDiTests : IClassFixture<MapifyNetFrameworkDiTests.MapifyNetFrameworkDiFixture> {
    private readonly MapifyNetFrameworkDiFixture _fixture;

    public MapifyNetFrameworkDiTests(MapifyNetFrameworkDiFixture fixture) {
        _fixture = fixture;
    }

    [Fact]
    public void AddMapify_ShouldResolveIMapifyAndApplyProfiles() {
        var mapper = _fixture.Provider.GetRequiredService<IMapify>();
        var result = mapper.Map<Source, Target>(new Source { Value = 123 });

        Assert.Equal(123, result.Value);
    }

    [Fact]
    public void AddMapify_ShouldUseSameMapperInstanceFromSingletonDi() {
        var mapper = _fixture.Provider.GetRequiredService<IMapify>();
        var result = mapper.Map<Source, Target>(new Source { Value = 7 });

        Assert.Equal(7, result.Value);

        var mapper2 = _fixture.Provider.GetRequiredService<IMapify>();
        Assert.Same(mapper, mapper2);
    }

    private class Source {
        public int Value { get; set; }
    }

    private class Target {
        public int Value { get; set; }
    }

    private class NetFxProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<Source, Target>();
        }
    }

    public sealed class MapifyNetFrameworkDiFixture : IDisposable {
        public ServiceProvider Provider { get; }

        public MapifyNetFrameworkDiFixture() {
            Provider = BuildProvider(services => {
                services.AddMapifyProfile<NetFxProfile>();
                services.AddMapify(ServiceLifetime.Singleton);
            });
        }

        public void Dispose() {
            Provider.Dispose();
        }

        private static ServiceProvider BuildProvider(Action<IServiceCollection> configureServices) {
            var services = new ServiceCollection();
            configureServices(services);
            return services.BuildServiceProvider();
        }
    }
}
