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
        Mapper.UseDefaultMapIfTypeMapIsMissing(true);
        var target = Mapper.Map<Source, Target>(source);

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
        Mapper.UseDefaultMapIfTypeMapIsMissing(true);
        var target = Mapper.Map<A2, B2>(source);

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
        Mapper.UseDefaultMapIfTypeMapIsMissing(true);
        Mapper.Map(source, target);

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
        Mapper.UseDefaultMapIfTypeMapIsMissing(true);
        var target = Mapper.Map<SourceNullable, TargetNonNullable>(source);

        // Assert
        Assert.Equal(10, target.Value);
    }

    [Fact]
    public void Map_ShouldHandleNullableToNonNullable_WithNull()
    {
        // Arrange
        var source = new SourceNullable { Value = null };
        
        // Act
        Mapper.UseDefaultMapIfTypeMapIsMissing(true);
        var target = Mapper.Map<SourceNullable, TargetNonNullable>(source);

        // Assert
        Assert.Equal(0, target.Value); // Default for int
    }

    [Fact]
    public void Map_ShouldHandleNonNullableToNullable()
    {
        // Arrange
        var source = new TargetNonNullable { Value = 10 };

        // Act
        Mapper.UseDefaultMapIfTypeMapIsMissing(true);
        var target = Mapper.Map<TargetNonNullable, TargetNullable>(source);

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
        Mapper.UseDefaultMapIfTypeMapIsMissing(true);
        var expr = Mapper.GetMap<A2, B2>();
        Assert.NotNull(expr);
        var func = expr!.Compile();
        var target = func(source);
        
        // Assert
        Assert.Equal(10, target.Prop);
    }

    [Fact]
    public void GetMap_ShouldReturnCachedDefaultExpression_OnSecondCall()
    {
        Mapper.UseDefaultMapIfTypeMapIsMissing(true);
        var expr1 = Mapper.GetMap<A2, B2>();
        var expr2 = Mapper.GetMap<A2, B2>();

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
    public void GetRequiredMap_WithParameters_ShouldResolveParameterMarkerFromStaticMap()
    {
        Mapper.AddMap(ParameterMarkerProfile.CreateParameterizedMap());

        var map = Mapper.GetRequiredMap<A2, B2>(new Dictionary<string, object?> { ["offset"] = 7 });
        var mapped = map.Compile().Invoke(new A2 { Prop = 2 });

        Assert.Equal(9, mapped.Prop);
    }

    [Fact]
    public void GetRequiredMap_WithParameters_ShouldThrow_WhenParameterMarkerValueIsMissing()
    {
        Mapper.AddMap(ParameterMarkerProfile.CreateParameterizedMap());

        var ex = Assert.Throws<KeyNotFoundException>(() => Mapper.GetRequiredMap<A2, B2>());
        Assert.Contains("offset", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ProjectTo_IQueryable_WithParameters_ShouldResolveParameterMarkerFromStaticMap()
    {
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

        Assert.Equal([1, 2, 3], mapped.Numbers);
    }

    [Fact]
    public void CreateMap_ShouldRemoveIgnoredOptionalPropertyBinding()
    {
        var map = CreateMapWithResolver<IgnoreSource, IgnoreTarget>(IgnoreMarkerProfile.CreateOptionalIgnorePartial(), null);

        var init = Assert.IsType<MemberInitExpression>(map.Body);
        Assert.DoesNotContain(init.Bindings, x => x.Member.Name == nameof(IgnoreTarget.Ignored));

        var mapped = map.Map(new IgnoreSource { Included = 7, Ignored = 99 });
        Assert.Equal(7, mapped.Included);
        Assert.Equal(default, mapped.Ignored);
    }

    [Fact]
    public void CreateAndAddMap_WithIgnoreMarker_ShouldIgnorePropertyInStaticMapper()
    {
        Mapper.CreateAndAddMap<IgnoreSource, IgnoreTarget>(IgnoreMarkerProfile.CreateOptionalIgnorePartial());

        var mapped = Mapper.Map<IgnoreSource, IgnoreTarget>(new IgnoreSource { Included = 7, Ignored = 99 });

        Assert.Equal(7, mapped.Included);
        Assert.Equal(default, mapped.Ignored);
    }

    [Fact]
    public void CreateAndAddMap_WithIgnoreMarker_ShouldSkipIgnoredPropertyWhenMappingToExistingInStaticMapper()
    {
        Mapper.CreateAndAddMap<IgnoreSource, IgnoreTarget>(IgnoreMarkerProfile.CreateOptionalIgnorePartial());

        var target = new IgnoreTarget { Included = 1, Ignored = 123 };
        Mapper.Map(new IgnoreSource { Included = 10, Ignored = 999 }, target);

        Assert.Equal(10, target.Included);
        Assert.Equal(123, target.Ignored);
    }

    [Fact]
    public void CreateMap_ShouldBindDefaultValue_WhenRequiredPropertyIsIgnored()
    {
        var map = CreateMapWithResolver<IgnoreSource, IgnoreRequiredTarget>(IgnoreMarkerProfile.CreateRequiredIgnorePartial(), null);
        var mapped = map.Map(new IgnoreSource { Included = 7, Ignored = 99 });

        Assert.Equal(7, mapped.Included);
        Assert.Equal(default, mapped.Ignored);
    }

    [Fact]
    public void CompileMapper_ShouldSkipIgnoredProperties_WhenMappingToExistingObject()
    {
        var map = CreateMapWithResolver<IgnoreSource, IgnoreRequiredTarget>(IgnoreMarkerProfile.CreateRequiredIgnorePartial(), null);
        var action = Mapper.CompileMapper(map);

        var target = new IgnoreRequiredTarget { Included = 1, Ignored = 123 };
        action(new IgnoreSource { Included = 10, Ignored = 999 }, target);

        Assert.Equal(10, target.Included);
        Assert.Equal(123, target.Ignored);
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
        static LambdaExpression? Resolver(Type a, Type b, string? c) =>
            (Expression<Func<ResolverInnerSource, ResolverInnerSource, ResolverInnerTarget>>)((left, right) => new ResolverInnerTarget { Value = left.Value + right.Value });

        var map = CreateMapWithResolver<ResolverSource, ResolverTarget>(null, Resolver);
        var mapped = map.Map(new ResolverSource { Child = new ResolverInnerSource { Value = 3 } });

        Assert.Null(mapped.Child);
    }

    [Fact]
    public void CreateMap_InternalResolverWithIncompatibleSourceParameter_ShouldFallbackWithoutBinding()
    {
        static LambdaExpression? Resolver(Type a, Type b, string? c) =>
            (Expression<Func<string, ResolverInnerTarget>>)(text => new ResolverInnerTarget { Value = text.Length });

        var map = CreateMapWithResolver<ResolverSource, ResolverTarget>(null, Resolver);
        var mapped = map.Map(new ResolverSource { Child = new ResolverInnerSource { Value = 5 } });

        Assert.Null(mapped.Child);
    }

    [Fact]
    public void CreateMap_InternalResolverWithIncompatibleMappedResult_ShouldFallbackWithoutBinding()
    {
        static LambdaExpression? Resolver(Type a, Type b, string? c) =>
            (Expression<Func<ResolverInnerSource, int>>)(x => x.Value);

        var map = CreateMapWithResolver<ResolverSource, ResolverTarget>(null, Resolver);
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
    public class ListBindingTarget { public List<int> Items { get; } = []; }

    public class EnumerableCtorSource { public int[] Numbers { get; set; } = []; }
    public class EnumerableCtorContainerTarget { public EnumerableCtorCollection Numbers { get; set; } = new EnumerableCtorCollection([]); }
    public class EnumerableCtorCollection(IEnumerable<int> values) : IEnumerable<int> {
        private readonly int[] _values = [.. values];

        public int[] ToArray() => _values;

        public IEnumerator<int> GetEnumerator() => ((IEnumerable<int>)_values).GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _values.GetEnumerator();
    }

    public class ResolverInnerSource { public int Value { get; set; } }
    public class ResolverInnerTarget { public int Value { get; set; } }
    public class ResolverSource { public ResolverInnerSource Child { get; set; } = new ResolverInnerSource(); }
    public class ResolverTarget { public ResolverInnerTarget? Child { get; set; } }

    public class IgnoreSource { public int Included { get; set; } public int Ignored { get; set; } }
    public class IgnoreTarget { public int Included { get; set; } public int Ignored { get; set; } }
    public class IgnoreRequiredTarget { public int Included { get; set; } public required int Ignored { get; set; } }

    private sealed class IgnoreMarkerProfile : MapifyProfile {
        protected override void Configure() {
        }

        public static Expression<Func<IgnoreSource, IgnoreTarget>> CreateOptionalIgnorePartial()
            => x => new IgnoreTarget {
                Included = x.Included,
                Ignored = Ignore<int>()
            };

        public static Expression<Func<IgnoreSource, IgnoreRequiredTarget>> CreateRequiredIgnorePartial()
            => x => new IgnoreRequiredTarget {
                Included = x.Included,
                Ignored = Ignore<int>()
            };
    }

    public class PersonNameSource { public string Name { get; set; } = string.Empty; }

    private sealed class ParameterMarkerProfile : MapifyProfile {
        protected override void Configure() {
        }

        public static Expression<Func<A2, B2>> CreateParameterizedMap()
            => x => new B2 {
                Prop = x.Prop + Parameter<int>("offset")
            };
    }

    public enum SourceStatus { Inactive = 0, Active = 1 }
    public enum TargetStatus { Disabled = 0, Enabled = 1 }
}
