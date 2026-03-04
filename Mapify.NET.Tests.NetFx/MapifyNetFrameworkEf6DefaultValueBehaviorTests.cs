namespace Mapify.NET.Tests.NetFx;

public class MapifyNetFrameworkEf6DefaultValueBehaviorTests {
    public MapifyNetFrameworkEf6DefaultValueBehaviorTests() {
        Mapper.UseDefaultMapIfTypeMapIsMissing(false);
        Mapper.ClearMappings();
    }

    [Fact]
    public void CreateMap_ShouldKeepNull_ForNullableCollection_WhenSourceIsNull() {
        var map = Mapper.CreateMap<Ef6NullableCollectionSource, Ef6NullableCollectionTarget>();

        var mapped = map.Map(new Ef6NullableCollectionSource {
            Numbers = null
        });

        Assert.Null(mapped.Numbers);
    }

    [Fact]
    public void CreateMap_ShouldUseEmpty_ForNonNullableCollection_WhenSourceIsNull() {
        var map = Mapper.CreateMap<Ef6NullableCollectionSource, Ef6NonNullableCollectionTarget>();

        var mapped = map.Map(new Ef6NullableCollectionSource {
            Numbers = null
        });

        Assert.NotNull(mapped.Numbers);
        Assert.Empty(mapped.Numbers);
    }

    [Fact]
    public void CreateMap_ShouldPreserveInitializer_ForNonNullableCollection_WhenSourcePropertyIsMissing() {
        var map = Mapper.CreateMap<Ef6SourceWithoutCollection, Ef6InitializedCollectionTarget>();

        var mapped = map.Map(new Ef6SourceWithoutCollection {
            Id = 7
        });

        Assert.Equal(7, mapped.Id);
        Assert.Equal(new[] { 88 }, mapped.Numbers.ToArray());
    }

    [Fact]
    public void CreateMap_ShouldUseEmpty_ForUninitializedNonNullableCollection_WhenSourcePropertyIsMissing() {
        var map = Mapper.CreateMap<Ef6SourceWithoutCollection, Ef6UninitializedCollectionTarget>();

        var mapped = map.Map(new Ef6SourceWithoutCollection {
            Id = 7
        });

        Assert.Equal(7, mapped.Id);
        Assert.NotNull(mapped.Numbers);
        Assert.Empty(mapped.Numbers);
    }

    private class Ef6NullableCollectionSource {
        public List<int>? Numbers { get; set; }
    }

    private class Ef6NullableCollectionTarget {
        public List<int>? Numbers { get; set; }
    }

    private class Ef6NonNullableCollectionTarget {
        public List<int> Numbers { get; set; } = null!;
    }

    private class Ef6SourceWithoutCollection {
        public int Id { get; set; }
    }

    private class Ef6InitializedCollectionTarget {
        public int Id { get; set; }
        public List<int> Numbers { get; set; } = [88];
    }

    private class Ef6UninitializedCollectionTarget {
        public int Id { get; set; }
        public List<int> Numbers { get; set; } = null!;
    }
}
