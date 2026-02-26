using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

namespace Mapify.NET {
    /// <summary>
    /// Extension methods for projecting non-generic and generic query/sequence sources to mapped target types.
    /// </summary>
    public static class MapifyProjectToExtensions {
        /// <summary>
        /// Projects a non-generic query to <typeparamref name="TTarget"/> using the static mapper configuration.
        /// </summary>
        /// <typeparam name="TTarget">The target projection type.</typeparam>
        /// <param name="source">The source query.</param>
        /// <param name="useDefaultMapIfTypeMapIsMissing">Whether to allow automatic default-map creation for this call.</param>
        /// <returns>A projected query of <typeparamref name="TTarget"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        public static IQueryable<TTarget> ProjectTo<TTarget>(this IQueryable source, bool useDefaultMapIfTypeMapIsMissing = false) {
            if (source == null) {
                throw new ArgumentNullException(nameof(source));
            }

            var mapExpression = GetStaticMapExpression(source.ElementType, typeof(TTarget), useDefaultMapIfTypeMapIsMissing);
            return BuildProjectedQuery<TTarget>(source, mapExpression);
        }

        /// <summary>
        /// Projects a non-generic query to <typeparamref name="TTarget"/> using an instance mapper.
        /// </summary>
        /// <typeparam name="TTarget">The target projection type.</typeparam>
        /// <param name="source">The source query.</param>
        /// <param name="mapify">The mapper instance used to resolve maps.</param>
        /// <param name="useDefaultMapIfTypeMapIsMissing">Whether to allow automatic default-map creation for this call.</param>
        /// <returns>A projected query of <typeparamref name="TTarget"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="mapify"/> is null.</exception>
        public static IQueryable<TTarget> ProjectTo<TTarget>(this IQueryable source, IMapify mapify, bool useDefaultMapIfTypeMapIsMissing = false) {
            if (source == null) {
                throw new ArgumentNullException(nameof(source));
            }

            if (mapify == null) {
                throw new ArgumentNullException(nameof(mapify));
            }

            var mapExpression = GetInstanceMapExpression(source.ElementType, typeof(TTarget), mapify, useDefaultMapIfTypeMapIsMissing);
            return BuildProjectedQuery<TTarget>(source, mapExpression);
        }

        /// <summary>
        /// Projects a non-generic query to <typeparamref name="TTarget"/> using a named map on an instance mapper.
        /// </summary>
        /// <typeparam name="TTarget">The target projection type.</typeparam>
        /// <param name="source">The source query.</param>
        /// <param name="mapify">The mapper instance used to resolve maps.</param>
        /// <param name="name">The map name.</param>
        /// <returns>A projected query of <typeparamref name="TTarget"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="mapify"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null, empty, or whitespace.</exception>
        public static IQueryable<TTarget> ProjectTo<TTarget>(this IQueryable source, IMapify mapify, string name) {
            if (source == null) {
                throw new ArgumentNullException(nameof(source));
            }

            if (mapify == null) {
                throw new ArgumentNullException(nameof(mapify));
            }

            if (string.IsNullOrWhiteSpace(name)) {
                throw new ArgumentException("Mapping name must not be null or whitespace.", nameof(name));
            }

            var mapExpression = GetInstanceMapExpression(source.ElementType, typeof(TTarget), mapify, name);
            return BuildProjectedQuery<TTarget>(source, mapExpression);
        }

        /// <summary>
        /// Marker overload intended for use inside map expressions.
        /// For runtime named projections, use <c>query.ProjectTo&lt;TTarget&gt;(mapify, name)</c>.
        /// </summary>
        /// <typeparam name="TTarget">The target projection type.</typeparam>
        /// <param name="source">The source query.</param>
        /// <param name="name">The map name.</param>
        /// <returns>Never returns; this overload always throws.</returns>
        /// <exception cref="InvalidOperationException">Always thrown for direct runtime use.</exception>
        public static IQueryable<TTarget> ProjectTo<TTarget>(this IQueryable source, string name)
            => throw new InvalidOperationException($"{nameof(ProjectTo)} with a map name requires an {nameof(IMapify)} instance. Use query.ProjectTo<TTarget>(mapify, name). Inside {nameof(MapifyProfile)} CreateMap expressions this overload is used as a marker and is resolved during map creation.");

        /// <summary>
        /// Projects a non-generic sequence to <typeparamref name="TTarget"/> using the static mapper configuration.
        /// </summary>
        /// <typeparam name="TTarget">The target projection type.</typeparam>
        /// <param name="source">The source sequence.</param>
        /// <param name="useDefaultMapIfTypeMapIsMissing">Whether to allow automatic default-map creation for this call.</param>
        /// <returns>A projected sequence of <typeparamref name="TTarget"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        public static IEnumerable<TTarget> ProjectTo<TTarget>(this IEnumerable source, bool useDefaultMapIfTypeMapIsMissing = false) {
            if (source == null) {
                throw new ArgumentNullException(nameof(source));
            }

            var sourceElementType = GetEnumerableElementType(source.GetType());
            var method = typeof(MapifyProjectToExtensions)
                .GetMethod(nameof(ProjectToEnumerableCoreStatic), BindingFlags.Static | BindingFlags.NonPublic)!
                .MakeGenericMethod(sourceElementType, typeof(TTarget));

            return (IEnumerable<TTarget>)method.Invoke(null, [source, useDefaultMapIfTypeMapIsMissing])!;
        }

        /// <summary>
        /// Projects a non-generic sequence to <typeparamref name="TTarget"/> using an instance mapper.
        /// </summary>
        /// <typeparam name="TTarget">The target projection type.</typeparam>
        /// <param name="source">The source sequence.</param>
        /// <param name="mapify">The mapper instance used to resolve maps.</param>
        /// <param name="useDefaultMapIfTypeMapIsMissing">Whether to allow automatic default-map creation for this call.</param>
        /// <returns>A projected sequence of <typeparamref name="TTarget"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="mapify"/> is null.</exception>
        public static IEnumerable<TTarget> ProjectTo<TTarget>(this IEnumerable source, IMapify mapify, bool useDefaultMapIfTypeMapIsMissing = false) {
            if (source == null) {
                throw new ArgumentNullException(nameof(source));
            }

            if (mapify == null) {
                throw new ArgumentNullException(nameof(mapify));
            }

            var sourceElementType = GetEnumerableElementType(source.GetType());
            var method = typeof(MapifyProjectToExtensions)
                .GetMethod(nameof(ProjectToEnumerableCoreInstance), BindingFlags.Static | BindingFlags.NonPublic)!
                .MakeGenericMethod(sourceElementType, typeof(TTarget));

            return (IEnumerable<TTarget>)method.Invoke(null, [source, mapify, useDefaultMapIfTypeMapIsMissing])!;
        }

        /// <summary>
        /// Projects a non-generic sequence to <typeparamref name="TTarget"/> using a named map on an instance mapper.
        /// </summary>
        /// <typeparam name="TTarget">The target projection type.</typeparam>
        /// <param name="source">The source sequence.</param>
        /// <param name="mapify">The mapper instance used to resolve maps.</param>
        /// <param name="name">The map name.</param>
        /// <returns>A projected sequence of <typeparamref name="TTarget"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="mapify"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null, empty, or whitespace.</exception>
        public static IEnumerable<TTarget> ProjectTo<TTarget>(this IEnumerable source, IMapify mapify, string name) {
            if (source == null) {
                throw new ArgumentNullException(nameof(source));
            }

            if (mapify == null) {
                throw new ArgumentNullException(nameof(mapify));
            }

            if (string.IsNullOrWhiteSpace(name)) {
                throw new ArgumentException("Mapping name must not be null or whitespace.", nameof(name));
            }

            var sourceElementType = GetEnumerableElementType(source.GetType());
            var method = typeof(MapifyProjectToExtensions)
                .GetMethod(nameof(ProjectToEnumerableCoreInstanceNamed), BindingFlags.Static | BindingFlags.NonPublic)!
                .MakeGenericMethod(sourceElementType, typeof(TTarget));

            return (IEnumerable<TTarget>)method.Invoke(null, [source, mapify, name])!;
        }

        /// <summary>
        /// Marker overload intended for use inside map expressions.
        /// For runtime named projections, use <c>sequence.ProjectTo&lt;TTarget&gt;(mapify, name)</c>.
        /// </summary>
        /// <typeparam name="TTarget">The target projection type.</typeparam>
        /// <param name="source">The source sequence.</param>
        /// <param name="name">The map name.</param>
        /// <returns>Never returns; this overload always throws.</returns>
        /// <exception cref="InvalidOperationException">Always thrown for direct runtime use.</exception>
        public static IEnumerable<TTarget> ProjectTo<TTarget>(this IEnumerable source, string name)
            => throw new InvalidOperationException($"{nameof(ProjectTo)} with a map name requires an {nameof(IMapify)} instance. Use sequence.ProjectTo<TTarget>(mapify, name). Inside {nameof(MapifyProfile)} CreateMap expressions this overload is used as a marker and is resolved during map creation.");

        private static IQueryable<TTarget> BuildProjectedQuery<TTarget>(IQueryable source, LambdaExpression mapExpression) {
            var selectExpression = Expression.Call(
                typeof(Queryable),
                nameof(Queryable.Select),
                [source.ElementType, typeof(TTarget)],
                source.Expression,
                Expression.Quote(mapExpression)
            );

            return source.Provider.CreateQuery<TTarget>(selectExpression);
        }

        private static LambdaExpression GetStaticMapExpression(Type sourceType, Type targetType, bool useDefaultMapIfTypeMapIsMissing) {
            var genericMethod = typeof(Mapper)
                .GetMethod(nameof(Mapper.GetRequiredMap), BindingFlags.Static | BindingFlags.Public)!
                .MakeGenericMethod(sourceType, targetType);

            return (LambdaExpression)genericMethod.Invoke(null, [useDefaultMapIfTypeMapIsMissing])!;
        }

        private static LambdaExpression GetInstanceMapExpression(Type sourceType, Type targetType, IMapify mapify, bool useDefaultMapIfTypeMapIsMissing) {
            var genericMethod = typeof(IMapify)
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Single(m => m.Name == nameof(IMapify.GetRequiredMap)
                    && m.IsGenericMethodDefinition
                    && m.GetGenericArguments().Length == 2
                    && m.GetParameters().Length == 1
                    && m.GetParameters()[0].ParameterType == typeof(bool))
                .MakeGenericMethod(sourceType, targetType);

            return (LambdaExpression)genericMethod.Invoke(mapify, [useDefaultMapIfTypeMapIsMissing])!;
        }

        private static LambdaExpression GetInstanceMapExpression(Type sourceType, Type targetType, IMapify mapify, string name) {
            var genericMethod = typeof(IMapify)
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Single(m => m.Name == nameof(IMapify.GetRequiredMap)
                    && m.IsGenericMethodDefinition
                    && m.GetGenericArguments().Length == 2
                    && m.GetParameters().Length == 1
                    && m.GetParameters()[0].ParameterType == typeof(string))
                .MakeGenericMethod(sourceType, targetType);

            return (LambdaExpression)genericMethod.Invoke(mapify, [name])!;
        }

        private static IEnumerable<TTarget> ProjectToEnumerableCoreStatic<TSource, TTarget>(IEnumerable source, bool useDefaultMapIfTypeMapIsMissing) {
            var map = Mapper.GetRequiredMap<TSource, TTarget>(useDefaultMapIfTypeMapIsMissing).Compile();
            return source.Cast<TSource>().Select(map);
        }

        private static IEnumerable<TTarget> ProjectToEnumerableCoreInstance<TSource, TTarget>(IEnumerable source, IMapify mapify, bool useDefaultMapIfTypeMapIsMissing) {
            var map = mapify.GetRequiredMap<TSource, TTarget>(useDefaultMapIfTypeMapIsMissing).Compile();
            return source.Cast<TSource>().Select(map);
        }

        private static IEnumerable<TTarget> ProjectToEnumerableCoreInstanceNamed<TSource, TTarget>(IEnumerable source, IMapify mapify, string name) {
            var map = mapify.GetRequiredMap<TSource, TTarget>(name).Compile();
            return source.Cast<TSource>().Select(map);
        }

        private static Type GetEnumerableElementType(Type enumerableType) {
            if (enumerableType == typeof(string)) {
                return typeof(char);
            }

            if (enumerableType.IsArray) {
                return enumerableType.GetElementType()!;
            }

            var enumerableInterface = enumerableType
                .GetInterfaces()
                .Concat([enumerableType])
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

            if (enumerableInterface == null) {
                return typeof(object);
            }

            return enumerableInterface.GetGenericArguments()[0];
        }
    }
}
