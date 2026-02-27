using Microsoft.Extensions.DependencyInjection;

using Mapify.TestProfiles.ModuleA;
using Mapify.TestProfiles.ModuleB;

namespace Mapify.NET.Tests;

public class MapifyDiExtensionsTests : IClassFixture<MapifyDiExtensionsTests.MapifyDiExtensionsFixture> {
    private readonly MapifyDiExtensionsFixture _fixture;

    public MapifyDiExtensionsTests(MapifyDiExtensionsFixture fixture) {
        _fixture = fixture;
    }

    [Fact]
    public void AddMapify_WithAssemblies_ShouldRegisterProfilesAndMapper() {
        var mapper = _fixture.Provider.GetRequiredService<IMapify>();

        var mapped = mapper.Map<DiSourceA, DiTargetA>(new DiSourceA { Value = 42 });

        Assert.Equal(42, mapped.Value);
    }

    [Fact]
    public void AddMapify_ShouldResolveSingletonMapperFromSharedContainer() {
        var mapper = _fixture.Provider.GetRequiredService<IMapify>();

        var mapped = mapper.Map<DiSourceA, DiTargetA>(new DiSourceA { Value = 99 });

        Assert.Equal(99, mapped.Value);

        var mapper2 = _fixture.Provider.GetRequiredService<IMapify>();
        Assert.Same(mapper, mapper2);
    }

    [Fact]
    public void AddMapify_ShouldRespectTransientLifecycle() {
        using var provider = BuildProvider(services => {
            services.AddMapifyProfile<DiProfileA>();
            services.AddMapify(ServiceLifetime.Transient);
        });

        var mapper1 = provider.GetRequiredService<IMapify>();
        var mapper2 = provider.GetRequiredService<IMapify>();

        Assert.NotSame(mapper1, mapper2);
    }

    [Fact]
    public void AddMapify_ShouldRespectScopedLifecycle() {
        using var provider = BuildProvider(services => {
            services.AddMapifyProfile<DiProfileA>();
            services.AddMapify(ServiceLifetime.Scoped);
        });

        using var scope1 = provider.CreateScope();
        var s1a = scope1.ServiceProvider.GetRequiredService<IMapify>();
        var s1b = scope1.ServiceProvider.GetRequiredService<IMapify>();
        Assert.Same(s1a, s1b);

        using var scope2 = provider.CreateScope();
        var s2 = scope2.ServiceProvider.GetRequiredService<IMapify>();
        Assert.NotSame(s1a, s2);
    }

    [Fact]
    public void NamedMapper_ShouldUseOnlyNamedProfiles() {
        using var provider = BuildProvider(services => {
            services.AddMapifyProfile<DiProfileA>();
            services.AddMapify(ServiceLifetime.Singleton);

            services.AddMapifyProfile<DiProfileB>("secondary");
            services.AddMapifyNamed("secondary", ServiceLifetime.Singleton);
        });

        var defaultMapper = provider.GetRequiredService<IMapify>();
        var namedMapper = provider.GetMapify("secondary");

        var defaultMapped = defaultMapper.Map<DiSourceA, DiTargetA>(new DiSourceA { Value = 5 });
        Assert.Equal(5, defaultMapped.Value);

        var namedMapped = namedMapper.Map<DiSourceB, DiTargetB>(new DiSourceB { Text = "ok" });
        Assert.Equal("ok", namedMapped.Text);

        Assert.Throws<ArgumentException>(() => defaultMapper.Map<DiSourceB, DiTargetB>(new DiSourceB { Text = "x" }));
        Assert.Throws<ArgumentException>(() => namedMapper.Map<DiSourceA, DiTargetA>(new DiSourceA { Value = 1 }));
    }

    [Fact]
    public void AddMapify_CalledMultipleTimes_ShouldKeepSingleMapperAndAccumulateProfiles() {
        var services = new ServiceCollection();

        // Simulate first project registration
        services.AddMapifyProfile<DiProfileA>();
        services.AddMapify(ServiceLifetime.Singleton);

        // Simulate another project registering additional profiles later
        services.AddMapifyProfiles(typeof(DiProfileB).Assembly);
        services.AddMapify(ServiceLifetime.Singleton);

        Assert.Single(services, x => x.ServiceType == typeof(IMapify));

        using var provider = services.BuildServiceProvider();
        var mapper = provider.GetRequiredService<IMapify>();

        var mappedA = mapper.Map<DiSourceA, DiTargetA>(new DiSourceA { Value = 7 });
        Assert.Equal(7, mappedA.Value);

        var mappedB = mapper.Map<DiSourceB, DiTargetB>(new DiSourceB { Text = "added-later" });
        Assert.Equal("added-later", mappedB.Text);
    }

    [Fact]
    public void AddMapify_WithAssemblyOverload_CalledConsecutively_ShouldAccumulateProfiles() {
        var services = new ServiceCollection();

        services.AddMapify(typeof(ModuleAProfile).Assembly);
        services.AddMapify(typeof(ModuleBProfile).Assembly);

        Assert.Single(services, x => x.ServiceType == typeof(IMapify));

        using var provider = services.BuildServiceProvider();
        var mapper = provider.GetRequiredService<IMapify>();

        var mappedA = mapper.Map<ModuleASource, ModuleATarget>(new ModuleASource { Value = 11 });
        Assert.Equal(11, mappedA.Value);

        var mappedB = mapper.Map<ModuleBSource, ModuleBTarget>(new ModuleBSource { Text = "module-b" });
        Assert.Equal("module-b", mappedB.Text);
    }

    [Fact]
    public void AddMapify_ShouldThrow_WhenServicesIsNull() {
        Assert.Throws<ArgumentNullException>(() => MapifyServiceCollectionExtensions.AddMapify(null!));
    }

    [Fact]
    public void AddMapify_WithLifecycleAndAssemblies_ShouldThrow_WhenServicesIsNull() {
        Assert.Throws<ArgumentNullException>(() => MapifyServiceCollectionExtensions.AddMapify(null!, ServiceLifetime.Singleton, typeof(DiProfileA).Assembly));
    }

    [Fact]
    public void AddMapify_WithAssembliesOnly_ShouldThrow_WhenServicesIsNull() {
        Assert.Throws<ArgumentNullException>(() => MapifyServiceCollectionExtensions.AddMapify(null!, typeof(DiProfileA).Assembly));
    }

    [Fact]
    public void AddMapify_ShouldThrow_WhenLifecycleIsInvalid() {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentOutOfRangeException>(() => services.AddMapify((ServiceLifetime)999));
    }

    [Fact]
    public void AddMapifyProfile_ShouldThrow_WhenMapperNameIsWhitespace() {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentException>(() => services.AddMapifyProfile<DiProfileA>(" "));
    }

    [Fact]
    public void AddMapifyProfile_ShouldThrow_WhenServicesIsNull() {
        Assert.Throws<ArgumentNullException>(() => MapifyServiceCollectionExtensions.AddMapifyProfile<DiProfileA>(null!));
    }

    [Fact]
    public void AddMapifyNamed_ShouldThrow_WhenNameIsWhitespace() {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentException>(() => services.AddMapifyNamed(" "));
    }

    [Fact]
    public void AddMapifyNamed_ShouldThrow_WhenLifecycleIsInvalid() {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentOutOfRangeException>(() => services.AddMapifyNamed("named", (ServiceLifetime)999));
    }

    [Fact]
    public void AddMapifyProfiles_ShouldThrow_WhenMapperNameIsWhitespace() {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentException>(() => services.AddMapifyProfiles(" ", typeof(DiProfileA).Assembly));
    }

    [Fact]
    public void AddMapifyProfiles_ShouldThrow_WhenServicesIsNull() {
        Assert.Throws<ArgumentNullException>(() => MapifyServiceCollectionExtensions.AddMapifyProfiles(null!, typeof(DiProfileA).Assembly));
    }

    [Fact]
    public void AddMapifyProfiles_ShouldThrow_WhenAssembliesArgumentIsNull() {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(() => MapifyServiceCollectionExtensions.AddMapifyProfiles(services, "named", (System.Reflection.Assembly[])null!));
    }

    [Fact]
    public void GetMapify_ShouldThrow_WhenServiceProviderIsNull() {
        Assert.Throws<ArgumentNullException>(() => MapifyServiceCollectionExtensions.GetMapify(null!, "secondary"));
    }

    [Fact]
    public void GetMapify_ShouldThrow_WhenNameIsWhitespace() {
        using var provider = BuildProvider(services => services.AddMapifyNamed("secondary"));

        Assert.Throws<ArgumentException>(() => provider.GetMapify(" "));
    }

    [Fact]
    public void GetMapify_ShouldThrow_WhenNamedMapperIsMissing() {
        using var provider = BuildProvider(services => services.AddMapifyNamed("existing"));

        Assert.Throws<InvalidOperationException>(() => provider.GetMapify("missing"));
    }

    public class DiSourceA {
        public int Value { get; set; }
    }

    public class DiTargetA {
        public int Value { get; set; }
    }

    public class DiSourceB {
        public string Text { get; set; } = string.Empty;
    }

    public class DiTargetB {
        public string Text { get; set; } = string.Empty;
    }

    public class DiProfileA : MapifyProfile {
        protected override void Configure() {
            CreateMap<DiSourceA, DiTargetA>();
        }
    }

    public class DiProfileB : MapifyProfile {
        protected override void Configure() {
            CreateMap<DiSourceB, DiTargetB>();
        }
    }

    public sealed class MapifyDiExtensionsFixture : IDisposable {
        public ServiceProvider Provider { get; }

        public MapifyDiExtensionsFixture() {
            Provider = BuildProvider(services => {
                services.AddMapifyProfile<DiProfileA>();
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

    private static ServiceProvider BuildProvider(Action<IServiceCollection> configureServices) {
        var services = new ServiceCollection();
        configureServices(services);
        return services.BuildServiceProvider();
    }
}
