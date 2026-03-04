using System.Linq.Expressions;

namespace Mapify.NET.Tests;

public partial class MapperTests {
    [Fact]
    public void CreateMap_ShouldRemoveIgnoredOptionalPropertyBinding() {
        var map = CreateMapWithResolver<IgnoreSource, IgnoreTarget>(IgnoreMarkerProfile.CreateOptionalIgnorePartial(), null);

        var init = Assert.IsType<MemberInitExpression>(map.Body);
        Assert.DoesNotContain(init.Bindings, x => x.Member.Name == nameof(IgnoreTarget.Ignored));

        var mapped = map.Map(new IgnoreSource { Included = 7, Ignored = 99 });
        Assert.Equal(7, mapped.Included);
        Assert.Equal(default, mapped.Ignored);
    }

    [Fact]
    public void CreateAndAddMap_WithIgnoreMarker_ShouldIgnorePropertyInStaticMapper() {
        Mapper.CreateAndAddMap<IgnoreSource, IgnoreTarget>(IgnoreMarkerProfile.CreateOptionalIgnorePartial());

        var mapped = Mapper.Map<IgnoreSource, IgnoreTarget>(new IgnoreSource { Included = 7, Ignored = 99 });

        Assert.Equal(7, mapped.Included);
        Assert.Equal(default, mapped.Ignored);
    }

    [Fact]
    public void CreateAndAddMap_WithIgnoreMarker_ShouldSkipIgnoredPropertyWhenMappingToExistingInStaticMapper() {
        Mapper.CreateAndAddMap<IgnoreSource, IgnoreTarget>(IgnoreMarkerProfile.CreateOptionalIgnorePartial());

        var target = new IgnoreTarget { Included = 1, Ignored = 123 };
        Mapper.Map(new IgnoreSource { Included = 10, Ignored = 999 }, target);

        Assert.Equal(10, target.Included);
        Assert.Equal(123, target.Ignored);
    }

    [Fact]
    public void CreateMap_ShouldBindDefaultValue_WhenRequiredPropertyIsIgnored() {
        var map = CreateMapWithResolver<IgnoreSource, IgnoreRequiredTarget>(IgnoreMarkerProfile.CreateRequiredIgnorePartial(), null);
        var mapped = map.Map(new IgnoreSource { Included = 7, Ignored = 99 });

        Assert.Equal(7, mapped.Included);
        Assert.Equal(default, mapped.Ignored);
    }

    [Fact]
    public void CompileMapper_ShouldSkipIgnoredProperties_WhenMappingToExistingObject() {
        var map = CreateMapWithResolver<IgnoreSource, IgnoreRequiredTarget>(IgnoreMarkerProfile.CreateRequiredIgnorePartial(), null);
        var action = Mapper.CompileMapper(map);

        var target = new IgnoreRequiredTarget { Included = 1, Ignored = 123 };
        action(new IgnoreSource { Included = 10, Ignored = 999 }, target);

        Assert.Equal(10, target.Included);
        Assert.Equal(123, target.Ignored);
    }
}