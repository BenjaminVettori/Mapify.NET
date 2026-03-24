namespace Mapify.NET.Tests.NetFx;

public class MapifyNetFrameworkEf6DefaultValueBehaviorTests {
    [Fact]
    public void CreateMap_ShouldKeepNull_ForNullableCollection_WhenSourceIsNull() {
        var mapify = new Mapify([new Ef6NullableCollectionProfile()]);

        var mapped = mapify.Map<Ef6NullableCollectionSource, Ef6NullableCollectionTarget>(new Ef6NullableCollectionSource {
            Numbers = null
        });

        Assert.Null(mapped.Numbers);
    }

    [Fact]
    public void CreateMap_ShouldUseEmpty_ForNonNullableCollection_WhenSourceIsNull() {
        var mapify = new Mapify([new Ef6NonNullableCollectionProfile()]);

        var mapped = mapify.Map<Ef6NullableCollectionSource, Ef6NonNullableCollectionTarget>(new Ef6NullableCollectionSource {
            Numbers = null
        });

        Assert.NotNull(mapped.Numbers);
        Assert.Empty(mapped.Numbers);
    }

    [Fact]
    public void CreateMap_ShouldPreserveInitializer_ForNonNullableCollection_WhenSourcePropertyIsMissing() {
        var mapify = new Mapify([new Ef6InitializedCollectionProfile()]);

        var mapped = mapify.Map<Ef6SourceWithoutCollection, Ef6InitializedCollectionTarget>(new Ef6SourceWithoutCollection {
            Id = 7
        });

        Assert.Equal(7, mapped.Id);
        Assert.Equal(new[] { 88 }, mapped.Numbers.ToArray());
    }

    [Fact]
    public void CreateMap_ShouldUseEmpty_ForUninitializedNonNullableCollection_WhenSourcePropertyIsMissing() {
        var mapify = new Mapify([new Ef6UninitializedCollectionProfile()]);

        var mapped = mapify.Map<Ef6SourceWithoutCollection, Ef6UninitializedCollectionTarget>(new Ef6SourceWithoutCollection {
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

    private sealed class Ef6NullableCollectionProfile : MapifyProfile {
        protected override void Configure() => CreateMap<Ef6NullableCollectionSource, Ef6NullableCollectionTarget>();
    }

    private sealed class Ef6NonNullableCollectionProfile : MapifyProfile {
        protected override void Configure() => CreateMap<Ef6NullableCollectionSource, Ef6NonNullableCollectionTarget>();
    }

    private sealed class Ef6InitializedCollectionProfile : MapifyProfile {
        protected override void Configure() => CreateMap<Ef6SourceWithoutCollection, Ef6InitializedCollectionTarget>();
    }

    private sealed class Ef6UninitializedCollectionProfile : MapifyProfile {
        protected override void Configure() => CreateMap<Ef6SourceWithoutCollection, Ef6UninitializedCollectionTarget>();
    }
}
