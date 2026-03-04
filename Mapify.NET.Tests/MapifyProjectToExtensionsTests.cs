using System.Collections;
using System.Linq.Expressions;

namespace Mapify.NET.Tests;

[Collection("Mapper Tests")]
public class MapifyProjectToExtensionsTests {
    public MapifyProjectToExtensionsTests() {
        Mapper.UseDefaultMapIfTypeMapIsMissing(false);
        Mapper.ClearMappings();
    }

    [Fact]
    public void ProjectTo_IEnumerable_Static_ShouldProjectItems() {
        Mapper.AddMap<ProjectSource, ProjectTarget>(x => new ProjectTarget { Value = x.Value + 1 });

        IEnumerable source = new[] {
            new ProjectSource { Value = 1 },
            new ProjectSource { Value = 2 }
        };

        var projected = source.ProjectTo<ProjectTarget>().Select(x => x.Value).ToArray();

        Assert.Equal([2, 3], projected);
    }

    [Fact]
    public void ProjectTo_IEnumerable_StaticWithParameters_ShouldApplyParameterMarker() {
        Mapper.AddMap(StaticParameterizedMapProfile.CreateMap());

        IEnumerable source = new[] {
            new ProjectSource { Value = 2 }
        };

        var projected = source
            .ProjectTo<ProjectTarget>(new Dictionary<string, object?> { ["offset"] = 5 })
            .Single();

        Assert.Equal(7, projected.Value);
    }

    [Fact]
    public void ProjectTo_IEnumerable_Instance_ShouldProjectItems() {
        var mapify = new Mapify([new EnumerableProjectProfile()]);
        IEnumerable source = new[] {
            new ProjectSource { Value = 3 }
        };

        var projected = source.ProjectTo<ProjectTarget>(mapify).Single();

        Assert.Equal(4, projected.Value);
    }

    [Fact]
    public void ProjectTo_IEnumerable_InstanceWithParameters_ShouldApplyParameterMarker() {
        var mapify = new Mapify([new EnumerableProjectWithParameterProfile()]);
        IEnumerable source = new[] {
            new ProjectSourceWithParameter { Value = 4 }
        };

        var projected = source
            .ProjectTo<ProjectTargetWithParameter>(mapify, new Dictionary<string, object?> { ["offset"] = 6 })
            .Single();

        Assert.Equal(10, projected.Value);
    }

    [Fact]
    public void ProjectTo_IEnumerable_InstanceNamed_ShouldUseNamedMap() {
        var mapify = new Mapify([new EnumerableNamedProjectProfile()]);
        IEnumerable source = new[] {
            new ProjectSource { Value = 1 }
        };

        var projected = source.ProjectTo<ProjectTarget>(mapify, "Named").Single();

        Assert.Equal(11, projected.Value);
    }

    [Fact]
    public void ProjectTo_IEnumerable_InstanceNamedWithParameters_ShouldUseNamedParameterizedMap() {
        var mapify = new Mapify([new EnumerableNamedProjectWithParameterProfile()]);
        IEnumerable source = new[] {
            new ProjectSource { Value = 2 }
        };

        var projected = source
            .ProjectTo<ProjectTarget>(mapify, "NamedParam", new Dictionary<string, object?> { ["offset"] = 8 })
            .Single();

        Assert.Equal(10, projected.Value);
    }

    [Fact]
    public void ProjectTo_IQueryable_InstanceNamedWithParameters_ShouldUseNamedParameterizedMap() {
        var mapify = new Mapify([new EnumerableNamedProjectWithParameterProfile()]);
        IQueryable source = new[] {
            new ProjectSource { Value = 5 }
        }.AsQueryable();

        var projected = source
            .ProjectTo<ProjectTarget>(mapify, "NamedParam", new Dictionary<string, object?> { ["offset"] = 4 })
            .Single();

        Assert.Equal(9, projected.Value);
    }

    [Fact]
    public void ProjectTo_NamedMarkerOverload_ShouldThrowForDirectRuntimeUse() {
        IQueryable querySource = new[] { new ProjectSource { Value = 1 } }.AsQueryable();
        IEnumerable enumerableSource = new[] { new ProjectSource { Value = 1 } };

        Assert.Throws<InvalidOperationException>(() => querySource.ProjectTo<ProjectTarget>("Named"));
        Assert.Throws<InvalidOperationException>(() => enumerableSource.ProjectTo<ProjectTarget>("Named"));
    }

    [Fact]
    public void ProjectTo_IEnumerable_ShouldThrowForNullSourceOrMapify() {
        IEnumerable? nullSource = null;
        var mapify = new Mapify([new EnumerableProjectProfile()]);

        Assert.Throws<ArgumentNullException>(() => nullSource!.ProjectTo<ProjectTarget>());
        Assert.Throws<ArgumentNullException>(() => nullSource!.ProjectTo<ProjectTarget>(mapify));
        Assert.Throws<ArgumentNullException>(() => new[] { new ProjectSource { Value = 1 } }.ProjectTo<ProjectTarget>((IMapify)null!));
    }

    [Fact]
    public void ProjectTo_IQueryable_ShouldThrowForInvalidNameOrNullMapify() {
        IQueryable source = new[] { new ProjectSource { Value = 1 } }.AsQueryable();
        var mapify = new Mapify([new EnumerableNamedProjectProfile()]);

        Assert.Throws<ArgumentNullException>(() => source.ProjectTo<ProjectTarget>((IMapify)null!));
        Assert.Throws<ArgumentNullException>(() => source.ProjectTo<ProjectTarget>((IMapify)null!, "Named"));
        Assert.Throws<ArgumentNullException>(() => source.ProjectTo<ProjectTarget>((IMapify)null!, "Named", new Dictionary<string, object?>()));
        Assert.Throws<ArgumentException>(() => source.ProjectTo<ProjectTarget>(mapify, " "));
        Assert.Throws<ArgumentException>(() => source.ProjectTo<ProjectTarget>(mapify, " ", new Dictionary<string, object?>()));
    }

    [Fact]
    public void ProjectTo_IQueryable_StaticOverloads_ShouldThrowForNullSource() {
        IQueryable? source = null;

        Assert.Throws<ArgumentNullException>(() => source!.ProjectTo<ProjectTarget>());
        Assert.Throws<ArgumentNullException>(() => source!.ProjectTo<ProjectTarget>(new Dictionary<string, object?>()));
    }

    [Fact]
    public void ProjectTo_IQueryable_InstanceWithParameters_ShouldThrowForNullMapify() {
        IQueryable source = new[] { new ProjectSource { Value = 1 } }.AsQueryable();

        Assert.Throws<ArgumentNullException>(() => source.ProjectTo<ProjectTarget>((IMapify)null!, new Dictionary<string, object?>()));
    }

    [Fact]
    public void ProjectTo_IQueryable_InstanceOverloads_ShouldThrowForNullSource() {
        IQueryable? source = null;
        var mapify = new Mapify([new EnumerableNamedProjectWithParameterProfile()]);

        Assert.Throws<ArgumentNullException>(() => source!.ProjectTo<ProjectTarget>(mapify));
        Assert.Throws<ArgumentNullException>(() => source!.ProjectTo<ProjectTarget>(mapify, new Dictionary<string, object?>()));
        Assert.Throws<ArgumentNullException>(() => source!.ProjectTo<ProjectTarget>(mapify, "NamedParam"));
        Assert.Throws<ArgumentNullException>(() => source!.ProjectTo<ProjectTarget>(mapify, "NamedParam", new Dictionary<string, object?>()));
    }

    [Fact]
    public void ProjectTo_IEnumerable_InstanceNamedOverloads_ShouldThrowForInvalidNameAndNullMapify() {
        IEnumerable source = new[] { new ProjectSource { Value = 1 } };
        var mapify = new Mapify([new EnumerableNamedProjectProfile()]);

        Assert.Throws<ArgumentException>(() => source.ProjectTo<ProjectTarget>(mapify, " "));
        Assert.Throws<ArgumentException>(() => source.ProjectTo<ProjectTarget>(mapify, " ", new Dictionary<string, object?>()));
        Assert.Throws<ArgumentNullException>(() => source.ProjectTo<ProjectTarget>((IMapify)null!, new Dictionary<string, object?>()));
    }

    [Fact]
    public void ProjectTo_IEnumerable_ShouldResolveStringElementTypeAsChar() {
        Mapper.AddMap<char, string>(c => c.ToString().ToUpperInvariant());
        IEnumerable source = "ab";

        var projected = source.ProjectTo<string>().ToArray();

        Assert.Equal(["A", "B"], projected);
    }

    [Fact]
    public void ProjectTo_IEnumerable_ShouldFallbackToObjectForNonGenericCollection() {
        Mapper.AddMap<object, ProjectTarget>(x => new ProjectTarget {
            Value = ((ProjectSource)x).Value + 1
        });

        IEnumerable source = new ArrayList {
            new ProjectSource { Value = 7 }
        };

        var projected = source.ProjectTo<ProjectTarget>().Single();

        Assert.Equal(8, projected.Value);
    }

    [Fact]
    public void ProjectTo_IEnumerable_ShouldResolveElementTypeFromGenericEnumerableInterface() {
        Mapper.AddMap<ProjectSource, ProjectTarget>(x => new ProjectTarget { Value = x.Value + 1 });
        IEnumerable source = new List<ProjectSource> {
            new ProjectSource { Value = 9 }
        };

        var projected = source.ProjectTo<ProjectTarget>().Single();

        Assert.Equal(10, projected.Value);
    }

    [Fact]
    public void ProjectTo_IEnumerable_InstanceOverloads_ShouldThrowForNullSource() {
        IEnumerable? source = null;
        var mapify = new Mapify([new EnumerableNamedProjectWithParameterProfile()]);

        Assert.Throws<ArgumentNullException>(() => source!.ProjectTo<ProjectTarget>(new Dictionary<string, object?>()));
        Assert.Throws<ArgumentNullException>(() => source!.ProjectTo<ProjectTarget>(mapify, new Dictionary<string, object?>()));
        Assert.Throws<ArgumentNullException>(() => source!.ProjectTo<ProjectTarget>(mapify, "NamedParam"));
        Assert.Throws<ArgumentNullException>(() => source!.ProjectTo<ProjectTarget>(mapify, "NamedParam", new Dictionary<string, object?>()));
    }

    [Fact]
    public void ProjectTo_IEnumerable_InstanceNamed_ShouldThrowForNullMapify() {
        IEnumerable source = new[] { new ProjectSource { Value = 1 } };

        Assert.Throws<ArgumentNullException>(() => source.ProjectTo<ProjectTarget>((IMapify)null!, "Named"));
    }

    private sealed class StaticParameterizedMapProfile : MapifyProfile {
        protected override void Configure() {
        }

        public static Expression<Func<ProjectSource, ProjectTarget>> CreateMap()
            => x => new ProjectTarget {
                Value = x.Value + Parameter<int>("offset")
            };
    }

    private sealed class EnumerableProjectProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<ProjectSource, ProjectTarget>(x => new ProjectTarget { Value = x.Value + 1 });
        }
    }

    private sealed class EnumerableProjectWithParameterProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<ProjectSourceWithParameter, ProjectTargetWithParameter>(x => new ProjectTargetWithParameter {
                Value = x.Value + Parameter<int>("offset")
            });
        }
    }

    private sealed class EnumerableNamedProjectProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<ProjectSource, ProjectTarget>("Named", x => new ProjectTarget { Value = x.Value + 10 });
        }
    }

    private sealed class EnumerableNamedProjectWithParameterProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<ProjectSource, ProjectTarget>("NamedParam", x => new ProjectTarget {
                Value = x.Value + Parameter<int>("offset")
            });
        }
    }

    private sealed class ProjectSource {
        public int Value { get; set; }
    }

    private sealed class ProjectTarget {
        public int Value { get; set; }
    }

    private sealed class ProjectSourceWithParameter {
        public int Value { get; set; }
    }

    private sealed class ProjectTargetWithParameter {
        public int Value { get; set; }
    }
}
