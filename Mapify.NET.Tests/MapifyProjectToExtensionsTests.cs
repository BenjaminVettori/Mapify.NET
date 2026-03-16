using System.Collections;
using System.Reflection;

namespace Mapify.NET.Tests;

[Collection("Mapper Tests")]
public class MapifyProjectToExtensionsTests {
    public MapifyProjectToExtensionsTests() {
        Mapper.UseDefaultMapIfTypeMapIsMissing(false);
        Mapper.ClearMappings();
    }

    private static TException AssertThrowsUnwrapped<TException>(Action action)
        where TException : Exception {
        try {
            action();
        } catch (TargetInvocationException ex) when (ex.InnerException is TException inner) {
            return inner;
        }

        return Assert.Throws<TException>(action);
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
    public void ProjectTo_IEnumerable_InstanceWithParameters_ShouldConvertStringParameterToInt() {
        var mapify = new Mapify([new EnumerableProjectWithParameterProfile()]);
        IEnumerable source = new[] {
            new ProjectSourceWithParameter { Value = 4 }
        };

        var projected = source
            .ProjectTo<ProjectTargetWithParameter>(mapify, new Dictionary<string, object?> { ["offset"] = "6" })
            .Single();

        Assert.Equal(10, projected.Value);
    }

    [Fact]
    public void ProjectTo_IQueryable_InstanceWithParameters_ShouldParseEnumParameterFromString_CaseInsensitive() {
        var mapify = new Mapify([new EnumerableProjectEnumParameterProfile()]);
        IQueryable source = new[] {
            new ProjectSourceForEnumParameter { Value = 1 }
        }.AsQueryable();

        var projected = source
            .ProjectTo<ProjectTargetForEnumParameter>(mapify, new Dictionary<string, object?> { ["status"] = "enabled" })
            .Single();

        Assert.Equal(ProjectTargetStatus.Enabled, projected.Status);
    }

    [Fact]
    public void ProjectTo_IQueryable_InstanceWithParameters_ShouldAllowNullForNullableParameterType() {
        var mapify = new Mapify([new EnumerableProjectNullableParameterProfile()]);
        IQueryable source = new[] {
            new ProjectSourceForNullableParameter { Value = 1 }
        }.AsQueryable();

        var projected = source
            .ProjectTo<ProjectTargetForNullableParameter>(mapify, new Dictionary<string, object?> { ["offset"] = null })
            .Single();

        Assert.Null(projected.Value);
    }

    [Fact]
    public void ProjectTo_IQueryable_InstanceWithParameters_ShouldThrowWhenNullProvidedForNonNullableParameterType() {
        var mapify = new Mapify([new EnumerableProjectWithParameterProfile()]);
        IQueryable source = new[] {
            new ProjectSourceWithParameter { Value = 4 }
        }.AsQueryable();

        var ex = AssertThrowsUnwrapped<InvalidOperationException>(() => source
            .ProjectTo<ProjectTargetWithParameter>(mapify, new Dictionary<string, object?> { ["offset"] = null })
            .Single());

        Assert.Contains("cannot be null", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ProjectTo_IEnumerable_InstanceWithParameters_ShouldThrowWhenParameterCannotBeConverted() {
        var mapify = new Mapify([new EnumerableProjectWithParameterProfile()]);
        IEnumerable source = new[] {
            new ProjectSourceWithParameter { Value = 4 }
        };

        var ex = AssertThrowsUnwrapped<InvalidOperationException>(() => source
            .ProjectTo<ProjectTargetWithParameter>(mapify, new Dictionary<string, object?> { ["offset"] = "not-a-number" })
            .Single());

        Assert.Contains("cannot be converted", ex.Message, StringComparison.Ordinal);
        Assert.Contains("offset", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ProjectTo_IEnumerable_InstanceWithParameters_ShouldThrowWhenParameterMarkerNameIsWhitespace() {
        var mapify = new Mapify([new EnumerableProjectInvalidParameterNameProfile()]);
        IEnumerable source = new[] {
            new ProjectSourceInvalidParameterName { Value = 4 }
        };

        var ex = AssertThrowsUnwrapped<InvalidOperationException>(() => source
            .ProjectTo<ProjectTargetInvalidParameterName>(mapify, new Dictionary<string, object?> { ["offset"] = 1 })
            .Single());

        Assert.Contains("non-empty constant string", ex.Message, StringComparison.Ordinal);
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
    }

    [Fact]
    public void ProjectTo_IQueryable_InstanceWithParameters_ShouldThrowForNullMapify() {
        IQueryable source = new[] { new ProjectSource { Value = 1 } }.AsQueryable();

        Assert.Throws<ArgumentNullException>(() => source.ProjectTo<ProjectTarget>((IMapify)null!, new Dictionary<string, object?>()));
    }

    [Fact]
    public void ProjectTo_IQueryable_InstanceWithParameters_ShouldThrowForEmptyParameters() {
        var mapify = new Mapify([new EnumerableProjectWithParameterProfile()]);
        IQueryable source = new[] { new ProjectSourceWithParameter { Value = 1 } }.AsQueryable();

        var ex = Assert.Throws<ArgumentException>(() => source.ProjectTo<ProjectTargetWithParameter>(mapify, new Dictionary<string, object?>()));

        Assert.Contains("At least one runtime parameter", ex.Message, StringComparison.Ordinal);
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
    public void ProjectTo_IEnumerable_InstanceWithParameters_ShouldThrowForEmptyParameters() {
        var mapify = new Mapify([new EnumerableProjectWithParameterProfile()]);
        IEnumerable source = new[] { new ProjectSourceWithParameter { Value = 1 } };

        var ex = Assert.Throws<ArgumentException>(() => source.ProjectTo<ProjectTargetWithParameter>(mapify, new Dictionary<string, object?>()));

        Assert.Contains("At least one runtime parameter", ex.Message, StringComparison.Ordinal);
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

        Assert.Throws<ArgumentNullException>(() => source!.ProjectTo<ProjectTarget>(mapify, new Dictionary<string, object?>()));
        Assert.Throws<ArgumentNullException>(() => source!.ProjectTo<ProjectTarget>(mapify, "NamedParam"));
        Assert.Throws<ArgumentNullException>(() => source!.ProjectTo<ProjectTarget>(mapify, "NamedParam", new Dictionary<string, object?>()));
    }

    [Fact]
    public void ProjectTo_IEnumerable_InstanceNamed_ShouldThrowForNullMapify() {
        IEnumerable source = new[] { new ProjectSource { Value = 1 } };

        Assert.Throws<ArgumentNullException>(() => source.ProjectTo<ProjectTarget>((IMapify)null!, "Named"));
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

    private sealed class EnumerableProjectEnumParameterProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<ProjectSourceForEnumParameter, ProjectTargetForEnumParameter>(x => new ProjectTargetForEnumParameter {
                Status = Parameter<ProjectTargetStatus>("status")
            });
        }
    }

    private sealed class EnumerableProjectNullableParameterProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<ProjectSourceForNullableParameter, ProjectTargetForNullableParameter>(x => new ProjectTargetForNullableParameter {
                Value = Parameter<int?>("offset")
            });
        }
    }

    private sealed class EnumerableProjectInvalidParameterNameProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<ProjectSourceInvalidParameterName, ProjectTargetInvalidParameterName>(x => new ProjectTargetInvalidParameterName {
                Value = Parameter<int>(" ")
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

    private enum ProjectTargetStatus {
        Disabled = 0,
        Enabled = 1
    }

    private sealed class ProjectSourceForEnumParameter {
        public int Value { get; set; }
    }

    private sealed class ProjectTargetForEnumParameter {
        public ProjectTargetStatus Status { get; set; }
    }

    private sealed class ProjectSourceForNullableParameter {
        public int Value { get; set; }
    }

    private sealed class ProjectTargetForNullableParameter {
        public int? Value { get; set; }
    }

    private sealed class ProjectSourceInvalidParameterName {
        public int Value { get; set; }
    }

    private sealed class ProjectTargetInvalidParameterName {
        public int Value { get; set; }
    }
}
