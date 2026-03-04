namespace Mapify.NET.Tests;

public partial class MapperTests {
    [Fact]
    public void CreateMap_ShouldUseExistingNestedMapImplicitly_WhenPropertyTypesDiffer() {
        Mapper.AddMap<NestedSource, NestedTarget>(x => new NestedTarget { Value = x.Value + 1 });

        var parentMap = Mapper.CreateMap<ParentWithNestedSource, ParentWithNestedTarget>();
        var mapped = parentMap.Map(new ParentWithNestedSource {
            Nested = new NestedSource { Value = 9 }
        });

        Assert.NotNull(mapped.Nested);
        Assert.Equal(10, mapped.Nested.Value);
    }

    [Fact]
    public void CreateMap_ShouldLiftNonNullableMap_ForAllNullableVariants() {
        Mapper.AddMap<NumberSource, NumberTarget>(x => new NumberTarget { Value = x.Value + 1 });

        var s1 = new ContainerSrcToTarget { Number = new NumberSource { Value = 1 } };
        var s2 = new ContainerSrcToNullableTarget { Number = new NumberSource { Value = 2 } };
        var s3WithValue = new ContainerNullableSrcToTarget { Number = new NumberSource { Value = 3 } };
        var s3Null = new ContainerNullableSrcToTarget { Number = null };
        var s4WithValue = new ContainerNullableSrcToNullableTarget { Number = new NumberSource { Value = 4 } };
        var s4Null = new ContainerNullableSrcToNullableTarget { Number = null };

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
    public void CreateMap_ShouldPreferExactNullableMap_OverUnderlyingTypeFallback() {
        Mapper.AddMap<PrecedenceNumberSource, PrecedenceNumberTarget>(x => new PrecedenceNumberTarget { Value = x.Value + 1 });
        Mapper.AddMap<PrecedenceNumberSource?, PrecedenceNumberTarget?>(
            x => x == null
                ? (PrecedenceNumberTarget?)null
                : new PrecedenceNumberTarget { Value = x.Value.Value + 100 }
        );

        var map = Mapper.CreateMap<PrecedenceNullableContainerSource, PrecedenceNullableContainerTarget>();
        var mapped = map.Map(new PrecedenceNullableContainerSource {
            Item = new PrecedenceNumberSource { Value = 5 }
        });

        Assert.NotNull(mapped.Item);
        Assert.Equal(105, mapped.Item!.Value.Value);
    }

    [Fact]
    public void CreateMap_ShouldFallbackToUnderlyingNullableTypeMap_WhenExactNullableMapIsMissing() {
        Mapper.AddMap<PrecedenceNumberSource, PrecedenceNumberTarget>(x => new PrecedenceNumberTarget { Value = x.Value + 1 });

        var map = Mapper.CreateMap<PrecedenceNullableContainerSource, PrecedenceNullableContainerTarget>();
        var mapped = map.Map(new PrecedenceNullableContainerSource {
            Item = new PrecedenceNumberSource { Value = 5 }
        });

        Assert.NotNull(mapped.Item);
        Assert.Equal(6, mapped.Item!.Value.Value);
    }

    [Fact]
    public void CreateMap_ShouldThrowForAmbiguousEnumerableElementType() {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            Mapper.CreateMap<AmbiguousEnumerableContainerSource, AmbiguousEnumerableContainerTarget>()
        );

        Assert.Contains("multiple IEnumerable<T> element types", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CreateMap_NullableComplexStructToNonNullableSameType_ShouldUseExistingMapWhenPresent() {
        Mapper.AddMap<ComplexValue, ComplexValue>(x => new ComplexValue { Value = x.Value + 1 });

        var withValue = new ComplexValueContainerSource { Item = new ComplexValue { Value = 10 } };
        var withNull = new ComplexValueContainerSource { Item = null };

        var map = Mapper.CreateMap<ComplexValueContainerSource, ComplexValueContainerTarget>();
        var mappedWithValue = map.Map(withValue);
        var mappedWithNull = map.Map(withNull);

        Assert.Equal(11, mappedWithValue.Item.Value);
        Assert.Equal(default, mappedWithNull.Item);
    }

    [Fact]
    public void CreateMap_ShouldMaterializeEnumerableIntoPropertyTypeWithIEnumerableConstructor() {
        var map = Mapper.CreateMap<EnumerableCtorSource, EnumerableCtorContainerTarget>();

        var mapped = map.Map(new EnumerableCtorSource { Numbers = [1, 2, 3] });

        Assert.Equal([1, 2, 3], mapped.Numbers);
    }
}