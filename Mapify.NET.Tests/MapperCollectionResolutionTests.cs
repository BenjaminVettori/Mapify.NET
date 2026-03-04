namespace Mapify.NET.Tests;

[Collection("Mapper Tests")]
public class MapperCollectionResolutionTests {
    public MapperCollectionResolutionTests() {
        Mapper.UseDefaultMapIfTypeMapIsMissing(false);
        Mapper.ClearMappings();
    }

    [Fact]
    public void CreateMap_ShouldPreferExactCollectionMap_OverElementFallback() {
        Mapper.AddMap<CollectionElementSource, CollectionElementTarget>(
            x => new CollectionElementTarget { Value = x.Value + 1 }
        );
        Mapper.AddMap<List<CollectionElementSource>, List<CollectionElementTarget>>(
            x => x.Select(i => new CollectionElementTarget { Value = i.Value + 100 }).ToList()
        );

        var map = Mapper.CreateMap<CollectionContainerSource, CollectionContainerTarget>();
        var mapped = map.Map(new CollectionContainerSource {
            Items = [new CollectionElementSource { Value = 1 }]
        });

        Assert.Equal([101], mapped.Items.Select(x => x.Value).ToArray());
    }

    [Fact]
    public void CreateMap_ShouldFallbackToElementMap_WhenExactCollectionMapIsMissing() {
        Mapper.AddMap<CollectionElementSource, CollectionElementTarget>(
            x => new CollectionElementTarget { Value = x.Value + 1 }
        );

        var map = Mapper.CreateMap<CollectionContainerSource, CollectionContainerTarget>();
        var mapped = map.Map(new CollectionContainerSource {
            Items = [new CollectionElementSource { Value = 1 }]
        });

        Assert.Equal([2], mapped.Items.Select(x => x.Value).ToArray());
    }

    [Fact]
    public void CreateMap_ShouldUseAssignableCollectionMap_BeforeElementFallback() {
        Mapper.AddMap<CollectionElementSource, CollectionElementTarget>(
            x => new CollectionElementTarget { Value = x.Value + 1 }
        );
        Mapper.AddMap<IEnumerable<CollectionElementSource>, IEnumerable<CollectionElementTarget>>(
            x => x.Select(i => new CollectionElementTarget { Value = i.Value + 10 })
        );

        var map = Mapper.CreateMap<CollectionContainerSource, CollectionContainerTarget>();
        var mapped = map.Map(new CollectionContainerSource {
            Items = [new CollectionElementSource { Value = 1 }]
        });

        Assert.Equal([11], mapped.Items.Select(x => x.Value).ToArray());
    }

    [Fact]
    public void CreateMap_ShouldHandleNullCollectionSource_WithoutThrowing() {
        Mapper.AddMap<CollectionElementSource, CollectionElementTarget>(
            x => new CollectionElementTarget { Value = x.Value + 1 }
        );

        var map = Mapper.CreateMap<NullableCollectionContainerSource, NullableCollectionContainerTarget>();
        var mapped = map.Map(new NullableCollectionContainerSource {
            Items = null
        });

        Assert.Null(mapped.Items);
    }

    private sealed class CollectionElementSource { public int Value { get; set; } }
    private sealed class CollectionElementTarget { public int Value { get; set; } }
    private sealed class CollectionContainerSource { public List<CollectionElementSource> Items { get; set; } = []; }
    private sealed class CollectionContainerTarget { public List<CollectionElementTarget> Items { get; set; } = []; }
    private sealed class NullableCollectionContainerSource { public List<CollectionElementSource>? Items { get; set; } }
    private sealed class NullableCollectionContainerTarget { public List<CollectionElementTarget>? Items { get; set; } }
}
