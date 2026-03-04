using System.Linq.Expressions;

namespace Mapify.NET.Tests;

[Collection("Mapper Tests")]
public class MapperDefaultValueBehaviorTests {
    public MapperDefaultValueBehaviorTests() {
        Mapper.UseDefaultMapIfTypeMapIsMissing(false);
        Mapper.ClearMappings();
    }

    [Fact]
    public void CreateMap_ShouldKeepNull_ForNullableCollection_WhenSourceIsNull() {
        Mapper.AddMap<DefaultElementSource, DefaultElementTarget>(
            x => new DefaultElementTarget { Value = x.Value + 1 }
        );

        var map = Mapper.CreateMap<NullableCollectionSource, NullableCollectionTarget>();
        var mapped = map.Map(new NullableCollectionSource { Items = null });

        Assert.Null(mapped.Items);
    }

    [Fact]
    public void CreateMap_ShouldUseEmpty_ForNonNullableCollection_WhenSourceIsNull() {
        Mapper.AddMap<DefaultElementSource, DefaultElementTarget>(
            x => new DefaultElementTarget { Value = x.Value + 1 }
        );

        var map = Mapper.CreateMap<NullableCollectionSource, NonNullableCollectionTarget>();
        var mapped = map.Map(new NullableCollectionSource { Items = null });

        Assert.NotNull(mapped.Items);
        Assert.Empty(mapped.Items);
    }

    [Fact]
    public void CreateMap_ShouldUseEmpty_ForRequiredCollection_WhenSourceIsNull() {
        Mapper.AddMap<DefaultElementSource, DefaultElementTarget>(
            x => new DefaultElementTarget { Value = x.Value + 1 }
        );

        var map = Mapper.CreateMap<NullableCollectionSource, RequiredCollectionTarget>();
        var mapped = map.Map(new NullableCollectionSource { Items = null });

        Assert.NotNull(mapped.Items);
        Assert.Empty(mapped.Items);
    }

    [Fact]
    public void CreateMap_ShouldPreserveInitializer_ForRequiredCollection_WhenSourcePropertyIsMissing() {
        var map = Mapper.CreateMap<SourceWithoutItems, RequiredCollectionInitializedTarget>();

        var mapped = map.Map(new SourceWithoutItems { Value = 42 });

        Assert.Equal(42, mapped.Value);
        Assert.Equal([77], mapped.Items.Select(x => x.Value).ToArray());
    }

    [Fact]
    public void CreateMap_ShouldUseEmpty_ForUninitializedNonNullableCollection_WhenSourcePropertyIsMissing() {
        var map = Mapper.CreateMap<SourceWithoutItems, NonNullableUninitializedCollectionTarget>();

        var mapped = map.Map(new SourceWithoutItems { Value = 42 });

        Assert.Equal(42, mapped.Value);
        Assert.NotNull(mapped.Items);
        Assert.Empty(mapped.Items);
    }

    [Fact]
    public void CreateMap_ShouldPreserveDerivedCtorInitializer_ForInheritedCollection_WhenSourcePropertyIsMissing() {
        var map = Mapper.CreateMap<SourceWithoutItems, DerivedCtorInitializedCollectionTarget>();

        var mapped = map.Map(new SourceWithoutItems { Value = 42 });

        Assert.Equal(42, mapped.Value);
        Assert.Equal([88], mapped.Items.Select(x => x.Value).ToArray());
    }

    [Fact]
    public void ClearMappings_ShouldClearInitializedPropertyCache() {
        var map = Mapper.CreateMap<SourceWithoutItems, DerivedCtorInitializedCollectionTarget>();
        _ = map.Map(new SourceWithoutItems { Value = 1 });

        var cacheField = typeof(Mapper).GetField("_initializedPropertyCache", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!;
        var cache = cacheField.GetValue(null)!;
        var countProperty = cache.GetType().GetProperty("Count")!;

        var countBefore = (int)countProperty.GetValue(cache)!;
        Assert.True(countBefore > 0);

        Mapper.ClearMappings();

        var countAfter = (int)countProperty.GetValue(cache)!;
        Assert.Equal(0, countAfter);
    }

    private sealed class DefaultElementSource { public int Value { get; set; } }
    private sealed class DefaultElementTarget { public int Value { get; set; } }

    private sealed class NullableCollectionSource { public List<DefaultElementSource>? Items { get; set; } }
    private sealed class NullableCollectionTarget { public List<DefaultElementTarget>? Items { get; set; } }
    private sealed class NonNullableCollectionTarget { public List<DefaultElementTarget> Items { get; set; } = null!; }
    private sealed class RequiredCollectionTarget { public required List<DefaultElementTarget> Items { get; set; } }

    private sealed class SourceWithoutItems { public int Value { get; set; } }
    private sealed class RequiredCollectionInitializedTarget {
        public int Value { get; set; }
        public required List<DefaultElementTarget> Items { get; set; } = [new DefaultElementTarget { Value = 77 }];
    }

    private sealed class NonNullableUninitializedCollectionTarget {
        public int Value { get; set; }
        public List<DefaultElementTarget> Items { get; set; } = null!;
    }

    private abstract class BaseCollectionTarget {
        public int Value { get; set; }
        public List<DefaultElementTarget> Items { get; set; } = null!;
    }

    private sealed class DerivedCtorInitializedCollectionTarget : BaseCollectionTarget {
        public DerivedCtorInitializedCollectionTarget() {
            Items = [new DefaultElementTarget { Value = 88 }];
        }
    }
}
