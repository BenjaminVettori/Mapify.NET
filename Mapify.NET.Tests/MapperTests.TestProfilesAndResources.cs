using System.Linq.Expressions;
using System.Reflection;

namespace Mapify.NET.Tests;

public partial class MapperTests {
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
}
