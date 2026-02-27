using System.Linq.Expressions;
using System.Reflection;

namespace Mapify.NET.Tests;

[Collection("Mapper Tests")]
public class MapperTests
{
    public class Source
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? Age { get; set; }
        public DateTime Date { get; set; }
    }

    public class Target
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? Age { get; set; }
        public DateTime Date { get; set; }
    }

    public class TargetSubset
    {
        public string Name { get; set; } = string.Empty;
    }

    public class TargetWithDifferentProp
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
    }
    
    public class SourceNullable
    {
        public int? Value { get; set; }
    }

    public class TargetNonNullable
    {
        public int Value { get; set; }
    }

    public class TargetNullable
    {
        public int? Value { get; set; }
    }

    public MapperTests()
    {
        // Reset global flag to default before each test (best effort since it's static)
        Mapper.UseDefaultMapIfTypeMapIsMissing(false);
        Mapper.ClearMappings();
    }

    [Fact]
    public void Map_ShouldMapProperties_WhenAutoMapIsUsed()
    {
        // Arrange
        var source = new Source { Id = 1, Name = "Test", Age = 25, Date = DateTime.Now };
        
        // Act
        // Use implicit creation
        var target = Mapper.Map<Source, Target>(source, useDefaultMapIfTypeMapIsMissing: true);

        // Assert
        Assert.Equal(source.Id, target.Id);
        Assert.Equal(source.Name, target.Name);
        Assert.Equal(source.Age, target.Age);
        Assert.Equal(source.Date, target.Date);
    }

    [Fact]
    public void AddMap_ShouldThrowException_WhenMappingAlreadyExists()
    {
        // Arrange
        Mapper.AddMap<Source, TargetSubset>(s => new TargetSubset { Name = s.Name });

        // Act & Assert
        Assert.Throws<ArgumentException>(() => Mapper.AddMap<Source, TargetSubset>(s => new TargetSubset { Name = s.Name }));
    }

    [Fact]
    public void Map_ShouldUseExplicitMap_WhenAdded()
    {
        // Arrange
        // We need unique types to ensure no previous map exists or we are the ones adding it.
        // Using A1->B1
        Mapper.AddMap<A1, B1>(s => new B1 { Prop = s.Prop + 10 });
        var source = new A1 { Prop = 5 };

        // Act
        var target = Mapper.Map<A1, B1>(source);

        // Assert
        Assert.Equal(15, target.Prop);
    }

    [Fact]
    public void Map_ShouldThrowException_WhenMapMissingAndFlagFalse()
    {
        // Arrange
        // Ensure global flag is false
        Mapper.UseDefaultMapIfTypeMapIsMissing(false);
        var source = new A2 { Prop = 5 };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => Mapper.Map<A2, B2>(source));
    }

    [Fact]
    public void Map_ShouldUseDefaultMap_WhenMapMissingAndFlagTrue()
    {
        // Arrange
        var source = new A2 { Prop = 5 };

        // Act
        // Pass true to override global false
        var target = Mapper.Map<A2, B2>(source, useDefaultMapIfTypeMapIsMissing: true);

        // Assert
        Assert.Equal(5, target.Prop);
    }

    [Fact]
    public void Test_GlobalUseDefaultMapIfTypeMapIsMissing()
    {
        // Arrange
        Mapper.UseDefaultMapIfTypeMapIsMissing(true);
        var source = new A3 { Prop = 5 };

        // Act
        var target = Mapper.Map<A3, B3>(source);

        // Assert
        Assert.Equal(5, target.Prop);
    }

    [Fact]
    public void Map_ToExistingObject_ShouldUpdateProperties()
    {
        // Arrange
        var source = new Source { Id = 10, Name = "Updated" };
        var target = new Target { Id = 1, Name = "Original" };

        // Act
        Mapper.Map(source, target, useDefaultMapIfTypeMapIsMissing: true);

        // Assert
        Assert.Equal(10, target.Id);
        Assert.Equal("Updated", target.Name);
    }

    [Fact]
    public void Map_ToExistingObject_WithExplicitMap()
    {
         // Arrange
        var source = new A4 { Prop = 20 };
        var target = new B4 { Prop = 10 };
        
        Mapper.AddMap<A4, B4>(s => new B4 { Prop = s.Prop + 10 });
        
        // Act
        Mapper.Map(source, target);

        // Assert
        Assert.Equal(30, target.Prop);
    }

    [Fact]
    public void Map_ShouldHandleNullableToNonNullable_WithValues()
    {
        // Arrange
        var source = new SourceNullable { Value = 10 };
        
        // Act
        var target = Mapper.Map<SourceNullable, TargetNonNullable>(source, useDefaultMapIfTypeMapIsMissing: true);

        // Assert
        Assert.Equal(10, target.Value);
    }

    [Fact]
    public void Map_ShouldHandleNullableToNonNullable_WithNull()
    {
        // Arrange
        var source = new SourceNullable { Value = null };
        
        // Act
        var target = Mapper.Map<SourceNullable, TargetNonNullable>(source, useDefaultMapIfTypeMapIsMissing: true);

        // Assert
        Assert.Equal(0, target.Value); // Default for int
    }

    [Fact]
    public void Map_ShouldHandleNonNullableToNullable()
    {
        // Arrange
        var source = new TargetNonNullable { Value = 10 };

        // Act
        var target = Mapper.Map<TargetNonNullable, TargetNullable>(source, useDefaultMapIfTypeMapIsMissing: true);

        // Assert
        Assert.Equal(10, target.Value);
    }
    
    [Fact]
    public void CreateAndAddMap_ShouldCreateFullMap()
    {
         // Arrange
         // Unique types
         var source = new C1 { Id = 1, Name = "Test" };
         
         // Act
         // Partial map for Name, expected Id to be automapped
           var createdMap = Mapper.CreateAndAddMap<C1, D1>(s => new D1 { Name = s.Name + "_" });
         
         var target = Mapper.Map<C1, D1>(source);

         // Assert
           Assert.NotNull(createdMap);
         Assert.Equal(1, target.Id);
         Assert.Equal("Test_", target.Name);
    }

    [Fact]
    public void GetMap_ShouldReturnExpression()
    {
        // Arrange
        var source = new A2 { Prop = 10 };
        // Ensure we can get a map (defaults allowed)
        
        // Act
        var expr = Mapper.GetMap<A2, B2>(useDefaultMapIfTypeMapIsMissing: true);
        Assert.NotNull(expr);
        var func = expr!.Compile();
        var target = func(source);
        
        // Assert
        Assert.Equal(10, target.Prop);
    }

    [Fact]
    public void GetMap_ShouldReturnCachedDefaultExpression_OnSecondCall()
    {
        var expr1 = Mapper.GetMap<A2, B2>(useDefaultMapIfTypeMapIsMissing: true);
        var expr2 = Mapper.GetMap<A2, B2>(useDefaultMapIfTypeMapIsMissing: true);

        Assert.NotNull(expr1);
        Assert.Same(expr1, expr2);
    }

    [Fact]
    public void GetMap_ShouldReturnNull_WhenMapMissingAndDefaultDisabled()
    {
        // Arrange
        Mapper.UseDefaultMapIfTypeMapIsMissing(false);

        // Act
        var expr = Mapper.GetMap<A4, B4>();

        // Assert
        Assert.Null(expr);
    }

    [Fact]
    public void GetRequiredMap_ShouldThrow_WhenMapMissingAndDefaultDisabled()
    {
        // Arrange
        Mapper.UseDefaultMapIfTypeMapIsMissing(false);

        // Act / Assert
        Assert.Throws<ArgumentException>(() => Mapper.GetRequiredMap<A4, B4>());
    }

    [Fact]
    public void CompileMapper_ShouldReturnAction()
    {
        // Arrange
        Expression<Func<A2, B2>> expr = x => new B2 { Prop = x.Prop };
        
        // Act
        var action = Mapper.CompileMapper(expr);
        var source = new A2 { Prop = 50 };
        var target = new B2();
        action(source, target);
        
        // Assert
        Assert.Equal(50, target.Prop);
    }

    [Fact]
    public void CompileMapper_ShouldThrow_WhenExpressionIsNotMemberInitializer()
    {
        Expression<Func<A2, B2>> expr = x => new B2();

        Assert.Throws<ArgumentException>(() => Mapper.CompileMapper(expr));
    }

    [Fact]
    public void CompileMapper_ShouldThrow_WhenBindingIsNotMemberAssignment()
    {
        Expression<Func<ListBindingSource, ListBindingTarget>> expr = x => new ListBindingTarget { Items = { x.Value } };

        Assert.Throws<NotSupportedException>(() => Mapper.CompileMapper(expr));
    }

    [Fact]
    public void Map_ExpressionOverload_WithCache_ShouldWorkForNewAndExistingTargets()
    {
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
    public void Map_ShouldSupportEnumToEnumValueMapping()
    {
        // Arrange
        Mapper.AddMap<SourceStatus, TargetStatus>(x => x == SourceStatus.Active ? TargetStatus.Enabled : TargetStatus.Disabled);

        // Act
        var result = Mapper.Map<SourceStatus, TargetStatus>(SourceStatus.Active);

        // Assert
        Assert.Equal(TargetStatus.Enabled, result);
    }

    [Fact]
    public void Map_ShouldSupportObjectToStringValueMapping()
    {
        // Arrange
        Mapper.AddMap<PersonNameSource, string>(x => x.Name);

        // Act
        var result = Mapper.Map<PersonNameSource, string>(new PersonNameSource { Name = "Mapify" });

        // Assert
        Assert.Equal("Mapify", result);
    }

    [Fact]
    public void Map_ToExisting_ShouldThrowForValueMapping()
    {
        // Arrange
        Mapper.AddMap<PersonNameSource, string>(x => x.Name);
        var source = new PersonNameSource { Name = "Mapify" };
        var target = string.Empty;

        // Act / Assert
        Assert.Throws<NotSupportedException>(() => Mapper.Map(source, target));
    }

    [Fact]
    public void CreateMap_ShouldUseExistingNestedMapImplicitly_WhenPropertyTypesDiffer()
    {
        // Arrange
        Mapper.AddMap<NestedSource, NestedTarget>(x => new NestedTarget { Value = x.Value + 1 });

        // Act
        var parentMap = Mapper.CreateMap<ParentWithNestedSource, ParentWithNestedTarget>();
        var mapped = parentMap.Map(new ParentWithNestedSource {
            Nested = new NestedSource { Value = 9 }
        });

        // Assert
        Assert.NotNull(mapped.Nested);
        Assert.Equal(10, mapped.Nested.Value);
    }

    [Fact]
    public void CreateMap_ShouldLiftNonNullableMap_ForAllNullableVariants()
    {
        // Arrange: only register non-nullable map.
        Mapper.AddMap<NumberSource, NumberTarget>(x => new NumberTarget { Value = x.Value + 1 });

        var s1 = new ContainerSrcToTarget { Number = new NumberSource { Value = 1 } };
        var s2 = new ContainerSrcToNullableTarget { Number = new NumberSource { Value = 2 } };
        var s3WithValue = new ContainerNullableSrcToTarget { Number = new NumberSource { Value = 3 } };
        var s3Null = new ContainerNullableSrcToTarget { Number = null };
        var s4WithValue = new ContainerNullableSrcToNullableTarget { Number = new NumberSource { Value = 4 } };
        var s4Null = new ContainerNullableSrcToNullableTarget { Number = null };

        // Act
        var m1 = Mapper.CreateMap<ContainerSrcToTarget, ContainerTarget>();
        var m2 = Mapper.CreateMap<ContainerSrcToNullableTarget, ContainerNullableTarget>();
        var m3 = Mapper.CreateMap<ContainerNullableSrcToTarget, ContainerTarget>();
        var m4 = Mapper.CreateMap<ContainerNullableSrcToNullableTarget, ContainerNullableTarget>();

        var r1 = m1.Map(s1);
        var r2 = m2.Map(s2);
        var r3WithValue = m3.Map(s3WithValue);
        var r3Null = m3.Map(s3Null);
        var r4WithValue = m4.Map(s4WithValue);
        var r4Null = m4.Map(s4Null);

        // Assert
        Assert.Equal(2, r1.Number.Value);
        Assert.NotNull(r2.Number);
        Assert.Equal(3, r2.Number!.Value.Value);

        Assert.Equal(4, r3WithValue.Number.Value);
        Assert.Equal(default, r3Null.Number);

        Assert.NotNull(r4WithValue.Number);
        Assert.Equal(5, r4WithValue.Number!.Value.Value);
        Assert.Null(r4Null.Number);
    }

    [Fact]
    public void CreateMap_NullableComplexStructToNonNullableSameType_ShouldUseExistingMapWhenPresent()
    {
        // Arrange
        Mapper.AddMap<ComplexValue, ComplexValue>(x => new ComplexValue { Value = x.Value + 1 });

        var withValue = new ComplexValueContainerSource { Item = new ComplexValue { Value = 10 } };
        var withNull = new ComplexValueContainerSource { Item = null };

        // Act
        var map = Mapper.CreateMap<ComplexValueContainerSource, ComplexValueContainerTarget>();
        var mappedWithValue = map.Map(withValue);
        var mappedWithNull = map.Map(withNull);

        // Assert
        Assert.Equal(11, mappedWithValue.Item.Value);
        Assert.Equal(default, mappedWithNull.Item);
    }

    [Fact]
    public void CreateMap_ShouldMaterializeEnumerableIntoPropertyTypeWithIEnumerableConstructor()
    {
        var map = Mapper.CreateMap<EnumerableCtorSource, EnumerableCtorContainerTarget>();

        var mapped = map.Map(new EnumerableCtorSource { Numbers = [1, 2, 3] });

        Assert.Equal(new[] { 1, 2, 3 }, mapped.Numbers);
    }

    [Fact]
    public void CreateMap_InternalResolverNull_ShouldFallbackWithoutExistingMapBinding()
    {
        var map = CreateMapWithResolver<ResolverSource, ResolverTarget>(null, null);

        var mapped = map.Map(new ResolverSource { Child = new ResolverInnerSource { Value = 8 } });

        Assert.Null(mapped.Child);
    }

    [Fact]
    public void CreateMap_InternalResolverWithInvalidParameterCount_ShouldFallbackWithoutBinding()
    {
        Func<Type, Type, string?, LambdaExpression?> resolver = (_, _, _) =>
            (Expression<Func<ResolverInnerSource, ResolverInnerSource, ResolverInnerTarget>>)((left, right) => new ResolverInnerTarget { Value = left.Value + right.Value });

        var map = CreateMapWithResolver<ResolverSource, ResolverTarget>(null, resolver);
        var mapped = map.Map(new ResolverSource { Child = new ResolverInnerSource { Value = 3 } });

        Assert.Null(mapped.Child);
    }

    [Fact]
    public void CreateMap_InternalResolverWithIncompatibleSourceParameter_ShouldFallbackWithoutBinding()
    {
        Func<Type, Type, string?, LambdaExpression?> resolver = (_, _, _) =>
            (Expression<Func<string, ResolverInnerTarget>>)(text => new ResolverInnerTarget { Value = text.Length });

        var map = CreateMapWithResolver<ResolverSource, ResolverTarget>(null, resolver);
        var mapped = map.Map(new ResolverSource { Child = new ResolverInnerSource { Value = 5 } });

        Assert.Null(mapped.Child);
    }

    [Fact]
    public void CreateMap_InternalResolverWithIncompatibleMappedResult_ShouldFallbackWithoutBinding()
    {
        Func<Type, Type, string?, LambdaExpression?> resolver = (_, _, _) =>
            (Expression<Func<ResolverInnerSource, int>>)(x => x.Value);

        var map = CreateMapWithResolver<ResolverSource, ResolverTarget>(null, resolver);
        var mapped = map.Map(new ResolverSource { Child = new ResolverInnerSource { Value = 6 } });

        Assert.Null(mapped.Child);
    }

    [Fact]
    public void ProjectTo_IQueryable_ShouldApplyRegisteredMap_AndAllowFurtherComposition()
    {
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

    private static Expression<Func<TSource, TTarget>> CreateMapWithResolver<TSource, TTarget>(
        Expression<Func<TSource, TTarget>>? partial,
        Func<Type, Type, string?, LambdaExpression?>? resolver
    ) {
        var method = typeof(Mapper)
            .GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
            .Single(x => x.Name == "CreateMap" && x.IsGenericMethodDefinition && x.GetParameters().Length == 2);

        var generic = method.MakeGenericMethod(typeof(TSource), typeof(TTarget));
        return (Expression<Func<TSource, TTarget>>)generic.Invoke(null, [partial, resolver])!;
    }

    public class C1 { public int Id { get; set; } public string Name { get; set; } = string.Empty; }
    public class D1 { public int Id { get; set; } public string Name { get; set; } = string.Empty; }

    public class A1 { public int Prop { get; set; } }
    public class B1 { public int Prop { get; set; } }

    public class A2 { public int Prop { get; set; } }
    public class B2 { public int Prop { get; set; } }

    public class A3 { public int Prop { get; set; } }
    public class B3 { public int Prop { get; set; } }

    public class A4 { public int Prop { get; set; } }
    public class B4 { public int Prop { get; set; } }

    public class NestedSource { public int Value { get; set; } }
    public class NestedTarget { public int Value { get; set; } }

    public class ParentWithNestedSource { public NestedSource Nested { get; set; } = new NestedSource(); }
    public class ParentWithNestedTarget { public NestedTarget Nested { get; set; } = new NestedTarget(); }

    public struct NumberSource { public int Value { get; set; } }
    public struct NumberTarget { public int Value { get; set; } }

    public class ContainerSrcToTarget { public NumberSource Number { get; set; } }
    public class ContainerTarget { public NumberTarget Number { get; set; } }

    public class ContainerSrcToNullableTarget { public NumberSource Number { get; set; } }
    public class ContainerNullableTarget { public NumberTarget? Number { get; set; } }

    public class ContainerNullableSrcToTarget { public NumberSource? Number { get; set; } }
    public class ContainerNullableSrcToNullableTarget { public NumberSource? Number { get; set; } }

    public struct ComplexValue { public int Value { get; set; } }
    public class ComplexValueContainerSource { public ComplexValue? Item { get; set; } }
    public class ComplexValueContainerTarget { public ComplexValue Item { get; set; } }

    public class ListBindingSource { public int Value { get; set; } }
    public class ListBindingTarget { public List<int> Items { get; } = new List<int>(); }

    public class EnumerableCtorSource { public int[] Numbers { get; set; } = []; }
    public class EnumerableCtorContainerTarget { public EnumerableCtorCollection Numbers { get; set; } = new EnumerableCtorCollection(Array.Empty<int>()); }
    public class EnumerableCtorCollection : IEnumerable<int> {
        private readonly int[] _values;

        public EnumerableCtorCollection(IEnumerable<int> values) {
            _values = values.ToArray();
        }

        public int[] ToArray() => _values;

        public IEnumerator<int> GetEnumerator() => ((IEnumerable<int>)_values).GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _values.GetEnumerator();
    }

    public class ResolverInnerSource { public int Value { get; set; } }
    public class ResolverInnerTarget { public int Value { get; set; } }
    public class ResolverSource { public ResolverInnerSource Child { get; set; } = new ResolverInnerSource(); }
    public class ResolverTarget { public ResolverInnerTarget? Child { get; set; } }

    public class PersonNameSource { public string Name { get; set; } = string.Empty; }

    public enum SourceStatus { Inactive = 0, Active = 1 }
    public enum TargetStatus { Disabled = 0, Enabled = 1 }
}
