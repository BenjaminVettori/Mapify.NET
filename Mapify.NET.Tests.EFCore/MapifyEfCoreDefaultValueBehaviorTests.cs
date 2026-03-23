namespace Mapify.NET.Tests.EFCore;

public class MapifyEfCoreDefaultValueBehaviorTests {
    [Fact]
    public void CreateMap_ShouldKeepNull_ForNullableCollection_WhenSourceIsNull() {
        var mapify = new Mapify([new EfCoreNullableCollectionProfile()]);

        var mapped = mapify.Map<EfCoreNullableCollectionSource, EfCoreNullableCollectionTarget>(new EfCoreNullableCollectionSource {
            Numbers = null
        });

        Assert.Null(mapped.Numbers);
    }

    [Fact]
    public void CreateMap_ShouldUseEmpty_ForNonNullableCollection_WhenSourceIsNull() {
        var mapify = new Mapify([new EfCoreNonNullableCollectionProfile()]);

        var mapped = mapify.Map<EfCoreNullableCollectionSource, EfCoreNonNullableCollectionTarget>(new EfCoreNullableCollectionSource {
            Numbers = null
        });

        Assert.NotNull(mapped.Numbers);
        Assert.Empty(mapped.Numbers);
    }

    [Fact]
    public void CreateMap_ShouldUseEmpty_ForRequiredCollection_WhenSourceIsNull() {
        var mapify = new Mapify([new EfCoreRequiredCollectionProfile()]);

        var mapped = mapify.Map<EfCoreNullableCollectionSource, EfCoreRequiredCollectionTarget>(new EfCoreNullableCollectionSource {
            Numbers = null
        });

        Assert.NotNull(mapped.Numbers);
        Assert.Empty(mapped.Numbers);
    }

    [Fact]
    public void CreateMap_ShouldPreserveInitializer_ForNonNullableCollection_WhenSourcePropertyIsMissing() {
        var mapify = new Mapify([new EfCoreInitializedCollectionProfile()]);

        var mapped = mapify.Map<EfCoreSourceWithoutCollection, EfCoreInitializedCollectionTarget>(new EfCoreSourceWithoutCollection {
            Id = 5
        });

        Assert.Equal(5, mapped.Id);
        Assert.Equal([99], mapped.Numbers);
    }

    [Fact]
    public void CreateMap_ShouldUseEmpty_ForUninitializedNonNullableCollection_WhenSourcePropertyIsMissing() {
        var mapify = new Mapify([new EfCoreUninitializedCollectionProfile()]);

        var mapped = mapify.Map<EfCoreSourceWithoutCollection, EfCoreUninitializedCollectionTarget>(new EfCoreSourceWithoutCollection {
            Id = 5
        });

        Assert.Equal(5, mapped.Id);
        Assert.NotNull(mapped.Numbers);
        Assert.Empty(mapped.Numbers);
    }

    private sealed class EfCoreNullableCollectionSource {
        public List<int>? Numbers { get; set; }
    }

    private sealed class EfCoreNullableCollectionTarget {
        public List<int>? Numbers { get; set; }
    }

    private sealed class EfCoreNonNullableCollectionTarget {
        public List<int> Numbers { get; set; } = null!;
    }

    private sealed class EfCoreRequiredCollectionTarget {
        public required List<int> Numbers { get; set; }
    }

    private sealed class EfCoreSourceWithoutCollection {
        public int Id { get; set; }
    }

    private sealed class EfCoreInitializedCollectionTarget {
        public int Id { get; set; }
        public List<int> Numbers { get; set; } = [99];
    }

    private sealed class EfCoreUninitializedCollectionTarget {
        public int Id { get; set; }
        public List<int> Numbers { get; set; } = null!;
    }

    private sealed class EfCoreNullableCollectionProfile : MapifyProfile {
        protected override void Configure() => CreateMap<EfCoreNullableCollectionSource, EfCoreNullableCollectionTarget>();
    }

    private sealed class EfCoreNonNullableCollectionProfile : MapifyProfile {
        protected override void Configure() => CreateMap<EfCoreNullableCollectionSource, EfCoreNonNullableCollectionTarget>();
    }

    private sealed class EfCoreRequiredCollectionProfile : MapifyProfile {
        protected override void Configure() => CreateMap<EfCoreNullableCollectionSource, EfCoreRequiredCollectionTarget>();
    }

    private sealed class EfCoreInitializedCollectionProfile : MapifyProfile {
        protected override void Configure() => CreateMap<EfCoreSourceWithoutCollection, EfCoreInitializedCollectionTarget>();
    }

    private sealed class EfCoreUninitializedCollectionProfile : MapifyProfile {
        protected override void Configure() => CreateMap<EfCoreSourceWithoutCollection, EfCoreUninitializedCollectionTarget>();
    }
}
