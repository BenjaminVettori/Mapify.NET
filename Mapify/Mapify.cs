using System.Linq.Expressions;

namespace Mapify.NET {
    public class Mapify : IMapify, IMapifyConfigurator {
        private bool _useDefaultMapIfTypeMapIsMissing;

        private readonly IDictionary<Tuple<Type, Type>, LambdaExpression> _converters = new Dictionary<Tuple<Type, Type>, LambdaExpression>();

        private readonly IDictionary<Tuple<Type, Type>, LambdaExpression> _defaultMapCache = new Dictionary<Tuple<Type, Type>, LambdaExpression>();

        private readonly IDictionary<Tuple<Type, Type>, Delegate> _compiledMapToExistingCache = new Dictionary<Tuple<Type, Type>, Delegate>();

        private readonly IDictionary<Tuple<Type, Type>, Delegate> _compiledMapToNewCache = new Dictionary<Tuple<Type, Type>, Delegate>();

        public Mapify(params IMapifyProfile[] profiles)
            : this((IEnumerable<IMapifyProfile>)profiles) {
        }

        public Mapify(IEnumerable<IMapifyProfile>? profiles = null) {
            if (profiles == null) {
                return;
            }

            foreach (var profile in profiles) {
                if (profile is not MapifyProfile mapifyProfile) {
                    throw new ArgumentException($"Profile '{profile.GetType().FullName}' must inherit from {nameof(MapifyProfile)}.");
                }

                mapifyProfile.Apply(this);
            }
        }

        public void UseDefaultMapIfTypeMapIsMissing(bool value) {
            _useDefaultMapIfTypeMapIsMissing = value;
        }

        void IMapifyConfigurator.CreateAndAddMap<TSource, TTarget>(Expression<Func<TSource, TTarget>>? partial) {
            ((IMapifyConfigurator)this).AddMap<TSource, TTarget>(Mapper.CreateMap(partial));
        }

        void IMapifyConfigurator.AddMap<TSource, TTarget>(Expression<Func<TSource, TTarget>> mappingExpression) {
            var key = new Tuple<Type, Type>(typeof(TSource), typeof(TTarget));
            if (_converters.ContainsKey(key)) {
                throw new ArgumentException($"There already exists a mapping from TSource ({typeof(TSource).FullName}) to TTarget ({typeof(TTarget).FullName}). There can only be one mapping for each combination of TSource and TTarget.");
            }

            _converters[key] = mappingExpression;
            _compiledMapToNewCache[key] = mappingExpression.Compile();

            if (mappingExpression.Body is MemberInitExpression) {
                _compiledMapToExistingCache[key] = Mapper.CompileMapper(mappingExpression);
            }
        }

        private Expression<Func<TSource, TTarget>> GetMap<TSource, TTarget>(bool useDefaultMapIfTypeMapIsMissing = false) {
            var key = new Tuple<Type, Type>(typeof(TSource), typeof(TTarget));
            if (_converters.TryGetValue(key, out var existingConverter)) {
                return (Expression<Func<TSource, TTarget>>)existingConverter;
            }

            if ((!useDefaultMapIfTypeMapIsMissing && _useDefaultMapIfTypeMapIsMissing) || useDefaultMapIfTypeMapIsMissing) {
                if (_defaultMapCache.TryGetValue(key, out var map)) {
                    return (Expression<Func<TSource, TTarget>>)map;
                }

                var defaultMap = Mapper.CreateMap<TSource, TTarget>();
                _defaultMapCache[key] = defaultMap;
                return defaultMap;
            }

            throw new ArgumentException($"Missing type map configuration for TSource ({typeof(TSource).FullName}) to TTarget ({typeof(TTarget).FullName})");
        }

        public void Map<TSource, TTarget>(TSource source, TTarget target, bool useDefaultMapIfTypeMapIsMissing = false) {
            var key = new Tuple<Type, Type>(typeof(TSource), typeof(TTarget));
            if (_compiledMapToExistingCache.TryGetValue(key, out var map)) {
                ((Action<TSource, TTarget>)map).Invoke(source, target);
                return;
            }

            var expression = GetMap<TSource, TTarget>(useDefaultMapIfTypeMapIsMissing);

            if (expression.Body is not MemberInitExpression) {
                throw new NotSupportedException($"Mapping from TSource ({typeof(TSource).FullName}) to TTarget ({typeof(TTarget).FullName}) cannot map to an existing target instance because the map does not use an object initializer (x => new TTarget {{ ... }}). Use Map(source) instead.");
            }

            var compiled = Mapper.CompileMapper(expression);
            _compiledMapToExistingCache[key] = compiled;
            compiled.Invoke(source, target);
        }

        public TTarget Map<TSource, TTarget>(TSource source, bool useDefaultMapIfTypeMapIsMissing = false) {
            var key = new Tuple<Type, Type>(typeof(TSource), typeof(TTarget));
            if (_compiledMapToNewCache.TryGetValue(key, out var map)) {
                return ((Func<TSource, TTarget>)map).Invoke(source);
            }

            var expression = GetMap<TSource, TTarget>(useDefaultMapIfTypeMapIsMissing);
            var compiled = expression.Compile();
            _compiledMapToNewCache[key] = compiled;
            return compiled.Invoke(source);
        }
    }
}
