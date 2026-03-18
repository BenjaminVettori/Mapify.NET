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

    [Fact]
    public void Map_ShouldMapListToList_WhenOnlyElementMapIsRegistered() {
        Mapper.AddMap<CollectionElementSource, CollectionElementTarget>(
            x => new CollectionElementTarget { Value = x.Value + 1 }
        );

        var mapped = Mapper.Map<List<CollectionElementSource>, List<CollectionElementTarget>>([
            new CollectionElementSource { Value = 1 },
            new CollectionElementSource { Value = 2 }
        ]);

        Assert.Equal([2, 3], mapped.Select(x => x.Value).ToArray());
    }

    [Fact]
    public void Map_ShouldMapListOfLists_WhenOnlyElementMapIsRegistered() {
        Mapper.AddMap<CollectionElementSource, CollectionElementTarget>(
            x => new CollectionElementTarget { Value = x.Value + 1 }
        );

        var mapped = Mapper.Map<List<List<CollectionElementSource>>, List<List<CollectionElementTarget>>>([
            [
                new CollectionElementSource { Value = 1 },
                new CollectionElementSource { Value = 2 }
            ],
            [
                new CollectionElementSource { Value = 3 }
            ]
        ]);

        var flattened = mapped.SelectMany(inner => inner.Select(item => item.Value)).ToArray();

        Assert.Equal([2, 3, 4], flattened);
    }

    [Fact]
    public void Map_ShouldMapListOfListOfLists_WhenOnlyElementMapIsRegistered() {
        Mapper.AddMap<CollectionElementSource, CollectionElementTarget>(
            x => new CollectionElementTarget { Value = x.Value + 1 }
        );

        var mapped = Mapper.Map<List<List<List<CollectionElementSource>>>, List<List<List<CollectionElementTarget>>>>([
            [
                [
                    new CollectionElementSource { Value = 1 },
                    new CollectionElementSource { Value = 2 }
                ]
            ],
            [
                [
                    new CollectionElementSource { Value = 3 }
                ]
            ]
        ]);

        var flattened = mapped
            .SelectMany(middle => middle.SelectMany(inner => inner.Select(item => item.Value)))
            .ToArray();

        Assert.Equal([2, 3, 4], flattened);
    }

    [Theory]
    [MemberData(nameof(CollectionHierarchyKinds))]
    public void Map_ShouldFallbackFromConcreteListSource_ToAnyRegisteredHierarchyCollectionMap_WhenMappingToObject(CollectionHierarchyKind declaredMapKind) {
        var offset = GetOffsetForKind(declaredMapKind);
        AddCollectionSummaryMap(declaredMapKind, offset);

        var mapped = Mapper.Map<List<CollectionElementSource>, CollectionSummaryTarget>(CreateCollectionSourceList());

        Assert.Equal(3 + offset, mapped.Count);
    }

    [Theory]
    [MemberData(nameof(CollectionHierarchyCombinations))]
    public void Map_ShouldResolveAccordingToCollectionHierarchy_ForAllSourceAndRegisteredCollectionCombinations(
        CollectionHierarchyKind sourceKind,
        CollectionHierarchyKind declaredMapKind
    ) {
        var offset = GetOffsetForKind(declaredMapKind);
        AddCollectionSummaryMap(declaredMapKind, offset);

        if (CanResolveCollectionMap(sourceKind, declaredMapKind)) {
            var mapped = MapCollectionSummary(sourceKind, CreateCollectionSourceList());
            Assert.Equal(3 + offset, mapped.Count);
        } else {
            Assert.Throws<ArgumentException>(() => MapCollectionSummary(sourceKind, CreateCollectionSourceList()));
        }
    }

    [Theory]
    [MemberData(nameof(CollectionHierarchyCombinations))]
    public void Map_ShouldResolveAllCollectionCombinations_WhenOnlyElementMapIsRegistered(
        CollectionHierarchyKind sourceKind,
        CollectionHierarchyKind targetKind
    ) {
        Mapper.AddMap<CollectionElementSource, CollectionElementTarget>(
            x => new CollectionElementTarget { Value = x.Value + 1 }
        );

        var mappedCollection = MapCollectionElements(sourceKind, targetKind, CreateCollectionSourceList());
        var values = mappedCollection.Select(x => x.Value).ToArray();

        Assert.Equal([2, 3, 4], values);
    }

    [Fact]
    public void Map_ShouldPreferHigherRankedCollectionMap_WhenMultipleInterfaceMapsExist_AndNoExactMapIsRegistered() {
        AddCollectionSummaryMap(CollectionHierarchyKind.IEnumerable, 500);
        AddCollectionSummaryMap(CollectionHierarchyKind.IReadOnlyCollection, 400);
        AddCollectionSummaryMap(CollectionHierarchyKind.IReadOnlyList, 300);
        AddCollectionSummaryMap(CollectionHierarchyKind.ICollection, 200);
        AddCollectionSummaryMap(CollectionHierarchyKind.IList, 100);

        var mapped = Mapper.Map<List<CollectionElementSource>, CollectionSummaryTarget>(CreateCollectionSourceList());

        Assert.Equal(103, mapped.Count);
    }

    [Fact]
    public void Map_ShouldPreferExactCollectionMap_OverHierarchyCandidates_WhenBothExist() {
        AddCollectionSummaryMap(CollectionHierarchyKind.IList, 100);
        AddCollectionSummaryMap(CollectionHierarchyKind.List, 1000);

        var mapped = Mapper.Map<List<CollectionElementSource>, CollectionSummaryTarget>(CreateCollectionSourceList());

        Assert.Equal(1003, mapped.Count);
    }

    public static IEnumerable<object[]> CollectionHierarchyKinds() {
        foreach (var kind in Enum.GetValues<CollectionHierarchyKind>()) {
            yield return [kind];
        }
    }

    public static IEnumerable<object[]> CollectionHierarchyCombinations() {
        foreach (var sourceKind in Enum.GetValues<CollectionHierarchyKind>()) {
            foreach (var declaredMapKind in Enum.GetValues<CollectionHierarchyKind>()) {
                yield return [sourceKind, declaredMapKind];
            }
        }
    }

    private static List<CollectionElementSource> CreateCollectionSourceList()
        => [
            new CollectionElementSource { Value = 1 },
            new CollectionElementSource { Value = 2 },
            new CollectionElementSource { Value = 3 }
        ];

    private static int GetOffsetForKind(CollectionHierarchyKind kind)
        => kind switch {
            CollectionHierarchyKind.List => 10,
            CollectionHierarchyKind.IList => 20,
            CollectionHierarchyKind.ICollection => 30,
            CollectionHierarchyKind.IReadOnlyList => 40,
            CollectionHierarchyKind.IReadOnlyCollection => 50,
            CollectionHierarchyKind.IEnumerable => 60,
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };

    private static void AddCollectionSummaryMap(CollectionHierarchyKind kind, int offset) {
        switch (kind) {
            case CollectionHierarchyKind.List:
                Mapper.AddMap<List<CollectionElementSource>, CollectionSummaryTarget>(x => new CollectionSummaryTarget { Count = x.Count() + offset });
                break;
            case CollectionHierarchyKind.IList:
                Mapper.AddMap<IList<CollectionElementSource>, CollectionSummaryTarget>(x => new CollectionSummaryTarget { Count = x.Count() + offset });
                break;
            case CollectionHierarchyKind.ICollection:
                Mapper.AddMap<ICollection<CollectionElementSource>, CollectionSummaryTarget>(x => new CollectionSummaryTarget { Count = x.Count() + offset });
                break;
            case CollectionHierarchyKind.IReadOnlyList:
                Mapper.AddMap<IReadOnlyList<CollectionElementSource>, CollectionSummaryTarget>(x => new CollectionSummaryTarget { Count = x.Count() + offset });
                break;
            case CollectionHierarchyKind.IReadOnlyCollection:
                Mapper.AddMap<IReadOnlyCollection<CollectionElementSource>, CollectionSummaryTarget>(x => new CollectionSummaryTarget { Count = x.Count() + offset });
                break;
            case CollectionHierarchyKind.IEnumerable:
                Mapper.AddMap<IEnumerable<CollectionElementSource>, CollectionSummaryTarget>(x => new CollectionSummaryTarget { Count = x.Count() + offset });
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind));
        }
    }

    private static CollectionSummaryTarget MapCollectionSummary(CollectionHierarchyKind sourceKind, List<CollectionElementSource> source)
        => sourceKind switch {
            CollectionHierarchyKind.List => Mapper.Map<List<CollectionElementSource>, CollectionSummaryTarget>(source),
            CollectionHierarchyKind.IList => Mapper.Map<IList<CollectionElementSource>, CollectionSummaryTarget>(source),
            CollectionHierarchyKind.ICollection => Mapper.Map<ICollection<CollectionElementSource>, CollectionSummaryTarget>(source),
            CollectionHierarchyKind.IReadOnlyList => Mapper.Map<IReadOnlyList<CollectionElementSource>, CollectionSummaryTarget>(source),
            CollectionHierarchyKind.IReadOnlyCollection => Mapper.Map<IReadOnlyCollection<CollectionElementSource>, CollectionSummaryTarget>(source),
            CollectionHierarchyKind.IEnumerable => Mapper.Map<IEnumerable<CollectionElementSource>, CollectionSummaryTarget>(source),
            _ => throw new ArgumentOutOfRangeException(nameof(sourceKind))
        };

    private static IEnumerable<CollectionElementTarget> MapCollectionElements(
        CollectionHierarchyKind sourceKind,
        CollectionHierarchyKind targetKind,
        List<CollectionElementSource> source
    )
        => sourceKind switch {
            CollectionHierarchyKind.List => MapCollectionElementsCore<List<CollectionElementSource>>(source, targetKind),
            CollectionHierarchyKind.IList => MapCollectionElementsCore<IList<CollectionElementSource>>(source, targetKind),
            CollectionHierarchyKind.ICollection => MapCollectionElementsCore<ICollection<CollectionElementSource>>(source, targetKind),
            CollectionHierarchyKind.IReadOnlyList => MapCollectionElementsCore<IReadOnlyList<CollectionElementSource>>(source, targetKind),
            CollectionHierarchyKind.IReadOnlyCollection => MapCollectionElementsCore<IReadOnlyCollection<CollectionElementSource>>(source, targetKind),
            CollectionHierarchyKind.IEnumerable => MapCollectionElementsCore<IEnumerable<CollectionElementSource>>(source, targetKind),
            _ => throw new ArgumentOutOfRangeException(nameof(sourceKind))
        };

    private static IEnumerable<CollectionElementTarget> MapCollectionElementsCore<TSourceCollection>(
        TSourceCollection source,
        CollectionHierarchyKind targetKind
    ) where TSourceCollection : IEnumerable<CollectionElementSource>
        => targetKind switch {
            CollectionHierarchyKind.List => Mapper.Map<TSourceCollection, List<CollectionElementTarget>>(source),
            CollectionHierarchyKind.IList => Mapper.Map<TSourceCollection, IList<CollectionElementTarget>>(source),
            CollectionHierarchyKind.ICollection => Mapper.Map<TSourceCollection, ICollection<CollectionElementTarget>>(source),
            CollectionHierarchyKind.IReadOnlyList => Mapper.Map<TSourceCollection, IReadOnlyList<CollectionElementTarget>>(source),
            CollectionHierarchyKind.IReadOnlyCollection => Mapper.Map<TSourceCollection, IReadOnlyCollection<CollectionElementTarget>>(source),
            CollectionHierarchyKind.IEnumerable => Mapper.Map<TSourceCollection, IEnumerable<CollectionElementTarget>>(source),
            _ => throw new ArgumentOutOfRangeException(nameof(targetKind))
        };

    private static bool CanResolveCollectionMap(CollectionHierarchyKind sourceKind, CollectionHierarchyKind declaredMapKind)
        => sourceKind switch {
            CollectionHierarchyKind.List => declaredMapKind is CollectionHierarchyKind.List
                or CollectionHierarchyKind.IList
                or CollectionHierarchyKind.ICollection
                or CollectionHierarchyKind.IReadOnlyList
                or CollectionHierarchyKind.IReadOnlyCollection
                or CollectionHierarchyKind.IEnumerable,
            CollectionHierarchyKind.IList => declaredMapKind is CollectionHierarchyKind.IList
                or CollectionHierarchyKind.ICollection
                or CollectionHierarchyKind.IEnumerable,
            CollectionHierarchyKind.ICollection => declaredMapKind is CollectionHierarchyKind.ICollection
                or CollectionHierarchyKind.IEnumerable,
            CollectionHierarchyKind.IReadOnlyList => declaredMapKind is CollectionHierarchyKind.IReadOnlyList
                or CollectionHierarchyKind.IReadOnlyCollection
                or CollectionHierarchyKind.IEnumerable,
            CollectionHierarchyKind.IReadOnlyCollection => declaredMapKind is CollectionHierarchyKind.IReadOnlyCollection
                or CollectionHierarchyKind.IEnumerable,
            CollectionHierarchyKind.IEnumerable => declaredMapKind is CollectionHierarchyKind.IEnumerable,
            _ => false
        };

    public enum CollectionHierarchyKind {
        List,
        IList,
        ICollection,
        IReadOnlyList,
        IReadOnlyCollection,
        IEnumerable
    }

    private sealed class CollectionElementSource { public int Value { get; set; } }
    private sealed class CollectionElementTarget { public int Value { get; set; } }
    private sealed class CollectionSummaryTarget { public int Count { get; set; } }
    private sealed class CollectionContainerSource { public List<CollectionElementSource> Items { get; set; } = []; }
    private sealed class CollectionContainerTarget { public List<CollectionElementTarget> Items { get; set; } = []; }
    private sealed class NullableCollectionContainerSource { public List<CollectionElementSource>? Items { get; set; } }
    private sealed class NullableCollectionContainerTarget { public List<CollectionElementTarget>? Items { get; set; } }
}
