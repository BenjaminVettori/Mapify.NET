namespace Mapify.NET.Tests;

public class MapifyDslMapBuilderTests {
    [Fact]
    public void MapBuilder_ShouldApplyExistingTypeMap_WhenPropertyTypesDiffer() {
        var mapify = new Mapify([
            new ExistingMapFallbackProfile()
        ]);

        var result = mapify.Map<ExistingMapSource, ExistingMapTarget>(new ExistingMapSource {
            Name = "alice",
            Child = new ExistingMapChildSource { Value = 7 }
        });

        Assert.Equal("alice", result.Name);
        Assert.NotNull(result.Child);
        Assert.Equal(7, result.Child!.Value);
    }

    [Fact]
    public void MapBuilder_ShouldOverrideInitializerBinding_WhenConfiguredAfterInitializer() {
        var mapify = new Mapify([
            new InitializerOverrideProfile()
        ]);

        var result = mapify.Map<InitializerOverrideSource, InitializerOverrideTarget>(new InitializerOverrideSource {
            Name = "alice"
        });

        Assert.Equal("alice", result.Name);
        Assert.Equal("dsl-value", result.Label);
    }

    [Fact]
    public void MapBuilder_ShouldSupportMarkers() {
        var mapify = new Mapify([
            new MarkerDslProfile()
        ]);

        var result = mapify.Map<MarkerSource, MarkerTarget>(new MarkerSource {
            Name = "alice",
            Child = new MarkerChildSource { Value = 3 },
            Internal = "hidden"
        });

        Assert.Equal("alice", result.Name);
        Assert.NotNull(result.Child);
        Assert.Equal(13, result.Child!.Value);
        Assert.Null(result.Internal);
    }

    [Fact]
    public void MapBuilder_ShouldSupportParameterMarker() {
        var mapify = new Mapify([
            new ParameterDslProfile()
        ]);

        var parameters = new Dictionary<string, object?> {
            ["minScore"] = 50
        };

        var pass = mapify.Map<ParameterSource, ParameterTarget>(new ParameterSource {
            Name = "alice",
            Score = 70
        }, parameters);

        var fail = mapify.Map<ParameterSource, ParameterTarget>(new ParameterSource {
            Name = "bob",
            Score = 40
        }, parameters);

        Assert.Equal("alice", pass.Name);
        Assert.Equal("Pass", pass.ScoreCategory);
        Assert.Equal("bob", fail.Name);
        Assert.Equal("Fail", fail.ScoreCategory);
    }

    [Fact]
    public void MapBuilder_ShouldSupportNamedUseMapMarker() {
        var mapify = new Mapify([
            new NamedUseMapDslProfile()
        ]);

        var result = mapify.Map<NamedUseMapSource, NamedUseMapTarget>(new NamedUseMapSource {
            Name = "alice",
            Child = new NamedUseMapChildSource { Value = 3 }
        });

        Assert.Equal("alice", result.Name);
        Assert.NotNull(result.Child);
        Assert.Equal(103, result.Child!.Value);
    }

    [Fact]
    public void MapBuilder_ShouldSupportProjectToMarker() {
        var mapify = new Mapify([
            new ProjectToDslProfile()
        ]);

        var result = mapify.Map<ProjectToDslSource, ProjectToDslTarget>(new ProjectToDslSource {
            Children = [
                new ProjectToDslChildSource { Value = 1 },
                new ProjectToDslChildSource { Value = 2 }
            ]
        });

        Assert.Equal([11, 12], result.Children.Select(x => x.Value).ToArray());
    }

    [Fact]
    public void MapBuilder_ShouldSupportNamedProjectToMarker() {
        var mapify = new Mapify([
            new NamedProjectToDslProfile()
        ]);

        var result = mapify.Map<NamedProjectToDslSource, NamedProjectToDslTarget>(new NamedProjectToDslSource {
            Children = [
                new NamedProjectToDslChildSource { Value = 1 },
                new NamedProjectToDslChildSource { Value = 2 }
            ]
        });

        Assert.Equal([101, 102], result.Children.Select(x => x.Value).ToArray());
    }

    [Fact]
    public void MapBuilder_NamedMapExecution_ShouldSupportMarkers() {
        var mapify = new Mapify([
            new NamedDslExecutionProfile()
        ]);

        var parameters = new Dictionary<string, object?> {
            ["threshold"] = 50
        };

        var result = mapify.Map<NamedDslExecutionSource, NamedDslExecutionTarget>(new NamedDslExecutionSource {
            Name = "alice",
            Score = 75,
            Child = new NamedDslExecutionChildSource { Value = 4 },
            Internal = "secret"
        }, "Special", parameters);

        Assert.Equal("alice-special", result.Name);
        Assert.Equal("High", result.Category);
        Assert.NotNull(result.Child);
        Assert.Equal(204, result.Child!.Value);
        Assert.Null(result.Internal);
    }

    private sealed class ExistingMapChildSource {
        public int Value { get; set; }
    }

    private sealed class ExistingMapChildTarget {
        public int Value { get; set; }
    }

    private sealed class ExistingMapSource {
        public string Name { get; set; } = string.Empty;

        public ExistingMapChildSource? Child { get; set; }
    }

    private sealed class ExistingMapTarget {
        public required string Name { get; set; }

        public ExistingMapChildTarget? Child { get; set; }
    }

    private sealed class ExistingMapFallbackProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<ExistingMapChildSource, ExistingMapChildTarget>();

            CreateMap<ExistingMapSource, ExistingMapTarget>()
                .Map(d => d.Name, s => s.Name)
                .Map(d => d.Child, s => s.Child);
        }
    }

    private sealed class InitializerOverrideSource {
        public string Name { get; set; } = string.Empty;
    }

    private sealed class InitializerOverrideTarget {
        public required string Name { get; set; }

        public string Label { get; set; } = string.Empty;
    }

    private sealed class InitializerOverrideProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<InitializerOverrideSource, InitializerOverrideTarget>(s => new InitializerOverrideTarget {
                Name = s.Name,
                Label = "initializer-value"
            })
                .Map(d => d.Label, _ => "dsl-value");
        }
    }

    private sealed class MarkerChildSource {
        public int Value { get; set; }
    }

    private sealed class MarkerChildTarget {
        public int Value { get; set; }
    }

    private sealed class MarkerSource {
        public string Name { get; set; } = string.Empty;

        public MarkerChildSource? Child { get; set; }

        public string Internal { get; set; } = string.Empty;
    }

    private sealed class MarkerTarget {
        public required string Name { get; set; }

        public MarkerChildTarget? Child { get; set; }

        public string? Internal { get; set; }
    }

    private sealed class MarkerDslProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<MarkerChildSource, MarkerChildTarget>(s => new MarkerChildTarget {
                Value = s.Value + 10
            });

            CreateMap<MarkerSource, MarkerTarget>()
                .Map(d => d.Name, s => s.Name)
                .Map(d => d.Child, s => UseMap<MarkerChildSource?, MarkerChildTarget?>(s.Child))
                .Map(d => d.Internal, _ => Ignore<string?>());
        }
    }

    private sealed class ParameterSource {
        public required string Name { get; set; }

        public int Score { get; set; }
    }

    private sealed class ParameterTarget {
        public required string Name { get; set; }

        public required string ScoreCategory { get; set; }
    }

    private sealed class ParameterDslProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<ParameterSource, ParameterTarget>()
                .Map(d => d.Name, s => s.Name)
                .Map(d => d.ScoreCategory, s => s.Score >= Parameter<int>("minScore") ? "Pass" : "Fail");
        }
    }

    private sealed class NamedUseMapChildSource {
        public int Value { get; set; }
    }

    private sealed class NamedUseMapChildTarget {
        public int Value { get; set; }
    }

    private sealed class NamedUseMapSource {
        public required string Name { get; set; }

        public NamedUseMapChildSource? Child { get; set; }
    }

    private sealed class NamedUseMapTarget {
        public required string Name { get; set; }

        public NamedUseMapChildTarget? Child { get; set; }
    }

    private sealed class NamedUseMapDslProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<NamedUseMapChildSource, NamedUseMapChildTarget>(x => new NamedUseMapChildTarget {
                Value = x.Value + 1
            });

            CreateMap<NamedUseMapChildSource, NamedUseMapChildTarget>("Boost", x => new NamedUseMapChildTarget {
                Value = x.Value + 100
            });

            CreateMap<NamedUseMapSource, NamedUseMapTarget>()
                .Map(d => d.Name, s => s.Name)
                .Map(d => d.Child, s => UseMap<NamedUseMapChildSource?, NamedUseMapChildTarget?>("Boost", s.Child));
        }
    }

    private sealed class ProjectToDslChildSource {
        public int Value { get; set; }
    }

    private sealed class ProjectToDslChildTarget {
        public int Value { get; set; }
    }

    private sealed class ProjectToDslSource {
        public IEnumerable<ProjectToDslChildSource> Children { get; set; } = [];
    }

    private sealed class ProjectToDslTarget {
        public ProjectToDslChildTarget[] Children { get; set; } = [];
    }

    private sealed class ProjectToDslProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<ProjectToDslChildSource, ProjectToDslChildTarget>(x => new ProjectToDslChildTarget {
                Value = x.Value + 10
            });

            CreateMap<ProjectToDslSource, ProjectToDslTarget>()
                .Map(d => d.Children, s => s.Children.ProjectTo<ProjectToDslChildTarget>().ToArray());
        }
    }

    private sealed class NamedProjectToDslChildSource {
        public int Value { get; set; }
    }

    private sealed class NamedProjectToDslChildTarget {
        public int Value { get; set; }
    }

    private sealed class NamedProjectToDslSource {
        public IEnumerable<NamedProjectToDslChildSource> Children { get; set; } = [];
    }

    private sealed class NamedProjectToDslTarget {
        public NamedProjectToDslChildTarget[] Children { get; set; } = [];
    }

    private sealed class NamedProjectToDslProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<NamedProjectToDslChildSource, NamedProjectToDslChildTarget>(x => new NamedProjectToDslChildTarget {
                Value = x.Value + 1
            });

            CreateMap<NamedProjectToDslChildSource, NamedProjectToDslChildTarget>("Boost", x => new NamedProjectToDslChildTarget {
                Value = x.Value + 100
            });

            CreateMap<NamedProjectToDslSource, NamedProjectToDslTarget>()
                .Map(d => d.Children, s => s.Children.ProjectTo<NamedProjectToDslChildTarget>("Boost").ToArray());
        }
    }

    private sealed class NamedDslExecutionChildSource {
        public int Value { get; set; }
    }

    private sealed class NamedDslExecutionChildTarget {
        public int Value { get; set; }
    }

    private sealed class NamedDslExecutionSource {
        public required string Name { get; set; }

        public int Score { get; set; }

        public NamedDslExecutionChildSource? Child { get; set; }

        public string Internal { get; set; } = string.Empty;
    }

    private sealed class NamedDslExecutionTarget {
        public required string Name { get; set; }

        public required string Category { get; set; }

        public NamedDslExecutionChildTarget? Child { get; set; }

        public string? Internal { get; set; }
    }

    private sealed class NamedDslExecutionProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<NamedDslExecutionChildSource, NamedDslExecutionChildTarget>("SpecialChild", x => new NamedDslExecutionChildTarget {
                Value = x.Value + 200
            });

            CreateMap<NamedDslExecutionSource, NamedDslExecutionTarget>("Special")
                .Map(d => d.Name, s => s.Name + "-special")
                .Map(d => d.Category, s => s.Score >= Parameter<int>("threshold") ? "High" : "Low")
                .Map(d => d.Child, s => UseMap<NamedDslExecutionChildSource?, NamedDslExecutionChildTarget?>("SpecialChild", s.Child))
                .Map(d => d.Internal, _ => Ignore<string?>());
        }
    }
}
