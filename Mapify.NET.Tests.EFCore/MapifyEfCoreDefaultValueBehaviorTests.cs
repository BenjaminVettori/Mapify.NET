namespace Mapify.NET.Tests.EFCore;

public class MapifyEfCoreDefaultValueBehaviorTests {
    public MapifyEfCoreDefaultValueBehaviorTests() {
        Mapper.UseDefaultMapIfTypeMapIsMissing(false);
        Mapper.ClearMappings();
    }

    [Fact]
    public void CreateMap_ShouldKeepNull_ForNullableCollection_WhenSourceIsNull() {
        var map = Mapper.CreateMap<EfCoreNullableCollectionSource, EfCoreNullableCollectionTarget>();

        var mapped = map.Map(new EfCoreNullableCollectionSource {
            Numbers = null
        });

        Assert.Null(mapped.Numbers);
    }

    [Fact]
    public void CreateMap_ShouldUseEmpty_ForNonNullableCollection_WhenSourceIsNull() {
        var map = Mapper.CreateMap<EfCoreNullableCollectionSource, EfCoreNonNullableCollectionTarget>();

        var mapped = map.Map(new EfCoreNullableCollectionSource {
            Numbers = null
        });

        Assert.NotNull(mapped.Numbers);
        Assert.Empty(mapped.Numbers);
    }

    [Fact]
    public void CreateMap_ShouldUseEmpty_ForRequiredCollection_WhenSourceIsNull() {
        var map = Mapper.CreateMap<EfCoreNullableCollectionSource, EfCoreRequiredCollectionTarget>();

        var mapped = map.Map(new EfCoreNullableCollectionSource {
            Numbers = null
        });

        Assert.NotNull(mapped.Numbers);
        Assert.Empty(mapped.Numbers);
    }

    [Fact]
    public void CreateMap_ShouldPreserveInitializer_ForNonNullableCollection_WhenSourcePropertyIsMissing() {
        var map = Mapper.CreateMap<EfCoreSourceWithoutCollection, EfCoreInitializedCollectionTarget>();

        var mapped = map.Map(new EfCoreSourceWithoutCollection {
            Id = 5
        });

        Assert.Equal(5, mapped.Id);
        Assert.Equal([99], mapped.Numbers);
    }

    [Fact]
    public void CreateMap_ShouldUseEmpty_ForUninitializedNonNullableCollection_WhenSourcePropertyIsMissing() {
        var map = Mapper.CreateMap<EfCoreSourceWithoutCollection, EfCoreUninitializedCollectionTarget>();

        var mapped = map.Map(new EfCoreSourceWithoutCollection {
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
}
