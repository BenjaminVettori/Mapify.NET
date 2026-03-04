namespace Mapify.NET.Tests;

public partial class MapperTests {
    [Fact]
    public void GetMap_ShouldReturnExpression() {
        var source = new A2 { Prop = 10 };

        Mapper.UseDefaultMapIfTypeMapIsMissing(true);
        var expr = Mapper.GetMap<A2, B2>();
        Assert.NotNull(expr);

        var func = expr!.Compile();
        var target = func(source);

        Assert.Equal(10, target.Prop);
    }

    [Fact]
    public void GetMap_ShouldReturnCachedDefaultExpression_OnSecondCall() {
        Mapper.UseDefaultMapIfTypeMapIsMissing(true);
        var expr1 = Mapper.GetMap<A2, B2>();
        var expr2 = Mapper.GetMap<A2, B2>();

        Assert.NotNull(expr1);
        Assert.Same(expr1, expr2);
    }

    [Fact]
    public void GetMap_ShouldReturnNull_WhenMapMissingAndDefaultDisabled() {
        Mapper.UseDefaultMapIfTypeMapIsMissing(false);

        var expr = Mapper.GetMap<A4, B4>();

        Assert.Null(expr);
    }

    [Fact]
    public void GetRequiredMap_ShouldThrow_WhenMapMissingAndDefaultDisabled() {
        Mapper.UseDefaultMapIfTypeMapIsMissing(false);

        Assert.Throws<ArgumentException>(() => Mapper.GetRequiredMap<A4, B4>());
    }

    [Fact]
    public void GetRequiredMap_WithParameters_ShouldResolveParameterMarkerFromStaticMap() {
        Mapper.AddMap(ParameterMarkerProfile.CreateParameterizedMap());

        var map = Mapper.GetRequiredMap<A2, B2>(new Dictionary<string, object?> { ["offset"] = 7 });
        var mapped = map.Compile().Invoke(new A2 { Prop = 2 });

        Assert.Equal(9, mapped.Prop);
    }

    [Fact]
    public void GetRequiredMap_WithParameters_ShouldThrow_WhenParameterMarkerValueIsMissing() {
        Mapper.AddMap(ParameterMarkerProfile.CreateParameterizedMap());

        var ex = Assert.Throws<KeyNotFoundException>(() => Mapper.GetRequiredMap<A2, B2>());
        Assert.Contains("offset", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void GetMap_ShouldRequireExactTypes_AndNotReturnNullableOrElementFallbackMaps() {
        Mapper.UseDefaultMapIfTypeMapIsMissing(false);
        Mapper.AddMap<PrecedenceNumberSource, PrecedenceNumberTarget>(x => new PrecedenceNumberTarget { Value = x.Value + 1 });
        Mapper.AddMap<PrecedenceCollectionElementSource, PrecedenceCollectionElementTarget>(
            x => new PrecedenceCollectionElementTarget { Value = x.Value + 1 }
        );

        var nullableMap = Mapper.GetMap<PrecedenceNumberSource?, PrecedenceNumberTarget?>();
        var collectionMap = Mapper.GetMap<List<PrecedenceCollectionElementSource>, List<PrecedenceCollectionElementTarget>>();

        Assert.Null(nullableMap);
        Assert.Null(collectionMap);
        Assert.Throws<ArgumentException>(() => Mapper.GetRequiredMap<PrecedenceNumberSource?, PrecedenceNumberTarget?>());
        Assert.Throws<ArgumentException>(() => Mapper.GetRequiredMap<List<PrecedenceCollectionElementSource>, List<PrecedenceCollectionElementTarget>>());
    }
}