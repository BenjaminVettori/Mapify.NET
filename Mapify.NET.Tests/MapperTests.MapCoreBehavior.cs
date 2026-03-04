using System.Linq.Expressions;

namespace Mapify.NET.Tests;

public partial class MapperTests {
    [Fact]
    public void AddMap_ShouldThrowException_WhenMappingAlreadyExists() {
        Mapper.AddMap<Source, TargetSubset>(s => new TargetSubset { Name = s.Name });

        Assert.Throws<ArgumentException>(() => Mapper.AddMap<Source, TargetSubset>(s => new TargetSubset { Name = s.Name }));
    }

    [Fact]
    public void Map_ShouldUseExplicitMap_WhenAdded() {
        Mapper.AddMap<A1, B1>(s => new B1 { Prop = s.Prop + 10 });
        var source = new A1 { Prop = 5 };

        var target = Mapper.Map<A1, B1>(source);

        Assert.Equal(15, target.Prop);
    }

    [Fact]
    public void Map_ToExistingObject_ShouldUpdateProperties() {
        var source = new Source { Id = 10, Name = "Updated" };
        var target = new Target { Id = 1, Name = "Original" };

        Mapper.UseDefaultMapIfTypeMapIsMissing(true);
        Mapper.Map(source, target);

        Assert.Equal(10, target.Id);
        Assert.Equal("Updated", target.Name);
    }

    [Fact]
    public void Map_ToExistingObject_WithExplicitMap() {
        var source = new A4 { Prop = 20 };
        var target = new B4 { Prop = 10 };

        Mapper.AddMap<A4, B4>(s => new B4 { Prop = s.Prop + 10 });

        Mapper.Map(source, target);

        Assert.Equal(30, target.Prop);
    }

    [Fact]
    public void Map_ShouldHandleNullableToNonNullable_WithValues() {
        var source = new SourceNullable { Value = 10 };

        Mapper.UseDefaultMapIfTypeMapIsMissing(true);
        var target = Mapper.Map<SourceNullable, TargetNonNullable>(source);

        Assert.Equal(10, target.Value);
    }

    [Fact]
    public void Map_ShouldHandleNullableToNonNullable_WithNull() {
        var source = new SourceNullable { Value = null };

        Mapper.UseDefaultMapIfTypeMapIsMissing(true);
        var target = Mapper.Map<SourceNullable, TargetNonNullable>(source);

        Assert.Equal(0, target.Value);
    }

    [Fact]
    public void Map_ShouldHandleNonNullableToNullable() {
        var source = new TargetNonNullable { Value = 10 };

        Mapper.UseDefaultMapIfTypeMapIsMissing(true);
        var target = Mapper.Map<TargetNonNullable, TargetNullable>(source);

        Assert.Equal(10, target.Value);
    }

    [Fact]
    public void CreateAndAddMap_ShouldCreateFullMap() {
        var source = new C1 { Id = 1, Name = "Test" };

        var createdMap = Mapper.CreateAndAddMap<C1, D1>(s => new D1 { Name = s.Name + "_" });
        var target = Mapper.Map<C1, D1>(source);

        Assert.NotNull(createdMap);
        Assert.Equal(1, target.Id);
        Assert.Equal("Test_", target.Name);
    }

    [Fact]
    public void Map_ShouldSupportEnumToEnumValueMapping() {
        Mapper.AddMap<SourceStatus, TargetStatus>(x => x == SourceStatus.Active ? TargetStatus.Enabled : TargetStatus.Disabled);

        var result = Mapper.Map<SourceStatus, TargetStatus>(SourceStatus.Active);

        Assert.Equal(TargetStatus.Enabled, result);
    }

    [Fact]
    public void Map_ShouldSupportObjectToStringValueMapping() {
        Mapper.AddMap<PersonNameSource, string>(x => x.Name);

        var result = Mapper.Map<PersonNameSource, string>(new PersonNameSource { Name = "Mapify" });

        Assert.Equal("Mapify", result);
    }

    [Fact]
    public void Map_ToExisting_ShouldThrowForValueMapping() {
        Mapper.AddMap<PersonNameSource, string>(x => x.Name);
        var source = new PersonNameSource { Name = "Mapify" };
        var target = string.Empty;

        Assert.Throws<NotSupportedException>(() => Mapper.Map(source, target));
    }

    [Fact]
    public void CompileMapper_ShouldReturnAction() {
        Expression<Func<A2, B2>> expr = x => new B2 { Prop = x.Prop };

        var action = Mapper.CompileMapper(expr);
        var source = new A2 { Prop = 50 };
        var target = new B2();
        action(source, target);

        Assert.Equal(50, target.Prop);
    }

    [Fact]
    public void CompileMapper_ShouldThrow_WhenExpressionIsNotMemberInitializer() {
        Expression<Func<A2, B2>> expr = x => new B2();

        Assert.Throws<ArgumentException>(() => Mapper.CompileMapper(expr));
    }

    [Fact]
    public void CompileMapper_ShouldThrow_WhenBindingIsNotMemberAssignment() {
        Expression<Func<ListBindingSource, ListBindingTarget>> expr = x => new ListBindingTarget { Items = { x.Value } };

        Assert.Throws<NotSupportedException>(() => Mapper.CompileMapper(expr));
    }

    [Fact]
    public void Map_ExpressionOverload_WithCache_ShouldWorkForNewAndExistingTargets() {
        Expression<Func<A2, B2>> expr = x => new B2 { Prop = x.Prop + 1 };

        var firstNew = expr.Map(new A2 { Prop = 1 }, cache: true);
        var secondNew = expr.Map(new A2 { Prop = 2 }, cache: true);

        var existing = new B2();
        expr.Map(new A2 { Prop = 3 }, existing, cache: true);
        expr.Map(new A2 { Prop = 4 }, existing, cache: true);

        Assert.Equal(2, firstNew.Prop);
        Assert.Equal(3, secondNew.Prop);
        Assert.Equal(5, existing.Prop);
    }

    [Fact]
    public void ProjectTo_IQueryable_WithParameters_ShouldResolveParameterMarkerFromStaticMap() {
        Mapper.AddMap(ParameterMarkerProfile.CreateParameterizedMap());

        var projected = new[] {
                new A2 { Prop = 1 },
                new A2 { Prop = 2 }
            }
            .AsQueryable()
            .ProjectTo<B2>(new Dictionary<string, object?> { ["offset"] = 3 })
            .Select(x => x.Prop)
            .ToArray();

        Assert.Equal([4, 5], projected);
    }

    [Fact]
    public void ProjectTo_IQueryable_ShouldApplyRegisteredMap_AndAllowFurtherComposition() {
        Mapper.AddMap<A2, B2>(x => new B2 { Prop = x.Prop + 1 });

        var projectedValues = new[] {
                new A2 { Prop = 1 },
                new A2 { Prop = 2 },
                new A2 { Prop = 3 }
            }
            .AsQueryable()
            .ProjectTo<B2>()
            .Where(x => x.Prop > 2)
            .Select(x => x.Prop)
            .ToArray();

        Assert.Equal([3, 4], projectedValues);
    }
}