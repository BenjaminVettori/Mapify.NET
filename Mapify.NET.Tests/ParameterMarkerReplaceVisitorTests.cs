using System.Linq.Expressions;

namespace Mapify.NET.Tests;

[Collection("Mapper Tests")]
public class ParameterMarkerReplaceVisitorTests {
    public ParameterMarkerReplaceVisitorTests() {
        Mapper.UseDefaultMapIfTypeMapIsMissing(false);
        Mapper.ClearMappings();
    }

    [Fact]
    public void GetRequiredMap_WithNullableParameterType_ShouldAllowNullValue() {
        Mapper.AddMap(ParameterVisitorProfile.CreateNullableIntMap());

        var map = Mapper.GetRequiredMap<ParameterSource, NullableIntTarget>(new Dictionary<string, object?> {
            ["value"] = null
        });

        var mapped = map.Compile().Invoke(new ParameterSource());

        Assert.Null(mapped.Value);
    }

    [Fact]
    public void GetRequiredMap_WithNonNullableParameterType_ShouldThrowWhenValueIsNull() {
        Mapper.AddMap(ParameterVisitorProfile.CreateIntMap());

        var ex = Assert.Throws<InvalidOperationException>(() =>
            Mapper.GetRequiredMap<ParameterSource, IntTarget>(new Dictionary<string, object?> {
                ["value"] = null
            })
        );

        Assert.Contains("cannot be null", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetRequiredMap_WithStringValue_ShouldConvertToInt() {
        Mapper.AddMap(ParameterVisitorProfile.CreateIntMap());

        var map = Mapper.GetRequiredMap<ParameterSource, IntTarget>(new Dictionary<string, object?> {
            ["value"] = "12"
        });

        var mapped = map.Compile().Invoke(new ParameterSource());

        Assert.Equal(12, mapped.Value);
    }

    [Fact]
    public void GetRequiredMap_WithNullableIntParameter_ShouldAcceptUnderlyingIntValue() {
        Mapper.AddMap(ParameterVisitorProfile.CreateNullableIntMap());

        var map = Mapper.GetRequiredMap<ParameterSource, NullableIntTarget>(new Dictionary<string, object?> {
            ["value"] = 7
        });

        var mapped = map.Compile().Invoke(new ParameterSource());

        Assert.Equal(7, mapped.Value);
    }

    [Fact]
    public void GetRequiredMap_WithEnumParameter_ShouldParseFromStringIgnoringCase() {
        Mapper.AddMap(ParameterVisitorProfile.CreateEnumMap());

        var map = Mapper.GetRequiredMap<ParameterSource, EnumTarget>(new Dictionary<string, object?> {
            ["status"] = "active"
        });

        var mapped = map.Compile().Invoke(new ParameterSource());

        Assert.Equal(ParameterStatus.Active, mapped.Status);
    }

    [Fact]
    public void GetRequiredMap_WithEnumParameter_ShouldConvertFromNumericValue() {
        Mapper.AddMap(ParameterVisitorProfile.CreateEnumMap());

        var map = Mapper.GetRequiredMap<ParameterSource, EnumTarget>(new Dictionary<string, object?> {
            ["status"] = 2
        });

        var mapped = map.Compile().Invoke(new ParameterSource());

        Assert.Equal(ParameterStatus.Archived, mapped.Status);
    }

    [Fact]
    public void GetRequiredMap_WithIncompatibleParameterType_ShouldThrowInvalidOperationException() {
        Mapper.AddMap(ParameterVisitorProfile.CreateDateMap());

        var ex = Assert.Throws<InvalidOperationException>(() =>
            Mapper.GetRequiredMap<ParameterSource, DateTarget>(new Dictionary<string, object?> {
                ["date"] = new object()
            })
        );

        Assert.Contains("cannot be converted", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetRequiredMap_WithMissingParameter_ShouldThrowKeyNotFoundException() {
        Mapper.AddMap(ParameterVisitorProfile.CreateIntMap());

        var ex = Assert.Throws<KeyNotFoundException>(() =>
            Mapper.GetRequiredMap<ParameterSource, IntTarget>(new Dictionary<string, object?>())
        );

        Assert.Contains("Missing mapping parameter", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetRequiredMap_WithNullParameterBag_ShouldThrowKeyNotFoundException() {
        Mapper.AddMap(ParameterVisitorProfile.CreateIntMap());

        var ex = Assert.Throws<KeyNotFoundException>(() =>
            Mapper.GetRequiredMap<ParameterSource, IntTarget>((IReadOnlyDictionary<string, object?>?)null)
        );

        Assert.Contains("Missing mapping parameter", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetRequiredMap_WithNonConstantParameterNameExpression_ShouldThrowInvalidOperationException() {
        Mapper.AddMap(ParameterVisitorProfile.CreateNonConstantNameMap());

        var ex = Assert.Throws<InvalidOperationException>(() =>
            Mapper.GetRequiredMap<ParameterSource, IntTarget>(new Dictionary<string, object?> {
                ["value"] = 1
            })
        );

        Assert.Contains("constant string", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetRequiredMap_WithWhitespaceParameterName_ShouldThrowInvalidOperationException() {
        Mapper.AddMap(ParameterVisitorProfile.CreateWhitespaceNameMap());

        var ex = Assert.Throws<InvalidOperationException>(() =>
            Mapper.GetRequiredMap<ParameterSource, IntTarget>(new Dictionary<string, object?> {
                ["value"] = 1
            })
        );

        Assert.Contains("non-empty constant string", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class ParameterVisitorProfile : MapifyProfile {
        protected override void Configure() {
        }

        public static Expression<Func<ParameterSource, IntTarget>> CreateIntMap()
            => x => new IntTarget {
                Value = Parameter<int>("value")
            };

        public static Expression<Func<ParameterSource, NullableIntTarget>> CreateNullableIntMap()
            => x => new NullableIntTarget {
                Value = Parameter<int?>("value")
            };

        public static Expression<Func<ParameterSource, EnumTarget>> CreateEnumMap()
            => x => new EnumTarget {
                Status = Parameter<ParameterStatus>("status")
            };

        public static Expression<Func<ParameterSource, DateTarget>> CreateDateMap()
            => x => new DateTarget {
                Date = Parameter<DateTime>("date")
            };

        public static Expression<Func<ParameterSource, IntTarget>> CreateNonConstantNameMap()
            => x => new IntTarget {
                Value = Parameter<int>(x.Name)
            };

        public static Expression<Func<ParameterSource, IntTarget>> CreateWhitespaceNameMap()
            => x => new IntTarget {
                Value = Parameter<int>("   ")
            };
    }

    private sealed class ParameterSource {
        public string Name { get; set; } = "value";
    }

    private sealed class IntTarget {
        public int Value { get; set; }
    }

    private sealed class NullableIntTarget {
        public int? Value { get; set; }
    }

    private sealed class DateTarget {
        public DateTime Date { get; set; }
    }

    private sealed class EnumTarget {
        public ParameterStatus Status { get; set; }
    }

    private enum ParameterStatus {
        Unknown = 0,
        Active = 1,
        Archived = 2
    }
}
