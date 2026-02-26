using System.Linq.Expressions;

namespace Mapify.NET {
    /// <summary>
    /// Instance-based mapper that builds and caches mapping expressions from configured profiles.
    /// </summary>
    public class Mapify : IMapify, IMapifyConfigurator {
        private bool _useDefaultMapIfTypeMapIsMissing;

        private readonly Dictionary<MapKey, PendingMapRegistration> _pendingRegistrations = [];

        private readonly Dictionary<MapKey, MapBuildState> _buildStates = [];

        private readonly Dictionary<MapKey, LambdaExpression> _converters = [];

        private readonly Dictionary<Tuple<Type, Type>, LambdaExpression> _defaultMapCache = [];

        private readonly Dictionary<MapKey, Delegate> _compiledMapToExistingCache = [];

        private readonly Dictionary<MapKey, Delegate> _compiledMapToNewCache = [];

        /// <summary>
        /// Creates a mapper instance and applies the provided profiles.
        /// </summary>
        /// <param name="profiles">Profiles to apply.</param>
        public Mapify(params MapifyProfile[] profiles)
            : this((IEnumerable<MapifyProfile>)profiles) {
        }

        /// <summary>
        /// Creates a mapper instance and optionally applies the provided profiles.
        /// </summary>
        /// <param name="profiles">Profiles to apply, or <c>null</c> to create an empty mapper.</param>
        public Mapify(IEnumerable<MapifyProfile>? profiles = null) {
            if (profiles == null) {
                return;
            }

            foreach (var profile in profiles) {
                profile.Apply(this);
            }

            BuildRegisteredMaps();
        }

        /// <summary>
        /// Configures whether a default map should be generated when an explicit type map is missing.
        /// </summary>
        /// <param name="value">True to enable default-map fallback; otherwise false.</param>
        public void UseDefaultMapIfTypeMapIsMissing(bool value) {
            _useDefaultMapIfTypeMapIsMissing = value;
        }

        void IMapifyConfigurator.CreateMap<TSource, TTarget>(Expression<Func<TSource, TTarget>>? partial) {
            AddPendingMap(null, partial);
        }

        void IMapifyConfigurator.CreateMap<TSource, TTarget>(string name, Expression<Func<TSource, TTarget>>? partial) {
            if (string.IsNullOrWhiteSpace(name)) {
                throw new ArgumentException("Mapping name must not be null or whitespace.", nameof(name));
            }

            AddPendingMap(name, partial);
        }

        private void AddPendingMap<TSource, TTarget>(string? name, Expression<Func<TSource, TTarget>>? partial) {
            var key = new MapKey(typeof(TSource), typeof(TTarget), name);
            if (_pendingRegistrations.ContainsKey(key) || _converters.ContainsKey(key)) {
                var mappingScope = name == null ? "default" : $"named '{name}'";
                throw new ArgumentException($"There already exists a {mappingScope} mapping from TSource ({typeof(TSource).FullName}) to TTarget ({typeof(TTarget).FullName}). There can only be one mapping per name and source/target combination.");
            }

            _pendingRegistrations[key] = new PendingMapRegistration(partial);
        }

        private void BuildRegisteredMaps() {
            var keys = _pendingRegistrations.Keys.ToArray();
            foreach (var key in keys) {
                EnsureBuilt(key);
            }
        }

        private void EnsureBuilt(MapKey key) {
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
                    var mappingScope = key.Name == null ? "default" : $"named '{key.Name}'";
                    throw new InvalidOperationException($"Cyclic mapping dependency detected while building {mappingScope} map from TSource ({key.SourceType.FullName}) to TTarget ({key.TargetType.FullName}).");
                }
            }

            _buildStates[key] = MapBuildState.Building;
            try {
                if (pending.Partial != null && pending.Partial.Body is not MemberInitExpression) {
                    AddMapUntyped(pending.Partial, key.Name);
                } else {
                    var created = CreateMapFromPending(key.SourceType, key.TargetType, key.Name, pending.Partial);
                    AddMapUntyped(created, key.Name);
                }

                _pendingRegistrations.Remove(key);
                _buildStates[key] = MapBuildState.Built;
            } finally {
                if (_buildStates.TryGetValue(key, out var currentState) && currentState == MapBuildState.Building) {
                    _buildStates[key] = MapBuildState.NotBuilt;
                }
            }
        }

        private LambdaExpression CreateMapFromPending(Type sourceType, Type targetType, string? mapName, LambdaExpression? partial) {
            var method = typeof(Mapify).GetMethod(nameof(CreateMapFromPendingGeneric), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
            var generic = method.MakeGenericMethod(sourceType, targetType);
            return (LambdaExpression)generic.Invoke(this, [mapName, partial])!;
        }

        private Expression<Func<TSource, TTarget>> CreateMapFromPendingGeneric<TSource, TTarget>(string? mapName, LambdaExpression? partial)
            => Mapper.CreateMap((Expression<Func<TSource, TTarget>>?)partial, (sourceType, targetType, requestedMapName) => ResolveExistingMapForBuild(sourceType, targetType, requestedMapName ?? mapName));

        private LambdaExpression? ResolveExistingMapForBuild(Type sourceType, Type targetType, string? mapName) {
            if (!string.IsNullOrWhiteSpace(mapName)) {
                var namedKey = new MapKey(sourceType, targetType, mapName);
                if (_converters.TryGetValue(namedKey, out var namedConverter)) {
                    return namedConverter;
                }

                if (_pendingRegistrations.ContainsKey(namedKey)) {
                    if (_buildStates.TryGetValue(namedKey, out var state) && state == MapBuildState.Building) {
                        return null;
                    }

                    EnsureBuilt(namedKey);
                    if (_converters.TryGetValue(namedKey, out namedConverter)) {
                        return namedConverter;
                    }
                }
            }

            var key = new MapKey(sourceType, targetType, null);

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

        private void AddMapUntyped(LambdaExpression mappingExpression, string? name) {
            var sourceType = mappingExpression.Parameters[0].Type;
            var targetType = mappingExpression.ReturnType;

            var method = typeof(Mapify).GetMethod(nameof(AddMapGeneric), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
            var generic = method.MakeGenericMethod(sourceType, targetType);
            generic.Invoke(this, [mappingExpression, name]);
        }

        private void AddMapGeneric<TSource, TTarget>(LambdaExpression mappingExpression, string? name)
            => AddMap((Expression<Func<TSource, TTarget>>)mappingExpression, name);

        private void AddMap<TSource, TTarget>(Expression<Func<TSource, TTarget>> mappingExpression, string? name = null) {
            var key = new MapKey(typeof(TSource), typeof(TTarget), name);
            if (_converters.ContainsKey(key)) {
                var mappingScope = name == null ? "default" : $"named '{name}'";
                throw new ArgumentException($"There already exists a {mappingScope} mapping from TSource ({typeof(TSource).FullName}) to TTarget ({typeof(TTarget).FullName}). There can only be one mapping per name and source/target combination.");
            }

            _converters[key] = mappingExpression;
            _compiledMapToNewCache[key] = mappingExpression.Compile();

            if (mappingExpression.Body is MemberInitExpression) {
                _compiledMapToExistingCache[key] = Mapper.CompileMapper(mappingExpression);
            }
        }

        /// <summary>
        /// Gets the named map expression for the source and target types.
        /// Returns <c>null</c> when the named map is missing.
        /// </summary>
        /// <typeparam name="TSource">The source type.</typeparam>
        /// <typeparam name="TTarget">The target type.</typeparam>
        /// <param name="name">The map name.</param>
        /// <returns>The mapping expression, or <c>null</c> if not found.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null, empty, or whitespace.</exception>
        public Expression<Func<TSource, TTarget>>? GetMap<TSource, TTarget>(string name) {
            if (string.IsNullOrWhiteSpace(name)) {
                throw new ArgumentException("Mapping name must not be null or whitespace.", nameof(name));
            }

            var key = new MapKey(typeof(TSource), typeof(TTarget), name);
            if (_converters.TryGetValue(key, out var existingConverter)) {
                return (Expression<Func<TSource, TTarget>>)existingConverter;
            }

            return null;
        }

        /// <summary>
        /// Gets the named map expression for the source and target types, throwing if it is missing.
        /// </summary>
        /// <typeparam name="TSource">The source type.</typeparam>
        /// <typeparam name="TTarget">The target type.</typeparam>
        /// <param name="name">The map name.</param>
        /// <returns>The required named mapping expression.</returns>
        /// <exception cref="ArgumentException">Thrown when the name is invalid or the named map is missing.</exception>
        public Expression<Func<TSource, TTarget>> GetRequiredMap<TSource, TTarget>(string name) {
            var map = GetMap<TSource, TTarget>(name);
            if (map != null) {
                return map;
            }

            throw new ArgumentException($"Missing named type map configuration '{name}' for TSource ({typeof(TSource).FullName}) to TTarget ({typeof(TTarget).FullName})");
        }

        /// <summary>
        /// Gets the default map expression for the source and target types.
        /// Returns <c>null</c> when no map exists and default-map fallback is disabled.
        /// </summary>
        /// <typeparam name="TSource">The source type.</typeparam>
        /// <typeparam name="TTarget">The target type.</typeparam>
        /// <param name="useDefaultMapIfTypeMapIsMissing">Whether to allow automatic default-map creation for this call.</param>
        /// <returns>The mapping expression, or <c>null</c> if not found and fallback is disabled.</returns>
        public Expression<Func<TSource, TTarget>>? GetMap<TSource, TTarget>(bool useDefaultMapIfTypeMapIsMissing = false) {
            var key = new MapKey(typeof(TSource), typeof(TTarget), null);
            if (_converters.TryGetValue(key, out var existingConverter)) {
                return (Expression<Func<TSource, TTarget>>)existingConverter;
            }

            if ((!useDefaultMapIfTypeMapIsMissing && _useDefaultMapIfTypeMapIsMissing) || useDefaultMapIfTypeMapIsMissing) {
                var defaultCacheKey = new Tuple<Type, Type>(typeof(TSource), typeof(TTarget));
                if (_defaultMapCache.TryGetValue(defaultCacheKey, out var existingDefaultMap)) {
                    return (Expression<Func<TSource, TTarget>>)existingDefaultMap;
                }

                var defaultMap = Mapper.CreateMap<TSource, TTarget>(null, (sourceType, targetType, requestedMapName) => ResolveExistingMapForBuild(sourceType, targetType, requestedMapName));
                _defaultMapCache[defaultCacheKey] = defaultMap;
                return defaultMap;
            }

            return null;
        }

        /// <summary>
        /// Gets the default map expression for the source and target types, throwing if none is available.
        /// </summary>
        /// <typeparam name="TSource">The source type.</typeparam>
        /// <typeparam name="TTarget">The target type.</typeparam>
        /// <param name="useDefaultMapIfTypeMapIsMissing">Whether to allow automatic default-map creation for this call.</param>
        /// <returns>The required mapping expression.</returns>
        /// <exception cref="ArgumentException">Thrown when no map is available.</exception>
        public Expression<Func<TSource, TTarget>> GetRequiredMap<TSource, TTarget>(bool useDefaultMapIfTypeMapIsMissing = false) {
            var map = GetMap<TSource, TTarget>(useDefaultMapIfTypeMapIsMissing);
            if (map != null) {
                return map;
            }

            throw new ArgumentException($"Missing type map configuration for TSource ({typeof(TSource).FullName}) to TTarget ({typeof(TTarget).FullName})");
        }

        /// <summary>
        /// Maps values from the source object to an existing target object using a named map.
        /// </summary>
        /// <typeparam name="TSource">The source type.</typeparam>
        /// <typeparam name="TTarget">The target type.</typeparam>
        /// <param name="source">The source object.</param>
        /// <param name="target">The target object to update.</param>
        /// <param name="name">The map name.</param>
        /// <exception cref="ArgumentException">Thrown when the name is invalid or the named map is missing.</exception>
        /// <exception cref="NotSupportedException">Thrown when the map cannot target an existing instance.</exception>
        public void Map<TSource, TTarget>(TSource source, TTarget target, string name) {
            if (string.IsNullOrWhiteSpace(name)) {
                throw new ArgumentException("Mapping name must not be null or whitespace.", nameof(name));
            }

            var key = new MapKey(typeof(TSource), typeof(TTarget), name);
            if (_compiledMapToExistingCache.TryGetValue(key, out var map)) {
                ((Action<TSource, TTarget>)map).Invoke(source, target);
                return;
            }

            var expression = GetRequiredMap<TSource, TTarget>(name);

            if (expression.Body is not MemberInitExpression) {
                throw new NotSupportedException($"Mapping from TSource ({typeof(TSource).FullName}) to TTarget ({typeof(TTarget).FullName}) cannot map to an existing target instance because the map does not use an object initializer (x => new TTarget {{ ... }}). Use Map(source, name) instead.");
            }

            var compiled = Mapper.CompileMapper(expression);
            _compiledMapToExistingCache[key] = compiled;
            compiled.Invoke(source, target);
        }

        /// <summary>
        /// Maps values from the source object to an existing target object using the default map.
        /// </summary>
        /// <typeparam name="TSource">The source type.</typeparam>
        /// <typeparam name="TTarget">The target type.</typeparam>
        /// <param name="source">The source object.</param>
        /// <param name="target">The target object to update.</param>
        /// <param name="useDefaultMapIfTypeMapIsMissing">Whether to allow automatic default-map creation for this call.</param>
        /// <exception cref="ArgumentException">Thrown when no map is available.</exception>
        /// <exception cref="NotSupportedException">Thrown when the map cannot target an existing instance.</exception>
        public void Map<TSource, TTarget>(TSource source, TTarget target, bool useDefaultMapIfTypeMapIsMissing = false) {
            var key = new MapKey(typeof(TSource), typeof(TTarget), null);
            if (_compiledMapToExistingCache.TryGetValue(key, out var map)) {
                ((Action<TSource, TTarget>)map).Invoke(source, target);
                return;
            }

            var expression = GetRequiredMap<TSource, TTarget>(useDefaultMapIfTypeMapIsMissing);

            if (expression.Body is not MemberInitExpression) {
                throw new NotSupportedException($"Mapping from TSource ({typeof(TSource).FullName}) to TTarget ({typeof(TTarget).FullName}) cannot map to an existing target instance because the map does not use an object initializer (x => new TTarget {{ ... }}). Use Map(source) instead.");
            }

            var compiled = Mapper.CompileMapper(expression);
            _compiledMapToExistingCache[key] = compiled;
            compiled.Invoke(source, target);
        }

        /// <summary>
        /// Maps the source object to a new target object using a named map.
        /// </summary>
        /// <typeparam name="TSource">The source type.</typeparam>
        /// <typeparam name="TTarget">The target type.</typeparam>
        /// <param name="source">The source object.</param>
        /// <param name="name">The map name.</param>
        /// <returns>A new mapped target object.</returns>
        /// <exception cref="ArgumentException">Thrown when the name is invalid or the named map is missing.</exception>
        public TTarget Map<TSource, TTarget>(TSource source, string name) {
            if (string.IsNullOrWhiteSpace(name)) {
                throw new ArgumentException("Mapping name must not be null or whitespace.", nameof(name));
            }

            var key = new MapKey(typeof(TSource), typeof(TTarget), name);
            if (_compiledMapToNewCache.TryGetValue(key, out var map)) {
                return ((Func<TSource, TTarget>)map).Invoke(source);
            }

            var expression = GetRequiredMap<TSource, TTarget>(name);
            var compiled = expression.Compile();
            _compiledMapToNewCache[key] = compiled;
            return compiled.Invoke(source);
        }

        /// <summary>
        /// Maps the source object to a new target object using the default map.
        /// </summary>
        /// <typeparam name="TSource">The source type.</typeparam>
        /// <typeparam name="TTarget">The target type.</typeparam>
        /// <param name="source">The source object.</param>
        /// <param name="useDefaultMapIfTypeMapIsMissing">Whether to allow automatic default-map creation for this call.</param>
        /// <returns>A new mapped target object.</returns>
        /// <exception cref="ArgumentException">Thrown when no map is available.</exception>
        public TTarget Map<TSource, TTarget>(TSource source, bool useDefaultMapIfTypeMapIsMissing = false) {
            var key = new MapKey(typeof(TSource), typeof(TTarget), null);
            if (_compiledMapToNewCache.TryGetValue(key, out var map)) {
                return ((Func<TSource, TTarget>)map).Invoke(source);
            }

            var expression = GetRequiredMap<TSource, TTarget>(useDefaultMapIfTypeMapIsMissing);
            var compiled = expression.Compile();
            _compiledMapToNewCache[key] = compiled;
            return compiled.Invoke(source);
        }

        private readonly struct MapKey(Type sourceType, Type targetType, string? name) : IEquatable<MapKey> {
            public Type SourceType { get; } = sourceType;

            public Type TargetType { get; } = targetType;

            public string? Name { get; } = name;

            public bool Equals(MapKey other)
                => SourceType == other.SourceType
                   && TargetType == other.TargetType
                   && string.Equals(Name, other.Name, StringComparison.Ordinal);

            public override bool Equals(object? obj)
                => obj is MapKey other && Equals(other);

            public override int GetHashCode() {
                unchecked {
                    var hash = 17;
                    hash = (hash * 23) + SourceType.GetHashCode();
                    hash = (hash * 23) + TargetType.GetHashCode();
                    hash = (hash * 23) + (Name != null ? StringComparer.Ordinal.GetHashCode(Name) : 0);
                    return hash;
                }
            }
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
