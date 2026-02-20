using System.Linq.Expressions;

namespace Mapify.NET {
    public class Mapify : IMapify, IMapifyConfigurator {
        private bool _useDefaultMapIfTypeMapIsMissing;

        private readonly IDictionary<Tuple<Type, Type>, PendingMapRegistration> _pendingRegistrations = new Dictionary<Tuple<Type, Type>, PendingMapRegistration>();

        private readonly IDictionary<Tuple<Type, Type>, MapBuildState> _buildStates = new Dictionary<Tuple<Type, Type>, MapBuildState>();

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

            BuildRegisteredMaps();
        }

        public void UseDefaultMapIfTypeMapIsMissing(bool value) {
            _useDefaultMapIfTypeMapIsMissing = value;
        }

        void IMapifyConfigurator.CreateMap<TSource, TTarget>(Expression<Func<TSource, TTarget>>? partial) {
            var key = new Tuple<Type, Type>(typeof(TSource), typeof(TTarget));
            if (_pendingRegistrations.ContainsKey(key) || _converters.ContainsKey(key)) {
                throw new ArgumentException($"There already exists a mapping from TSource ({typeof(TSource).FullName}) to TTarget ({typeof(TTarget).FullName}). There can only be one mapping for each combination of TSource and TTarget.");
            }

            if (partial != null && partial.Body is not MemberInitExpression) {
                _pendingRegistrations[key] = new PendingMapRegistration(partial);
                return;
            }

            _pendingRegistrations[key] = new PendingMapRegistration(partial);
        }

        private void BuildRegisteredMaps() {
            var keys = _pendingRegistrations.Keys.ToArray();
            foreach (var key in keys) {
                EnsureBuilt(key);
            }
        }

        private void EnsureBuilt(Tuple<Type, Type> key) {
            if (_converters.ContainsKey(key)) {
                return;
            }

            if (!_pendingRegistrations.TryGetValue(key, out var pending)) {
                return;
            }

            if (_buildStates.TryGetValue(key, out var state)) {
                if (state == MapBuildState.Built) {
                    return;
                }

                if (state == MapBuildState.Building) {
                    throw new InvalidOperationException($"Cyclic mapping dependency detected while building map from TSource ({key.Item1.FullName}) to TTarget ({key.Item2.FullName}).");
                }
            }

            _buildStates[key] = MapBuildState.Building;
            try {
                if (pending.Partial != null && pending.Partial.Body is not MemberInitExpression) {
                    AddMapUntyped(pending.Partial);
                } else {
                    var created = CreateMapFromPending(key.Item1, key.Item2, pending.Partial);
                    AddMapUntyped(created);
                }

                _pendingRegistrations.Remove(key);
                _buildStates[key] = MapBuildState.Built;
            } finally {
                if (_buildStates.TryGetValue(key, out var currentState) && currentState == MapBuildState.Building) {
                    _buildStates[key] = MapBuildState.NotBuilt;
                }
            }
        }

        private LambdaExpression CreateMapFromPending(Type sourceType, Type targetType, LambdaExpression? partial) {
            var method = typeof(Mapify).GetMethod(nameof(CreateMapFromPendingGeneric), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
            var generic = method.MakeGenericMethod(sourceType, targetType);
            return (LambdaExpression)generic.Invoke(this, new object?[] { partial })!;
        }

        private Expression<Func<TSource, TTarget>> CreateMapFromPendingGeneric<TSource, TTarget>(LambdaExpression? partial)
            => Mapper.CreateMap((Expression<Func<TSource, TTarget>>?)partial, ResolveExistingMapForBuild);

        private LambdaExpression? ResolveExistingMapForBuild(Type sourceType, Type targetType) {
            var key = new Tuple<Type, Type>(sourceType, targetType);

            if (_converters.TryGetValue(key, out var existingConverter)) {
                return existingConverter;
            }

            if (_pendingRegistrations.ContainsKey(key)) {
                if (_buildStates.TryGetValue(key, out var state) && state == MapBuildState.Building) {
                    return null;
                }

                EnsureBuilt(key);
                if (_converters.TryGetValue(key, out existingConverter)) {
                    return existingConverter;
                }
            }

            return null;
        }

        private void AddMapUntyped(LambdaExpression mappingExpression) {
            var sourceType = mappingExpression.Parameters[0].Type;
            var targetType = mappingExpression.ReturnType;

            var method = typeof(Mapify).GetMethod(nameof(AddMapGeneric), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
            var generic = method.MakeGenericMethod(sourceType, targetType);
            generic.Invoke(this, new object[] { mappingExpression });
        }

        private void AddMapGeneric<TSource, TTarget>(LambdaExpression mappingExpression)
            => AddMap((Expression<Func<TSource, TTarget>>)mappingExpression);

        private void AddMap<TSource, TTarget>(Expression<Func<TSource, TTarget>> mappingExpression) {
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

        public Expression<Func<TSource, TTarget>> GetMap<TSource, TTarget>(bool useDefaultMapIfTypeMapIsMissing = false) {
            var key = new Tuple<Type, Type>(typeof(TSource), typeof(TTarget));
            if (_converters.TryGetValue(key, out var existingConverter)) {
                return (Expression<Func<TSource, TTarget>>)existingConverter;
            }

            if ((!useDefaultMapIfTypeMapIsMissing && _useDefaultMapIfTypeMapIsMissing) || useDefaultMapIfTypeMapIsMissing) {
                if (_defaultMapCache.TryGetValue(key, out var map)) {
                    return (Expression<Func<TSource, TTarget>>)map;
                }

                var defaultMap = Mapper.CreateMap<TSource, TTarget>(null, ResolveExistingMapForBuild);
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

        private sealed class PendingMapRegistration(LambdaExpression? partial) {
            public LambdaExpression? Partial { get; } = partial;
        }

        private enum MapBuildState {
            NotBuilt = 0,
            Building = 1,
            Built = 2
        }
    }
}
