namespace Mapify.NET.Tests; 
public class MapifyInstanceTests {
    private class Source {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private class Target {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private class SourceA {
        public int ValueA { get; set; }
    }

    private class TargetA {
        public int ValueA { get; set; }
    }

    private class SourceB {
        public string ValueB { get; set; } = string.Empty;
    }

    private class TargetB {
        public string ValueB { get; set; } = string.Empty;
    }

    private class NameSource {
        public string Name { get; set; } = string.Empty;
    }

    private class ChildSource {
        public int Number { get; set; }
    }

    private class ChildTarget {
        public int Number { get; set; }
    }

    private class ParentSource {
        public ChildSource Child { get; set; } = new ChildSource();
    }

    private class ParentTarget {
        public ChildTarget Child { get; set; } = new ChildTarget();
    }

    private class MarkerChildSource {
        public int Number { get; set; }
    }

    private class MarkerChildTarget {
        public int Number { get; set; }
    }

    private class MarkerParentSource {
        public MarkerChildSource Child { get; set; } = new MarkerChildSource();
    }

    private class MarkerParentTarget {
        public MarkerChildTarget Child { get; set; } = new MarkerChildTarget();
    }

    private class LeafSource {
        public int Number { get; set; }
    }

    private class LeafTarget {
        public int Number { get; set; }
    }

    private class MiddleSource {
        public LeafSource Leaf { get; set; } = new LeafSource();
    }

    private class MiddleTarget {
        public LeafTarget Leaf { get; set; } = new LeafTarget();
    }

    private class RootSource {
        public MiddleSource Middle { get; set; } = new MiddleSource();
    }

    private class RootTarget {
        public MiddleTarget Middle { get; set; } = new MiddleTarget();
    }

    private struct NumberSource {
        public int Value { get; set; }
    }

    private struct NumberTarget {
        public int Value { get; set; }
    }

    private class NumberContainerSrcToTarget {
        public NumberSource Number { get; set; }
    }

    private class NumberContainerTarget {
        public NumberTarget Number { get; set; }
    }

    private class NumberContainerSrcToNullableTarget {
        public NumberSource Number { get; set; }
    }

    private class NumberContainerNullableTarget {
        public NumberTarget? Number { get; set; }
    }

    private class NumberContainerNullableSrcToTarget {
        public NumberSource? Number { get; set; }
    }

    private class NumberContainerNullableSrcToNullableTarget {
        public NumberSource? Number { get; set; }
    }

    private class UseMapContainerSrcToTarget {
        public NumberSource Number { get; set; }
    }

    private class UseMapContainerTarget {
        public NumberTarget Number { get; set; }
    }

    private class UseMapContainerSrcToNullableTarget {
        public NumberSource Number { get; set; }
    }

    private class UseMapContainerNullableTarget {
        public NumberTarget? Number { get; set; }
    }

    private class UseMapContainerNullableSrcToTarget {
        public NumberSource? Number { get; set; }
    }

    private class UseMapContainerNullableSrcToNullableTarget {
        public NumberSource? Number { get; set; }
    }

    private class NumberContainerSrcPropToTarget {
        public NumberSource SourceNumber { get; set; }
    }

    private class NumberContainerSrcPropToNullableTarget {
        public NumberSource SourceNumber { get; set; }
    }

    private class NumberContainerNullableSrcPropToTarget {
        public NumberSource? SourceNumber { get; set; }
    }

    private class NumberContainerNullableSrcPropToNullableTarget {
        public NumberSource? SourceNumber { get; set; }
    }

    private class NumberContainerTargetNamed {
        public NumberTarget Number { get; set; }
    }

    private class NumberContainerNullableTargetNamed {
        public NumberTarget? Number { get; set; }
    }

    private class ElementSource {
        public int Value { get; set; }
    }

    private class ElementTarget {
        public int Value { get; set; }
    }

    private class CollectionUseMapSource {
        public ElementSource[] ItemsArray { get; set; } = [];
        public List<ElementSource> ItemsList { get; set; } = [];
    }

    private class CollectionUseMapTarget {
        public ElementTarget[] ItemsArray { get; set; } = [];
        public List<ElementTarget> ItemsList { get; set; } = [];
        public IEnumerable<ElementTarget> ItemsEnumerable { get; set; } = [];
        public ICollection<ElementTarget> ItemsCollection { get; set; } = [];
        public IList<ElementTarget> ItemsArrayAsList { get; set; } = [];
    }

    private class ImplicitPrimitiveCollectionsSource {
        public int[] Numbers { get; set; } = [];
        public ICollection<string> Texts { get; set; } = [];
    }

    private class ImplicitPrimitiveCollectionsTarget {
        public List<int> Numbers { get; set; } = [];
        public string[] Texts { get; set; } = [];
    }

    private class ImplicitCollectionParentSource {
        public ElementSource[] Items { get; set; } = [];
    }

    private class ImplicitCollectionParentTarget {
        public ElementTarget[] Items { get; set; } = [];
    }

    private class NamedPerson {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
    }

    private class NamedStudentSource {
        public string Name { get; set; } = string.Empty;
    }

    private class NamedStudentTarget {
        public string Name { get; set; } = string.Empty;
    }

    private class NamedClassSource {
        public IEnumerable<NamedStudentSource> Students { get; set; } = [];
    }

    private class NamedClassTarget {
        public IEnumerable<NamedStudentTarget> StudentsUpper { get; set; } = [];
        public IEnumerable<NamedStudentTarget> StudentsLower { get; set; } = [];
    }

    private class FilterStudentSource {
        public string? Name { get; set; }
    }

    private class FilterStudentTarget {
        public string Name { get; set; } = string.Empty;
    }

    private class FilterClassSource {
        public IEnumerable<FilterStudentSource> Students { get; set; } = [];
    }

    private class FilterClassTarget {
        public IEnumerable<FilterStudentTarget> Students { get; set; } = [];
    }

    private class ChainedAddressSource {
        public string StreetName { get; set; } = string.Empty;
    }

    private class ChainedAddressTarget {
        public string StreetName { get; set; } = string.Empty;
    }

    private class ChainedPersonSource {
        public IEnumerable<ChainedAddressSource> Addresses { get; set; } = [];
    }

    private class ChainedPersonTarget {
        public IEnumerable<ChainedAddressTarget> Addresses { get; set; } = [];
    }

    private class ChainedNamedPersonTarget {
        public IEnumerable<ChainedAddressTarget> Addresses { get; set; } = [];
    }

    private class CalculationPersonSource {
        public int AgeInYears { get; set; }
    }

    private class CalculationPersonTarget {
        public int AgeInDays { get; set; }
    }

    private class TemperatureMeasurementSource {
        public decimal Fahrenheit { get; set; }
    }

    private class TemperatureMeasurementDto {
        public decimal Fahrenheit { get; set; }
    }

    private class TemperatureSeriesSource {
        public IEnumerable<TemperatureMeasurementSource> Measurements { get; set; } = [];
    }

    private class TemperatureSeriesTarget {
        public decimal MaxTemperatureCelsius { get; set; }
    }

    private class ProjectToQuerySource {
        public int Value { get; set; }
    }

    private class ProjectToQueryTarget {
        public int Value { get; set; }
    }

    private class ProjectToAddressSource {
        public string StreetName { get; set; } = string.Empty;
    }

    private class ProjectToAddressTarget {
        public string StreetName { get; set; } = string.Empty;
    }

    private class ProjectToPersonSource {
        public IEnumerable<ProjectToAddressSource> Addresses { get; set; } = [];
    }

    private class ProjectToPersonTarget {
        public ProjectToAddressTarget[] Addresses { get; set; } = [];
    }

    private class ProjectToNamedPhoneSource {
        public string Number { get; set; } = string.Empty;
    }

    private class ProjectToNamedPhoneTarget {
        public string Number { get; set; } = string.Empty;
    }

    private class ProjectToNamedPersonSource {
        public IEnumerable<ProjectToNamedPhoneSource> Phones { get; set; } = [];
    }

    private class ProjectToNamedPersonTarget {
        public ProjectToNamedPhoneTarget[] Phones { get; set; } = [];
    }

    private enum FaultProfileMode {
        None,
        WhitespaceName,
        UseMapMissingDirect,
        UseMapInvalidNameDirect,
        UseMapNoMapNested,
        UseMapInvalidNameNested,
        UseMapFieldBinding,
        CoalesceBinding
    }

    private class FaultChildSource {
        public int Value { get; set; }
    }

    private class FaultChildTarget {
        public int Value { get; set; }
    }

    private class FaultSource {
        public FaultChildSource Child { get; set; } = new FaultChildSource();
        public int? MaybeValue { get; set; }
    }

    private class FaultTarget {
        public FaultChildTarget Child { get; set; } = new FaultChildTarget();
        public string Text { get; set; } = string.Empty;
        public int Value { get; set; }
    }

    private class FaultTargetWithField {
        public FaultChildTarget Child = new FaultChildTarget();
    }

    private class FaultProfile : MapifyProfile {
        public static FaultProfileMode Mode { get; set; }

        protected override void Configure() {
            var mode = Mode;

            if (mode == FaultProfileMode.WhitespaceName) {
                CreateMap<FaultSource, FaultTarget>(" ");
                return;
            }

            if (mode == FaultProfileMode.CoalesceBinding) {
                CreateMap<FaultSource, FaultTarget>(x => new FaultTarget {
                    Value = x.MaybeValue ?? 123
                });
                return;
            }

            if (mode != FaultProfileMode.UseMapMissingDirect && mode != FaultProfileMode.UseMapNoMapNested) {
                CreateMap<FaultChildSource, FaultChildTarget>(x => new FaultChildTarget { Value = x.Value + 1 });
            }

            if (mode == FaultProfileMode.UseMapMissingDirect) {
                CreateMap<FaultSource, FaultTarget>(x => new FaultTarget {
                    Child = UseMap<FaultChildSource, FaultChildTarget>(x.Child)
                });
                return;
            }

            if (mode == FaultProfileMode.UseMapInvalidNameDirect) {
                var name = "fault";
                CreateMap<FaultSource, FaultTarget>(x => new FaultTarget {
                    Child = UseMap<FaultChildSource, FaultChildTarget>(name, x.Child)
                });
                return;
            }

            if (mode == FaultProfileMode.UseMapNoMapNested) {
                CreateMap<FaultSource, FaultTarget>(x => new FaultTarget {
                    Text = UseMap<FaultChildSource, FaultChildTarget>(x.Child).ToString()!
                });
                return;
            }

            if (mode == FaultProfileMode.UseMapInvalidNameNested) {
                var name = "fault";
                CreateMap<FaultSource, FaultTarget>(x => new FaultTarget {
                    Text = UseMap<FaultChildSource, FaultChildTarget>(name, x.Child).ToString()!
                });
                return;
            }

            if (mode == FaultProfileMode.UseMapFieldBinding) {
                CreateMap<FaultSource, FaultTargetWithField>(x => new FaultTargetWithField {
                    Child = UseMap<FaultChildSource, FaultChildTarget>(x.Child)
                });
                return;
            }

            CreateMap<FaultSource, FaultTarget>(x => new FaultTarget {
                Child = UseMap<FaultChildSource, FaultChildTarget>(x.Child)
            });
        }
    }

    private class NamedValueOnlyProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<NameSource, string>("ByName", x => x.Name);
        }
    }

    private enum SourceStatus {
        Inactive = 0,
        Active = 1
    }

    private enum TargetStatus {
        Disabled = 0,
        Enabled = 1
    }

    private class ProfileA : MapifyProfile {
        protected override void Configure() {
            CreateMap<SourceA, TargetA>();
        }
    }

    private class ProfileB : MapifyProfile {
        protected override void Configure() {
            CreateMap<SourceB, TargetB>();
        }
    }

    private class ValueMapProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<NameSource, string>(x => x.Name);
            CreateMap<SourceStatus, TargetStatus>(x => x == SourceStatus.Active ? TargetStatus.Enabled : TargetStatus.Disabled);
        }
    }

    private class NamedObjectProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<SourceA, TargetA>("NamedObj", x => new TargetA { ValueA = x.ValueA + 1 });
        }
    }

    private class ParentProfile : MapifyProfile {
        protected override void Configure() {
            // No explicit initializer for Child -> should use ChildSource -> ChildTarget map implicitly.
            CreateMap<ParentSource, ParentTarget>();
        }
    }

    private class ChildProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<ChildSource, ChildTarget>(x => new ChildTarget { Number = x.Number + 1 });
        }
    }

    private class ParentProfileWithUseMapMarker : MapifyProfile {
        protected override void Configure() {
            CreateMap<MarkerParentSource, MarkerParentTarget>(x => new MarkerParentTarget {
                Child = UseMap<MarkerChildSource, MarkerChildTarget>(x.Child)
            });
        }
    }

    private class MarkerChildProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<MarkerChildSource, MarkerChildTarget>(x => new MarkerChildTarget { Number = x.Number + 1 });
        }
    }

    private class LeafProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<LeafSource, LeafTarget>(x => new LeafTarget { Number = x.Number + 1 });
        }
    }

    private class MiddleProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<MiddleSource, MiddleTarget>();
        }
    }

    private class RootProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<RootSource, RootTarget>();
        }
    }

    private class NumberProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<NumberSource, NumberTarget>(x => new NumberTarget { Value = x.Value + 1 });
        }
    }

    private class NumberContainerProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<NumberContainerSrcToTarget, NumberContainerTarget>();
            CreateMap<NumberContainerSrcToNullableTarget, NumberContainerNullableTarget>();
            CreateMap<NumberContainerNullableSrcToTarget, NumberContainerTarget>();
            CreateMap<NumberContainerNullableSrcToNullableTarget, NumberContainerNullableTarget>();
        }
    }

    private class NumberContainerUseMapProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<UseMapContainerSrcToTarget, UseMapContainerTarget>(x => new UseMapContainerTarget {
                Number = UseMap<NumberSource, NumberTarget>(x.Number)
            });

            CreateMap<UseMapContainerSrcToNullableTarget, UseMapContainerNullableTarget>(x => new UseMapContainerNullableTarget {
                Number = UseMap<NumberSource, NumberTarget>(x.Number)
            });

            CreateMap<UseMapContainerNullableSrcToTarget, UseMapContainerTarget>(x => new UseMapContainerTarget {
                Number = UseMap<NumberSource?, NumberTarget>(x.Number)
            });

            CreateMap<UseMapContainerNullableSrcToNullableTarget, UseMapContainerNullableTarget>(x => new UseMapContainerNullableTarget {
                Number = UseMap<NumberSource?, NumberTarget>(x.Number)
            });
        }
    }

    private class NumberContainerUseMapWithSourceArgProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<NumberContainerSrcPropToTarget, NumberContainerTargetNamed>(x => new NumberContainerTargetNamed {
                Number = UseMap<NumberSource, NumberTarget>(x.SourceNumber)
            });

            CreateMap<NumberContainerSrcPropToNullableTarget, NumberContainerNullableTargetNamed>(x => new NumberContainerNullableTargetNamed {
                Number = UseMap<NumberSource, NumberTarget>(x.SourceNumber)
            });

            CreateMap<NumberContainerNullableSrcPropToTarget, NumberContainerTargetNamed>(x => new NumberContainerTargetNamed {
                Number = UseMap<NumberSource?, NumberTarget>(x.SourceNumber)
            });

            CreateMap<NumberContainerNullableSrcPropToNullableTarget, NumberContainerNullableTargetNamed>(x => new NumberContainerNullableTargetNamed {
                Number = UseMap<NumberSource?, NumberTarget>(x.SourceNumber)
            });
        }
    }

    private class ElementProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<ElementSource, ElementTarget>(x => new ElementTarget { Value = x.Value + 1 });
        }
    }

    private class CollectionUseMapProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<CollectionUseMapSource, CollectionUseMapTarget>(x => new CollectionUseMapTarget {
                ItemsArray = UseMap<ElementSource[], ElementTarget[]>(x.ItemsArray),
                ItemsList = UseMap<List<ElementSource>, List<ElementTarget>>(x.ItemsList),
                ItemsEnumerable = UseMap<IEnumerable<ElementSource>, IEnumerable<ElementTarget>>(x.ItemsArray),
                ItemsCollection = UseMap<ICollection<ElementSource>, ICollection<ElementTarget>>(x.ItemsList),
                ItemsArrayAsList = UseMap<ICollection<ElementSource>, IList<ElementTarget>>(x.ItemsArray)
            });
        }
    }

    private class ImplicitPrimitiveCollectionsProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<ImplicitPrimitiveCollectionsSource, ImplicitPrimitiveCollectionsTarget>();
        }
    }

    private class ImplicitCollectionParentProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<ImplicitCollectionParentSource, ImplicitCollectionParentTarget>();
        }
    }

    private class NamedPersonValueMapsProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<NamedPerson, string>(x => x.FirstName);
            CreateMap<NamedPerson, string>("FullName", x => x.FirstName + " " + x.LastName);
            CreateMap<NamedPerson, string>("Initials", x => x.FirstName.Substring(0, 1) + x.LastName.Substring(0, 1));
        }
    }

    private class NamedStudentProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<NamedStudentSource, NamedStudentTarget>("Upper", x => new NamedStudentTarget {
                Name = x.Name.ToUpper()
            });

            CreateMap<NamedStudentSource, NamedStudentTarget>("Lower", x => new NamedStudentTarget {
                Name = x.Name.ToLower()
            });
        }
    }

    private class NamedClassProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<NamedClassSource, NamedClassTarget>(x => new NamedClassTarget {
                StudentsUpper = UseMap<IEnumerable<NamedStudentSource>, IEnumerable<NamedStudentTarget>>("Upper", x.Students),
                StudentsLower = UseMap<IEnumerable<NamedStudentSource>, IEnumerable<NamedStudentTarget>>("Lower", x.Students)
            });
        }
    }

    private class FilterStudentProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<FilterStudentSource, FilterStudentTarget>();
        }
    }

    private class FilterClassProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<FilterClassSource, FilterClassTarget>(x => new FilterClassTarget {
                Students = UseMap<IEnumerable<FilterStudentSource>, IEnumerable<FilterStudentTarget>>(x.Students.Where(s => s.Name != null))
            });
        }
    }

    private class ChainedAddressProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<ChainedAddressSource, ChainedAddressTarget>();
        }
    }

    private class ChainedPersonProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<ChainedPersonSource, ChainedPersonTarget>(x => new ChainedPersonTarget {
                Addresses = UseMap<IEnumerable<ChainedAddressSource>, IEnumerable<ChainedAddressTarget>>(x.Addresses)
                    .OrderBy(dto => dto.StreetName)
            });
        }
    }

    private class ChainedNamedAddressProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<ChainedAddressSource, ChainedAddressTarget>("Reverse", x => new ChainedAddressTarget {
                StreetName = new string(x.StreetName.Reverse().ToArray())
            });
        }
    }

    private class ChainedNamedPersonProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<ChainedPersonSource, ChainedNamedPersonTarget>(x => new ChainedNamedPersonTarget {
                Addresses = UseMap<IEnumerable<ChainedAddressSource>, IEnumerable<ChainedAddressTarget>>("Reverse", x.Addresses)
                    .OrderBy(dto => dto.StreetName)
            });
        }
    }

    private class CalculationNumberProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<int, int>(x => x);
        }
    }

    private class CalculationPersonProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<CalculationPersonSource, CalculationPersonTarget>(x => new CalculationPersonTarget {
                AgeInDays = 365 * UseMap<int, int>(x.AgeInYears)
            });
        }
    }

    private class TemperatureMeasurementProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<TemperatureMeasurementSource, TemperatureMeasurementDto>();
        }
    }

    private class TemperatureSeriesProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<TemperatureSeriesSource, TemperatureSeriesTarget>(x => new TemperatureSeriesTarget {
                MaxTemperatureCelsius = (
                    UseMap<IEnumerable<TemperatureMeasurementSource>, IEnumerable<TemperatureMeasurementDto>>(x.Measurements)
                        .OrderBy(m => m.Fahrenheit)
                        .Max(m => m.Fahrenheit)
                    - 32m) * 5m / 9m
            });
        }
    }

    private class ProjectToQueryProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<ProjectToQuerySource, ProjectToQueryTarget>(x => new ProjectToQueryTarget {
                Value = x.Value + 1
            });
        }
    }

    private class ProjectToAddressProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<ProjectToAddressSource, ProjectToAddressTarget>(x => new ProjectToAddressTarget {
                StreetName = x.StreetName + "-mapped"
            });
        }
    }

    private class ProjectToPersonProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<ProjectToPersonSource, ProjectToPersonTarget>(x => new ProjectToPersonTarget {
                Addresses = x.Addresses.ProjectTo<ProjectToAddressTarget>().ToArray()
            });
        }
    }

    private class ProjectToNamedPhoneProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<ProjectToNamedPhoneSource, ProjectToNamedPhoneTarget>("Raw", x => new ProjectToNamedPhoneTarget {
                Number = x.Number
            });

            CreateMap<ProjectToNamedPhoneSource, ProjectToNamedPhoneTarget>("Masked", x => new ProjectToNamedPhoneTarget {
                Number = x.Number + " [MASKED]"
            });
        }
    }

    private class ProjectToNamedPersonProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<ProjectToNamedPersonSource, ProjectToNamedPersonTarget>("Raw", x => new ProjectToNamedPersonTarget {
                Phones = x.Phones.ProjectTo<ProjectToNamedPhoneTarget>("Raw").ToArray()
            });

            CreateMap<ProjectToNamedPersonSource, ProjectToNamedPersonTarget>("Masked", x => new ProjectToNamedPersonTarget {
                Phones = x.Phones.ProjectTo<ProjectToNamedPhoneTarget>("Masked").ToArray()
            });
        }
    }

    [Fact]
    public void Map_ToNewObject_ShouldWorkLikeStaticMapper() {
        var mapify = new Mapify();
        var source = new Source { Id = 10, Name = "Alice" };

        var target = mapify.Map<Source, Target>(source, useDefaultMapIfTypeMapIsMissing: true);

        Assert.Equal(source.Id, target.Id);
        Assert.Equal(source.Name, target.Name);
    }

    [Fact]
    public void Map_ToExistingObject_ShouldWorkLikeStaticMapper() {
        var mapify = new Mapify();
        var source = new Source { Id = 22, Name = "Bob" };
        var target = new Target { Id = 1, Name = "Initial" };

        mapify.Map(source, target, useDefaultMapIfTypeMapIsMissing: true);

        Assert.Equal(22, target.Id);
        Assert.Equal("Bob", target.Name);
    }

    [Fact]
    public void Constructor_ShouldConfigureAllProfiles() {
        var mapify = new Mapify([new ProfileA(), new ProfileB()]);

        var aResult = mapify.Map<SourceA, TargetA>(new SourceA { ValueA = 7 });
        var bResult = mapify.Map<SourceB, TargetB>(new SourceB { ValueB = "ok" });

        Assert.Equal(7, aResult.ValueA);
        Assert.Equal("ok", bResult.ValueB);
    }

    [Fact]
    public void UseDefaultMapIfTypeMapIsMissing_ShouldBeInstanceScoped() {
        var mapify1 = new Mapify();
        var mapify2 = new Mapify();

        mapify1.UseDefaultMapIfTypeMapIsMissing(true);

        var result = mapify1.Map<SourceA, TargetA>(new SourceA { ValueA = 3 });
        Assert.Equal(3, result.ValueA);

        Assert.Throws<ArgumentException>(() => mapify2.Map<SourceA, TargetA>(new SourceA { ValueA = 3 }));
    }

    [Fact]
    public void Constructor_ShouldAllowNullProfileSequence() {
        var mapify = new Mapify((IEnumerable<MapifyProfile>?)null);

        var mapped = mapify.Map<SourceA, TargetA>(new SourceA { ValueA = 9 }, useDefaultMapIfTypeMapIsMissing: true);

        Assert.Equal(9, mapped.ValueA);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenWhitespaceMapNameIsConfigured() {
        FaultProfile.Mode = FaultProfileMode.WhitespaceName;
        try {
            Assert.Throws<ArgumentException>(() => new Mapify(new FaultProfile()));
        } finally {
            FaultProfile.Mode = FaultProfileMode.None;
        }
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenUseMapHasMissingMap_DirectBinding() {
        FaultProfile.Mode = FaultProfileMode.UseMapMissingDirect;
        try {
            Assert.ThrowsAny<Exception>(() => new Mapify(new FaultProfile()));
        } finally {
            FaultProfile.Mode = FaultProfileMode.None;
        }
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenUseMapNameIsNotConstant_DirectBinding() {
        FaultProfile.Mode = FaultProfileMode.UseMapInvalidNameDirect;
        try {
            Assert.ThrowsAny<Exception>(() => new Mapify(new FaultProfile()));
        } finally {
            FaultProfile.Mode = FaultProfileMode.None;
        }
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenUseMapHasMissingMap_NestedCall() {
        FaultProfile.Mode = FaultProfileMode.UseMapNoMapNested;
        try {
            Assert.ThrowsAny<Exception>(() => new Mapify(new FaultProfile()));
        } finally {
            FaultProfile.Mode = FaultProfileMode.None;
        }
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenUseMapNameIsNotConstant_NestedCall() {
        FaultProfile.Mode = FaultProfileMode.UseMapInvalidNameNested;
        try {
            Assert.ThrowsAny<Exception>(() => new Mapify(new FaultProfile()));
        } finally {
            FaultProfile.Mode = FaultProfileMode.None;
        }
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenUseMapIsBoundToField() {
        FaultProfile.Mode = FaultProfileMode.UseMapFieldBinding;
        try {
            Assert.ThrowsAny<Exception>(() => new Mapify(new FaultProfile()));
        } finally {
            FaultProfile.Mode = FaultProfileMode.None;
        }
    }

    [Fact]
    public void Constructor_ShouldSupportCoalesceBindingsInPartialMap() {
        FaultProfile.Mode = FaultProfileMode.CoalesceBinding;
        try {
            var mapify = new Mapify(new FaultProfile());
            var mapped = mapify.Map<FaultSource, FaultTarget>(new FaultSource { MaybeValue = null });
            Assert.Equal(123, mapped.Value);
        } finally {
            FaultProfile.Mode = FaultProfileMode.None;
        }
    }

    [Fact]
    public void Configurator_CreateMapNamed_ShouldThrow_WhenNameIsWhitespace() {
        var mapify = new Mapify();
        var method = typeof(Mapify)
            .GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            .Single(x => x.Name.Contains("IMapifyConfigurator.CreateMap")
                && x.GetParameters().Length == 2
                && x.GetParameters()[0].ParameterType == typeof(string));

        var generic = method.MakeGenericMethod(typeof(SourceA), typeof(TargetA));
        var ex = Assert.Throws<System.Reflection.TargetInvocationException>(() => generic.Invoke(mapify, [" ", null]));

        Assert.IsType<ArgumentException>(ex.InnerException);
    }

    [Fact]
    public void Map_ShouldSupportValueMappingsFromProfiles() {
        var mapify = new Mapify(new ValueMapProfile());

        var mappedName = mapify.Map<NameSource, string>(new NameSource { Name = "Mapify" });
        var mappedEnum = mapify.Map<SourceStatus, TargetStatus>(SourceStatus.Active);

        Assert.Equal("Mapify", mappedName);
        Assert.Equal(TargetStatus.Enabled, mappedEnum);
    }

    [Fact]
    public void Map_ToExisting_ShouldThrowForValueMappingInInstanceMapper() {
        var mapify = new Mapify(new ValueMapProfile());
        var source = new NameSource { Name = "Mapify" };
        var target = string.Empty;

        Assert.Throws<NotSupportedException>(() => mapify.Map(source, target));
    }

    [Fact]
    public void Map_ToExisting_DefaultMap_ShouldWorkAcrossRepeatedCalls() {
        var mapify = new Mapify();
        var target = new TargetA();

        mapify.Map(new SourceA { ValueA = 2 }, target, useDefaultMapIfTypeMapIsMissing: true);
        mapify.Map(new SourceA { ValueA = 5 }, target, useDefaultMapIfTypeMapIsMissing: true);

        Assert.Equal(5, target.ValueA);
    }

    [Fact]
    public void Map_ToExisting_NamedObjectMap_ShouldWorkAcrossRepeatedCalls() {
        var mapify = new Mapify(new NamedObjectProfile());
        var target = new TargetA();

        mapify.Map(new SourceA { ValueA = 1 }, target, "NamedObj");
        mapify.Map(new SourceA { ValueA = 4 }, target, "NamedObj");

        Assert.Equal(5, target.ValueA);
    }

    [Fact]
    public void Map_ToExisting_NamedObjectMap_ShouldCompileWhenExistingCacheIsCleared() {
        var mapify = new Mapify(new NamedObjectProfile());
        ClearPrivateDictionary(mapify, "_compiledMapToExistingCache");

        var target = new TargetA();
        mapify.Map(new SourceA { ValueA = 7 }, target, "NamedObj");

        Assert.Equal(8, target.ValueA);
    }

    [Fact]
    public void Map_ToNew_NamedObjectMap_ShouldCompileWhenNewCacheIsCleared() {
        var mapify = new Mapify(new NamedObjectProfile());
        ClearPrivateDictionary(mapify, "_compiledMapToNewCache");

        var mapped = mapify.Map<SourceA, TargetA>(new SourceA { ValueA = 10 }, "NamedObj");

        Assert.Equal(11, mapped.ValueA);
    }

    private static void ClearPrivateDictionary(Mapify mapify, string fieldName) {
        var field = typeof(Mapify).GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        var dictionary = field.GetValue(mapify)!;
        dictionary.GetType().GetMethod("Clear")!.Invoke(dictionary, null);
    }

    [Fact]
    public void Map_ShouldUseExistingNestedMapImplicitly_WhenPropertyTypesDiffer() {
        var mapify = new Mapify(new ChildProfile(), new ParentProfile());

        var source = new ParentSource {
            Child = new ChildSource { Number = 10 }
        };

        var mapped = mapify.Map<ParentSource, ParentTarget>(source);

        Assert.NotNull(mapped.Child);
        Assert.Equal(11, mapped.Child.Number);
    }

    [Fact]
    public void Map_ShouldUseLateRegisteredNestedMapImplicitly_AfterBuild() {
        // Parent profile is applied before Child profile on purpose.
        var mapify = new Mapify(new ParentProfile(), new ChildProfile());

        var source = new ParentSource {
            Child = new ChildSource { Number = 41 }
        };

        var mapped = mapify.Map<ParentSource, ParentTarget>(source);

        Assert.NotNull(mapped.Child);
        Assert.Equal(42, mapped.Child.Number);
    }

    [Fact]
    public void Map_ShouldLiftNonNullableMap_ForAllNullableVariants() {
        var mapify = new Mapify(new NumberContainerProfile(), new NumberProfile());

        var r1 = mapify.Map<NumberContainerSrcToTarget, NumberContainerTarget>(
            new NumberContainerSrcToTarget { Number = new NumberSource { Value = 1 } }
        );
        var r2 = mapify.Map<NumberContainerSrcToNullableTarget, NumberContainerNullableTarget>(
            new NumberContainerSrcToNullableTarget { Number = new NumberSource { Value = 2 } }
        );
        var r3Value = mapify.Map<NumberContainerNullableSrcToTarget, NumberContainerTarget>(
            new NumberContainerNullableSrcToTarget { Number = new NumberSource { Value = 3 } }
        );
        var r3Null = mapify.Map<NumberContainerNullableSrcToTarget, NumberContainerTarget>(
            new NumberContainerNullableSrcToTarget { Number = null }
        );
        var r4Value = mapify.Map<NumberContainerNullableSrcToNullableTarget, NumberContainerNullableTarget>(
            new NumberContainerNullableSrcToNullableTarget { Number = new NumberSource { Value = 4 } }
        );
        var r4Null = mapify.Map<NumberContainerNullableSrcToNullableTarget, NumberContainerNullableTarget>(
            new NumberContainerNullableSrcToNullableTarget { Number = null }
        );

        Assert.Equal(2, r1.Number.Value);
        Assert.NotNull(r2.Number);
        Assert.Equal(3, r2.Number!.Value.Value);

        Assert.Equal(4, r3Value.Number.Value);
        Assert.Equal(default(NumberTarget), r3Null.Number);

        Assert.NotNull(r4Value.Number);
        Assert.Equal(5, r4Value.Number!.Value.Value);
        Assert.Null(r4Null.Number);
    }

    [Fact]
    public void Map_ShouldBuildTransitiveDependencies_WhenProfilesAreUnordered() {
        // Registration order is reverse dependency order: root -> middle -> leaf.
        var mapify = new Mapify(new RootProfile(), new MiddleProfile(), new LeafProfile());

        var source = new RootSource {
            Middle = new MiddleSource {
                Leaf = new LeafSource { Number = 10 }
            }
        };

        var mapped = mapify.Map<RootSource, RootTarget>(source);

        Assert.NotNull(mapped.Middle);
        Assert.NotNull(mapped.Middle.Leaf);
        Assert.Equal(11, mapped.Middle.Leaf.Number);
    }

    [Fact]
    public void Map_ShouldResolveUseMapMarker_WhenDependencyIsRegisteredLater() {
        var mapify = new Mapify(new ParentProfileWithUseMapMarker(), new MarkerChildProfile());

        var source = new MarkerParentSource {
            Child = new MarkerChildSource { Number = 12 }
        };

        var mapped = mapify.Map<MarkerParentSource, MarkerParentTarget>(source);

        Assert.NotNull(mapped.Child);
        Assert.Equal(13, mapped.Child.Number);
    }

    [Fact]
    public void Map_UseMapMarker_ShouldLiftNonNullableMap_ForAllNullableVariants() {
        // Intentionally register container maps before the underlying Number map.
        var mapify = new Mapify(new NumberContainerUseMapProfile(), new NumberProfile());

        var r1 = mapify.Map<UseMapContainerSrcToTarget, UseMapContainerTarget>(
            new UseMapContainerSrcToTarget { Number = new NumberSource { Value = 10 } }
        );
        var r2 = mapify.Map<UseMapContainerSrcToNullableTarget, UseMapContainerNullableTarget>(
            new UseMapContainerSrcToNullableTarget { Number = new NumberSource { Value = 20 } }
        );
        var r3Value = mapify.Map<UseMapContainerNullableSrcToTarget, UseMapContainerTarget>(
            new UseMapContainerNullableSrcToTarget { Number = new NumberSource { Value = 30 } }
        );
        var r3Null = mapify.Map<UseMapContainerNullableSrcToTarget, UseMapContainerTarget>(
            new UseMapContainerNullableSrcToTarget { Number = null }
        );
        var r4Value = mapify.Map<UseMapContainerNullableSrcToNullableTarget, UseMapContainerNullableTarget>(
            new UseMapContainerNullableSrcToNullableTarget { Number = new NumberSource { Value = 40 } }
        );
        var r4Null = mapify.Map<UseMapContainerNullableSrcToNullableTarget, UseMapContainerNullableTarget>(
            new UseMapContainerNullableSrcToNullableTarget { Number = null }
        );

        Assert.Equal(11, r1.Number.Value);

        Assert.NotNull(r2.Number);
        Assert.Equal(21, r2.Number!.Value.Value);

        Assert.Equal(31, r3Value.Number.Value);
        Assert.Equal(default(NumberTarget), r3Null.Number);

        Assert.NotNull(r4Value.Number);
        Assert.Equal(41, r4Value.Number!.Value.Value);
        Assert.Null(r4Null.Number);
    }

    [Fact]
    public void Map_UseMapMarkerWithSourceArgument_ShouldLiftNonNullableMap_ForAllNullableVariants() {
        // Intentionally register container maps before the underlying Number map.
        var mapify = new Mapify(new NumberContainerUseMapWithSourceArgProfile(), new NumberProfile());

        var r1 = mapify.Map<NumberContainerSrcPropToTarget, NumberContainerTargetNamed>(
            new NumberContainerSrcPropToTarget { SourceNumber = new NumberSource { Value = 100 } }
        );
        var r2 = mapify.Map<NumberContainerSrcPropToNullableTarget, NumberContainerNullableTargetNamed>(
            new NumberContainerSrcPropToNullableTarget { SourceNumber = new NumberSource { Value = 200 } }
        );
        var r3Value = mapify.Map<NumberContainerNullableSrcPropToTarget, NumberContainerTargetNamed>(
            new NumberContainerNullableSrcPropToTarget { SourceNumber = new NumberSource { Value = 300 } }
        );
        var r3Null = mapify.Map<NumberContainerNullableSrcPropToTarget, NumberContainerTargetNamed>(
            new NumberContainerNullableSrcPropToTarget { SourceNumber = null }
        );
        var r4Value = mapify.Map<NumberContainerNullableSrcPropToNullableTarget, NumberContainerNullableTargetNamed>(
            new NumberContainerNullableSrcPropToNullableTarget { SourceNumber = new NumberSource { Value = 400 } }
        );
        var r4Null = mapify.Map<NumberContainerNullableSrcPropToNullableTarget, NumberContainerNullableTargetNamed>(
            new NumberContainerNullableSrcPropToNullableTarget { SourceNumber = null }
        );

        Assert.Equal(101, r1.Number.Value);

        Assert.NotNull(r2.Number);
        Assert.Equal(201, r2.Number!.Value.Value);

        Assert.Equal(301, r3Value.Number.Value);
        Assert.Equal(default(NumberTarget), r3Null.Number);

        Assert.NotNull(r4Value.Number);
        Assert.Equal(401, r4Value.Number!.Value.Value);
        Assert.Null(r4Null.Number);
    }

    [Fact]
    public void Map_UseMapMarker_ShouldSupportArraysAndEnumerables_FromElementMap() {
        var mapify = new Mapify(new CollectionUseMapProfile(), new ElementProfile());

        var source = new CollectionUseMapSource {
            ItemsArray = [
                new ElementSource { Value = 1 },
                new ElementSource { Value = 2 }
            ],
            ItemsList = [
                new ElementSource { Value = 3 },
                new ElementSource { Value = 4 }
            ]
        };

        var mapped = mapify.Map<CollectionUseMapSource, CollectionUseMapTarget>(source);

        Assert.Equal([2, 3], mapped.ItemsArray.Select(x => x.Value).ToArray());
        Assert.Equal([4, 5], mapped.ItemsList.Select(x => x.Value).ToArray());
        Assert.Equal([2, 3], mapped.ItemsEnumerable.Select(x => x.Value).ToArray());
        Assert.Equal([4, 5], mapped.ItemsCollection.Select(x => x.Value).ToArray());
        Assert.Equal([2, 3], mapped.ItemsArrayAsList.Select(x => x.Value).ToArray());
    }

    [Fact]
    public void Map_ShouldImplicitlyMapPrimitiveArraysAndCollections_WithoutUseMap() {
        var mapify = new Mapify(new ImplicitPrimitiveCollectionsProfile());

        var source = new ImplicitPrimitiveCollectionsSource {
            Numbers = [1, 2, 3],
            Texts = ["a", "b"]
        };

        var mapped = mapify.Map<ImplicitPrimitiveCollectionsSource, ImplicitPrimitiveCollectionsTarget>(source);

        Assert.Equal(new[] { 1, 2, 3 }, mapped.Numbers);
        Assert.Equal(new[] { "a", "b" }, mapped.Texts);
    }

    [Fact]
    public void Map_ShouldImplicitlyUseExistingElementMap_ForArrayMembersWithoutUseMap() {
        var mapify = new Mapify(new ImplicitCollectionParentProfile(), new ElementProfile());

        var source = new ImplicitCollectionParentSource {
            Items = [
                new ElementSource { Value = 1 },
                new ElementSource { Value = 2 }
            ]
        };

        var mapped = mapify.Map<ImplicitCollectionParentSource, ImplicitCollectionParentTarget>(source);

        Assert.Equal([2, 3], mapped.Items.Select(x => x.Value).ToArray());
    }

    [Fact]
    public void Map_UseMapMarker_ShouldAcceptSourceExpressions_SuchAsWhereFilters() {
        var mapify = new Mapify(new FilterClassProfile(), new FilterStudentProfile());

        var source = new FilterClassSource {
            Students = new[] {
                new FilterStudentSource { Name = "Alice" },
                new FilterStudentSource { Name = null },
                new FilterStudentSource { Name = "Bob" }
            }
        };

        var mapped = mapify.Map<FilterClassSource, FilterClassTarget>(source);

        Assert.Equal(new[] { "Alice", "Bob" }, mapped.Students.Select(x => x.Name).ToArray());
    }

    [Fact]
    public void Map_ShouldSupportNamedMappings_ForSameSourceAndTargetTypes() {
        var mapify = new Mapify(new NamedPersonValueMapsProfile());
        var person = new NamedPerson { FirstName = "Ada", LastName = "Lovelace" };

        var defaultValue = mapify.Map<NamedPerson, string>(person);
        var fullName = mapify.Map<NamedPerson, string>(person, "FullName");
        var initials = mapify.Map<NamedPerson, string>(person, "Initials");

        Assert.Equal("Ada", defaultValue);
        Assert.Equal("Ada Lovelace", fullName);
        Assert.Equal("AL", initials);
    }

    [Fact]
    public void GetMap_ShouldReturnNamedMapping_WhenNameIsProvided() {
        var mapify = new Mapify(new NamedPersonValueMapsProfile());
        var person = new NamedPerson { FirstName = "Grace", LastName = "Hopper" };

        var fullNameMap = mapify.GetMap<NamedPerson, string>("FullName");
        Assert.NotNull(fullNameMap);
        var mapped = fullNameMap.Compile().Invoke(person);

        Assert.Equal("Grace Hopper", mapped);
    }

    [Fact]
    public void GetMap_Default_ShouldReturnNull_WhenMapMissingAndDefaultDisabled() {
        var mapify = new Mapify();
        mapify.UseDefaultMapIfTypeMapIsMissing(false);

        var map = mapify.GetMap<SourceA, TargetA>();

        Assert.Null(map);
    }

    [Fact]
    public void GetRequiredMap_Default_ShouldThrow_WhenMapMissingAndDefaultDisabled() {
        var mapify = new Mapify();
        mapify.UseDefaultMapIfTypeMapIsMissing(false);

        Assert.Throws<ArgumentException>(() => mapify.GetRequiredMap<SourceA, TargetA>());
    }

    [Fact]
    public void GetMap_Named_ShouldReturnNull_WhenNamedMappingIsMissing() {
        var mapify = new Mapify(new NamedPersonValueMapsProfile());

        var map = mapify.GetMap<NamedPerson, string>("UnknownName");

        Assert.Null(map);
    }

    [Fact]
    public void GetRequiredMap_Named_ShouldThrow_WhenNamedMappingIsMissing() {
        var mapify = new Mapify(new NamedPersonValueMapsProfile());

        Assert.Throws<ArgumentException>(() => mapify.GetRequiredMap<NamedPerson, string>("UnknownName"));
    }

    [Fact]
    public void Map_Named_ShouldThrow_WhenNamedMappingIsMissing() {
        var mapify = new Mapify(new NamedPersonValueMapsProfile());
        var person = new NamedPerson { FirstName = "Katherine", LastName = "Johnson" };

        Assert.Throws<ArgumentException>(() => mapify.Map<NamedPerson, string>(person, "UnknownName"));
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenDuplicateNamedMappingIsRegistered() {
        Assert.Throws<ArgumentException>(() => new Mapify(new NamedPersonValueMapsProfile(), new NamedPersonValueMapsProfile()));
    }

    [Fact]
    public void Map_Named_ToExisting_ShouldThrowForValueMappingInInstanceMapper() {
        var mapify = new Mapify(new NamedValueOnlyProfile());
        var source = new NameSource { Name = "Mapify" };
        var target = string.Empty;

        Assert.Throws<NotSupportedException>(() => mapify.Map(source, target, "ByName"));
    }

    [Fact]
    public void GetMap_Named_ShouldThrow_WhenNameIsWhitespace() {
        var mapify = new Mapify(new NamedPersonValueMapsProfile());

        Assert.Throws<ArgumentException>(() => mapify.GetMap<NamedPerson, string>(" "));
    }

    [Fact]
    public void GetRequiredMap_Named_ShouldThrow_WhenNameIsWhitespace() {
        var mapify = new Mapify(new NamedPersonValueMapsProfile());

        Assert.Throws<ArgumentException>(() => mapify.GetRequiredMap<NamedPerson, string>(" "));
    }

    [Fact]
    public void Map_Named_ShouldThrow_WhenNameIsWhitespace() {
        var mapify = new Mapify(new NamedPersonValueMapsProfile());

        Assert.Throws<ArgumentException>(() => mapify.Map<NamedPerson, string>(new NamedPerson(), " "));
    }

    [Fact]
    public void Map_ToExisting_Named_ShouldThrow_WhenNameIsWhitespace() {
        var mapify = new Mapify(new NamedValueOnlyProfile());

        Assert.Throws<ArgumentException>(() => mapify.Map(new NameSource { Name = "x" }, string.Empty, " "));
    }

    [Fact]
    public void GetMap_Default_ShouldReturnCachedDefaultExpression_OnSecondCall() {
        var mapify = new Mapify();
        mapify.UseDefaultMapIfTypeMapIsMissing(true);

        var first = mapify.GetMap<SourceA, TargetA>();
        var second = mapify.GetMap<SourceA, TargetA>();

        Assert.Same(first, second);
    }

    [Fact]
    public void Map_UseMapMarker_ShouldSupportNamedMappings() {
        var mapify = new Mapify(new NamedClassProfile(), new NamedStudentProfile());

        var source = new NamedClassSource {
            Students = new[] {
                new NamedStudentSource { Name = "Alice" },
                new NamedStudentSource { Name = "Bob" }
            }
        };

        var mapped = mapify.Map<NamedClassSource, NamedClassTarget>(source);

        Assert.Equal(new[] { "ALICE", "BOB" }, mapped.StudentsUpper.Select(x => x.Name).ToArray());
        Assert.Equal(new[] { "alice", "bob" }, mapped.StudentsLower.Select(x => x.Name).ToArray());
    }

    [Fact]
    public void Map_UseMapMarker_ShouldAllowChaining_AfterUseMap() {
        var mapify = new Mapify(new ChainedAddressProfile(), new ChainedPersonProfile());

        var source = new ChainedPersonSource {
            Addresses = new[] {
                new ChainedAddressSource { StreetName = "B-Street" },
                new ChainedAddressSource { StreetName = "A-Street" }
            }
        };

        var mapped = mapify.Map<ChainedPersonSource, ChainedPersonTarget>(source);

        Assert.Equal(new[] { "A-Street", "B-Street" }, mapped.Addresses.Select(x => x.StreetName).ToArray());
    }

    [Fact]
    public void Map_UseMapMarkerNamed_ShouldAllowChaining_AfterUseMap() {
        var mapify = new Mapify(new ChainedNamedAddressProfile(), new ChainedNamedPersonProfile());

        var source = new ChainedPersonSource {
            Addresses = new[] {
                new ChainedAddressSource { StreetName = "abc" },
                new ChainedAddressSource { StreetName = "xyz" }
            }
        };

        var mapped = mapify.Map<ChainedPersonSource, ChainedNamedPersonTarget>(source);

        Assert.Equal(new[] { "cba", "zyx" }, mapped.Addresses.Select(x => x.StreetName).ToArray());
    }

    [Fact]
    public void Map_UseMapMarker_ShouldSupportCalculations() {
        var mapify = new Mapify(new CalculationNumberProfile(), new CalculationPersonProfile());

        var mapped = mapify.Map<CalculationPersonSource, CalculationPersonTarget>(new CalculationPersonSource {
            AgeInYears = 2
        });

        Assert.Equal(730, mapped.AgeInDays);
    }

    [Fact]
    public void Map_UseMapMarker_ShouldSupportComplexCalculation_WithOrderingAndMax() {
        var mapify = new Mapify(new TemperatureMeasurementProfile(), new TemperatureSeriesProfile());

        var source = new TemperatureSeriesSource {
            Measurements = new[] {
                new TemperatureMeasurementSource { Fahrenheit = 50m },
                new TemperatureMeasurementSource { Fahrenheit = 41m },
                new TemperatureMeasurementSource { Fahrenheit = 68m }
            }
        };

        var mapped = mapify.Map<TemperatureSeriesSource, TemperatureSeriesTarget>(source);

        Assert.Equal(20m, mapped.MaxTemperatureCelsius);
    }

    [Fact]
    public void ProjectTo_IQueryable_ShouldUseInstanceMapper_AndAllowFurtherQueryComposition() {
        var mapify = new Mapify(new ProjectToQueryProfile());

        var projectedValues = new[] {
                new ProjectToQuerySource { Value = 1 },
                new ProjectToQuerySource { Value = 2 },
                new ProjectToQuerySource { Value = 3 }
            }
            .AsQueryable()
            .ProjectTo<ProjectToQueryTarget>(mapify)
            .Where(x => x.Value > 2)
            .Select(x => x.Value)
            .ToArray();

        Assert.Equal([3, 4], projectedValues);
    }

    [Fact]
    public void CreateMap_ShouldTranslateNestedProjectToMarker_ToUseExistingMap() {
        var mapify = new Mapify(new ProjectToPersonProfile(), new ProjectToAddressProfile());

        var mapped = mapify.Map<ProjectToPersonSource, ProjectToPersonTarget>(new ProjectToPersonSource {
            Addresses = [
                new ProjectToAddressSource { StreetName = "A" },
                new ProjectToAddressSource { StreetName = "B" }
            ]
        });

        Assert.Equal(["A-mapped", "B-mapped"], mapped.Addresses.Select(x => x.StreetName).ToArray());
    }

    [Fact]
    public void ProjectTo_IQueryable_Named_ShouldUseInstanceMapperNamedMap() {
        var mapify = new Mapify(new ProjectToNamedPhoneProfile(), new ProjectToNamedPersonProfile());

        var projected = new[] {
                new ProjectToNamedPersonSource {
                    Phones = [
                        new ProjectToNamedPhoneSource { Number = "+1-100" }
                    ]
                }
            }
            .AsQueryable()
            .ProjectTo<ProjectToNamedPersonTarget>(mapify, "Masked")
            .Single();

        Assert.Equal(["+1-100 [MASKED]"], projected.Phones.Select(x => x.Number).ToArray());
    }

    [Fact]
    public void CreateMap_ShouldTranslateNestedNamedProjectToMarker_ToNamedUseMap() {
        var mapify = new Mapify(new ProjectToNamedPersonProfile(), new ProjectToNamedPhoneProfile());

        var mapped = mapify.Map<ProjectToNamedPersonSource, ProjectToNamedPersonTarget>(
            new ProjectToNamedPersonSource {
                Phones = [
                    new ProjectToNamedPhoneSource { Number = "+1-200" }
                ]
            },
            "Masked"
        );

        Assert.Equal(["+1-200 [MASKED]"], mapped.Phones.Select(x => x.Number).ToArray());
    }
}
