using System.Linq.Expressions;

namespace Mapify.NET.Tests;

public partial class MapperTests {
    [Fact]
    public void CreateMap_InternalResolverNull_ShouldFallbackWithoutExistingMapBinding() {
        var map = CreateMapWithResolver<ResolverSource, ResolverTarget>(null, null);

        var mapped = map.Map(new ResolverSource { Child = new ResolverInnerSource { Value = 8 } });

        Assert.Null(mapped.Child);
    }

    [Fact]
    public void CreateMap_InternalResolverWithInvalidParameterCount_ShouldFallbackWithoutBinding() {
        static LambdaExpression? Resolver(Type a, Type b, string? c) =>
            (Expression<Func<ResolverInnerSource, ResolverInnerSource, ResolverInnerTarget>>)((left, right) => new ResolverInnerTarget { Value = left.Value + right.Value });

        var map = CreateMapWithResolver<ResolverSource, ResolverTarget>(null, Resolver);
        var mapped = map.Map(new ResolverSource { Child = new ResolverInnerSource { Value = 3 } });

        Assert.Null(mapped.Child);
    }

    [Fact]
    public void CreateMap_InternalResolverWithIncompatibleSourceParameter_ShouldFallbackWithoutBinding() {
        static LambdaExpression? Resolver(Type a, Type b, string? c) =>
            (Expression<Func<string, ResolverInnerTarget>>)(text => new ResolverInnerTarget { Value = text.Length });

        var map = CreateMapWithResolver<ResolverSource, ResolverTarget>(null, Resolver);
        var mapped = map.Map(new ResolverSource { Child = new ResolverInnerSource { Value = 5 } });

        Assert.Null(mapped.Child);
    }

    [Fact]
    public void CreateMap_InternalResolverWithIncompatibleMappedResult_ShouldFallbackWithoutBinding() {
        static LambdaExpression? Resolver(Type a, Type b, string? c) =>
            (Expression<Func<ResolverInnerSource, int>>)(x => x.Value);

        var map = CreateMapWithResolver<ResolverSource, ResolverTarget>(null, Resolver);
        var mapped = map.Map(new ResolverSource { Child = new ResolverInnerSource { Value = 6 } });

        Assert.Null(mapped.Child);
    }
}