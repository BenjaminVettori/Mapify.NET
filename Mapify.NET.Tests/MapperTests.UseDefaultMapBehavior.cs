namespace Mapify.NET.Tests;

public partial class MapperTests {
    [Fact]
    public void Map_ShouldMapProperties_WhenAutoMapIsUsed() {
        var source = new Source { Id = 1, Name = "Test", Age = 25, Date = DateTime.Now };

        Mapper.UseDefaultMapIfTypeMapIsMissing(true);
        var target = Mapper.Map<Source, Target>(source);

        Assert.Equal(source.Id, target.Id);
        Assert.Equal(source.Name, target.Name);
        Assert.Equal(source.Age, target.Age);
        Assert.Equal(source.Date, target.Date);
    }

    [Fact]
    public void Map_ShouldThrowException_WhenMapMissingAndFlagFalse() {
        Mapper.UseDefaultMapIfTypeMapIsMissing(false);
        var source = new A2 { Prop = 5 };

        Assert.Throws<ArgumentException>(() => Mapper.Map<A2, B2>(source));
    }

    [Fact]
    public void Map_ShouldUseDefaultMap_WhenMapMissingAndFlagTrue() {
        var source = new A2 { Prop = 5 };

        Mapper.UseDefaultMapIfTypeMapIsMissing(true);
        var target = Mapper.Map<A2, B2>(source);

        Assert.Equal(5, target.Prop);
    }

    [Fact]
    public void Test_GlobalUseDefaultMapIfTypeMapIsMissing() {
        Mapper.UseDefaultMapIfTypeMapIsMissing(true);
        var source = new A3 { Prop = 5 };

        var target = Mapper.Map<A3, B3>(source);

        Assert.Equal(5, target.Prop);
    }
}