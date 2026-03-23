using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace Mapify.NET;

/// <summary>
/// Instance-based mapper that builds and caches mapping expressions from configured profiles.
/// </summary>
public partial class Mapify : IMapify, IMapifyConfigurator {
    private bool _useDefaultMapIfTypeMapIsMissing;

    private const int _defaultRecursiveUseMapDepth = 6;

    private const int _defaultRecursiveMapBuildHardCap = 10;

    private int _recursiveMapBuildHardCap = _defaultRecursiveMapBuildHardCap;

    private readonly Dictionary<MapKey, PendingMapRegistration> _pendingRegistrations = [];

    private readonly Dictionary<MapKey, MapBuildState> _buildStates = [];

    private readonly Dictionary<MapKey, LambdaExpression> _converters = [];

    private readonly Dictionary<Tuple<Type, Type>, LambdaExpression> _defaultMapCache = [];

    private readonly Dictionary<MapKey, Delegate> _compiledMapToExistingCache = [];

    private readonly Dictionary<MapKey, Delegate> _compiledMapToNewCache = [];

    private static readonly IReadOnlyDictionary<string, object?> _emptyParameters = new Dictionary<string, object?>();

    private static void ValidateRuntimeParameters(IReadOnlyDictionary<string, object?> parameters) {
        if (parameters == null) {
            throw new ArgumentNullException(nameof(parameters));
        }

        if (parameters.Count == 0 && !ReferenceEquals(parameters, _emptyParameters)) {
            throw new ArgumentException("At least one runtime parameter must be provided when using a parameterized overload.", nameof(parameters));
        }
    }

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

    /// <summary>
    /// Configures the hard cap used for recursive map expansion during map building.
    /// Explicit <c>UseMap(..., depth)</c> marker depths above this value throw during map build.
    /// </summary>
    /// <param name="value">The maximum allowed recursive expansion depth. Must be positive.</param>
    public void UseMaxRecursiveMapBuildDepth(int value) {
        if (value <= 0) {
            throw new ArgumentOutOfRangeException(nameof(value), "Recursive map build depth hard cap must be greater than zero.");
        }

        _recursiveMapBuildHardCap = value;
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
        var insertedFallback = false;
        try {
            var recursiveBuildDepth = DetermineRecursiveBuildDepth(pending.Partial, key);

            if (!_converters.ContainsKey(key)) {
                var fallback = CreateRecursiveFallbackMap(key.SourceType, key.TargetType);
                SetMapUntyped(fallback, key.Name, compileCaches: false);
                insertedFallback = true;
            }

            LambdaExpression created;
            if (pending.Partial != null && pending.Partial.Body is not MemberInitExpression) {
                created = pending.Partial;
                SetMapUntyped(created, key.Name, compileCaches: false);
            } else {
                created = null!;
                for (var i = 0; i < recursiveBuildDepth; i++) {
                    created = CreateMapFromPending(key.SourceType, key.TargetType, key.Name, pending.Partial);
                    SetMapUntyped(created, key.Name, compileCaches: false);
                }
            }

            SetMapUntyped(created, key.Name, compileCaches: true);

            _pendingRegistrations.Remove(key);
            _buildStates[key] = MapBuildState.Built;
        } finally {
            if (_buildStates.TryGetValue(key, out var finalState)
                && finalState != MapBuildState.Built
                && insertedFallback
                && _pendingRegistrations.ContainsKey(key)) {
                _converters.Remove(key);
                _compiledMapToExistingCache.Remove(key);
                _compiledMapToNewCache.Remove(key);
            }

            if (_buildStates.TryGetValue(key, out var currentState) && currentState == MapBuildState.Building) {
                _buildStates[key] = MapBuildState.NotBuilt;
            }
        }
    }

    private int DetermineRecursiveBuildDepth(LambdaExpression? partial, MapKey key) {
        var fallbackDepth = Math.Min(_defaultRecursiveUseMapDepth, _recursiveMapBuildHardCap);
        if (partial == null) {
            return fallbackDepth;
        }

        var markerInfo = UseMapDepthMarkerVisitor.Extract(partial);
        if (!markerInfo.HasUseMapMarkers) {
            return fallbackDepth;
        }

        if (markerInfo.MaxExplicitDepth > _recursiveMapBuildHardCap) {
            var mappingScope = key.Name == null ? "default" : $"named '{key.Name}'";
            throw new InvalidOperationException(
                $"UseMap depth {markerInfo.MaxExplicitDepth} exceeds the configured hard cap {_recursiveMapBuildHardCap} while building {mappingScope} map from TSource ({key.SourceType.FullName}) to TTarget ({key.TargetType.FullName}).");
        }

        var requestedDepth = 0;
        if (markerInfo.HasDepthlessUseMapMarkers) {
            requestedDepth = Math.Max(requestedDepth, _defaultRecursiveUseMapDepth);
        }

        if (markerInfo.MaxExplicitDepth > 0) {
            requestedDepth = Math.Max(requestedDepth, markerInfo.MaxExplicitDepth);
        }

        if (requestedDepth <= 0) {
            requestedDepth = _defaultRecursiveUseMapDepth;
        }

        return Math.Min(requestedDepth, _recursiveMapBuildHardCap);
    }

    private LambdaExpression CreateRecursiveFallbackMap(Type sourceType, Type targetType) {
        var method = typeof(Mapify).GetMethod(nameof(CreateRecursiveFallbackMapGeneric), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!;
        var generic = method.MakeGenericMethod(sourceType, targetType);
        return (LambdaExpression)generic.Invoke(null, null)!;
    }

    private static Expression<Func<TSource, TTarget>> CreateRecursiveFallbackMapGeneric<TSource, TTarget>()
        => _ => default!;

    private LambdaExpression CreateMapFromPending(Type sourceType, Type targetType, string? mapName, LambdaExpression? partial) {
        var method = typeof(Mapify).GetMethod(nameof(CreateMapFromPendingGeneric), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        var generic = method.MakeGenericMethod(sourceType, targetType);
        return (LambdaExpression)generic.Invoke(this, [mapName, partial])!;
    }

    private Expression<Func<TSource, TTarget>> CreateMapFromPendingGeneric<TSource, TTarget>(string? mapName, LambdaExpression? partial)
        => CreateMap((Expression<Func<TSource, TTarget>>?)partial, (sourceType, targetType, requestedMapName) => ResolveExistingMapForBuild(sourceType, targetType, requestedMapName ?? mapName));

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

    private void SetMapUntyped(LambdaExpression mappingExpression, string? name, bool compileCaches) {
        var sourceType = mappingExpression.Parameters[0].Type;
        var targetType = mappingExpression.ReturnType;

        var method = typeof(Mapify).GetMethod(nameof(SetMapGeneric), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        var generic = method.MakeGenericMethod(sourceType, targetType);
        generic.Invoke(this, [mappingExpression, name, compileCaches]);
    }

    private void AddMapGeneric<TSource, TTarget>(LambdaExpression mappingExpression, string? name)
        => AddMap((Expression<Func<TSource, TTarget>>)mappingExpression, name);

    private void SetMapGeneric<TSource, TTarget>(LambdaExpression mappingExpression, string? name, bool compileCaches) {
        var key = new MapKey(typeof(TSource), typeof(TTarget), name);
        var expression = (Expression<Func<TSource, TTarget>>)mappingExpression;

        _converters[key] = expression;
        _compiledMapToExistingCache.Remove(key);
        _compiledMapToNewCache.Remove(key);

        if (!compileCaches) {
            return;
        }

        if (!ContainsParameterMarkers(expression)) {
            _compiledMapToNewCache[key] = expression.Compile();
        }

        if (expression.Body is MemberInitExpression && !ContainsParameterMarkers(expression)) {
            _compiledMapToExistingCache[key] = CompileMapper(expression);
        }
    }

    private void AddMap<TSource, TTarget>(Expression<Func<TSource, TTarget>> mappingExpression, string? name = null) {
        var key = new MapKey(typeof(TSource), typeof(TTarget), name);
        if (_converters.ContainsKey(key)) {
            var mappingScope = name == null ? "default" : $"named '{name}'";
            throw new ArgumentException($"There already exists a {mappingScope} mapping from TSource ({typeof(TSource).FullName}) to TTarget ({typeof(TTarget).FullName}). There can only be one mapping per name and source/target combination.");
        }

        _converters[key] = mappingExpression;
        if (!ContainsParameterMarkers(mappingExpression)) {
            _compiledMapToNewCache[key] = mappingExpression.Compile();
        }

        if (mappingExpression.Body is MemberInitExpression && !ContainsParameterMarkers(mappingExpression)) {
            _compiledMapToExistingCache[key] = CompileMapper(mappingExpression);
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
        return GetMap<TSource, TTarget>(name, _emptyParameters);
    }

    /// <summary>
    /// Gets the named map expression for the source and target types and replaces runtime parameters.
    /// Returns <c>null</c> when the named map is missing.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TTarget">The target type.</typeparam>
    /// <param name="name">The map name.</param>
    /// <param name="parameters">Runtime parameters used by <see cref="MapifyProfile"/> <c>Parameter&lt;T&gt;(name)</c> markers.</param>
    /// <returns>The mapping expression, or <c>null</c> if not found.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null, empty, or whitespace.</exception>
    public Expression<Func<TSource, TTarget>>? GetMap<TSource, TTarget>(string name, IReadOnlyDictionary<string, object?> parameters) {
        if (string.IsNullOrWhiteSpace(name)) {
            throw new ArgumentException("Mapping name must not be null or whitespace.", nameof(name));
        }

        ValidateRuntimeParameters(parameters);

        var key = new MapKey(typeof(TSource), typeof(TTarget), name);
        if (_converters.TryGetValue(key, out var existingConverter)) {
            return ApplyParameters((Expression<Func<TSource, TTarget>>)existingConverter, parameters);
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
        return GetRequiredMap<TSource, TTarget>(name, _emptyParameters);
    }

    /// <summary>
    /// Gets the named map expression for the source and target types, replaces runtime parameters, and throws if it is missing.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TTarget">The target type.</typeparam>
    /// <param name="name">The map name.</param>
    /// <param name="parameters">Runtime parameters used by <see cref="MapifyProfile"/> <c>Parameter&lt;T&gt;(name)</c> markers.</param>
    /// <returns>The required named mapping expression.</returns>
    /// <exception cref="ArgumentException">Thrown when the name is invalid or the named map is missing.</exception>
    public Expression<Func<TSource, TTarget>> GetRequiredMap<TSource, TTarget>(string name, IReadOnlyDictionary<string, object?> parameters) {
        var map = GetMap<TSource, TTarget>(name, parameters);
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
    /// <returns>The mapping expression, or <c>null</c> if not found and fallback is disabled.</returns>
    public Expression<Func<TSource, TTarget>>? GetMap<TSource, TTarget>() {
        return GetMap<TSource, TTarget>(_emptyParameters);
    }

    /// <summary>
    /// Gets the default map expression for the source and target types and replaces runtime parameters.
    /// Returns <c>null</c> when no map exists and default-map fallback is disabled.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TTarget">The target type.</typeparam>
    /// <param name="parameters">Runtime parameters used by <see cref="MapifyProfile"/> <c>Parameter&lt;T&gt;(name)</c> markers.</param>
    /// <returns>The mapping expression, or <c>null</c> if not found and fallback is disabled.</returns>
    public Expression<Func<TSource, TTarget>>? GetMap<TSource, TTarget>(IReadOnlyDictionary<string, object?> parameters) {
        ValidateRuntimeParameters(parameters);

        var key = new MapKey(typeof(TSource), typeof(TTarget), null);
        if (_converters.TryGetValue(key, out var existingConverter)) {
            return ApplyParameters((Expression<Func<TSource, TTarget>>)existingConverter, parameters);
        }

        if (_useDefaultMapIfTypeMapIsMissing) {
            var defaultCacheKey = new Tuple<Type, Type>(typeof(TSource), typeof(TTarget));
            if (_defaultMapCache.TryGetValue(defaultCacheKey, out var existingDefaultMap)) {
                return ApplyParameters((Expression<Func<TSource, TTarget>>)existingDefaultMap, parameters);
            }

            var defaultMap = CreateMap<TSource, TTarget>(null, (sourceType, targetType, requestedMapName) => ResolveExistingMapForBuild(sourceType, targetType, requestedMapName));
            _defaultMapCache[defaultCacheKey] = defaultMap;
            return ApplyParameters(defaultMap, parameters);
        }

        return null;
    }

    /// <summary>
    /// Gets the default map expression for the source and target types, throwing if none is available.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TTarget">The target type.</typeparam>
    /// <returns>The required mapping expression.</returns>
    /// <exception cref="ArgumentException">Thrown when no map is available.</exception>
    public Expression<Func<TSource, TTarget>> GetRequiredMap<TSource, TTarget>() {
        return GetRequiredMap<TSource, TTarget>(_emptyParameters);
    }

    /// <summary>
    /// Gets the default map expression for the source and target types, replaces runtime parameters, and throws if none is available.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TTarget">The target type.</typeparam>
    /// <param name="parameters">Runtime parameters used by <see cref="MapifyProfile"/> <c>Parameter&lt;T&gt;(name)</c> markers.</param>
    /// <returns>The required mapping expression.</returns>
    /// <exception cref="ArgumentException">Thrown when no map is available.</exception>
    public Expression<Func<TSource, TTarget>> GetRequiredMap<TSource, TTarget>(IReadOnlyDictionary<string, object?> parameters) {
        var map = GetMap<TSource, TTarget>(parameters);
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

        var expression = GetRequiredRuntimeMap<TSource, TTarget>(name, _emptyParameters);

        if (expression.Body is not MemberInitExpression) {
            throw new NotSupportedException($"Mapping from TSource ({typeof(TSource).FullName}) to TTarget ({typeof(TTarget).FullName}) cannot map to an existing target instance because the map does not use an object initializer (x => new TTarget {{ ... }}). Use Map(source, name) instead.");
        }

        var compiled = CompileMapper(expression);
        _compiledMapToExistingCache[key] = compiled;
        compiled.Invoke(source, target);
    }

    /// <summary>
    /// Maps values from the source object to an existing target object using a named map and runtime parameters.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TTarget">The target type.</typeparam>
    /// <param name="source">The source object.</param>
    /// <param name="target">The target object to update.</param>
    /// <param name="name">The map name.</param>
    /// <param name="parameters">Runtime parameters used by <see cref="MapifyProfile"/> <c>Parameter&lt;T&gt;(name)</c> markers.</param>
    /// <exception cref="ArgumentException">Thrown when the name is invalid or the named map is missing.</exception>
    /// <exception cref="NotSupportedException">Thrown when the map cannot target an existing instance.</exception>
    public void Map<TSource, TTarget>(TSource source, TTarget target, string name, IReadOnlyDictionary<string, object?> parameters) {
        if (string.IsNullOrWhiteSpace(name)) {
            throw new ArgumentException("Mapping name must not be null or whitespace.", nameof(name));
        }

        ValidateRuntimeParameters(parameters);

        var expression = GetRequiredRuntimeMap<TSource, TTarget>(name, parameters);

        if (expression.Body is not MemberInitExpression) {
            throw new NotSupportedException($"Mapping from TSource ({typeof(TSource).FullName}) to TTarget ({typeof(TTarget).FullName}) cannot map to an existing target instance because the map does not use an object initializer (x => new TTarget {{ ... }}). Use Map(source, name, parameters) instead.");
        }

        var compiled = CompileMapper(expression);
        compiled.Invoke(source, target);
    }

    /// <summary>
    /// Maps values from the source object to an existing target object using the default map.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TTarget">The target type.</typeparam>
    /// <param name="source">The source object.</param>
    /// <param name="target">The target object to update.</param>
    /// <exception cref="ArgumentException">Thrown when no map is available.</exception>
    /// <exception cref="NotSupportedException">Thrown when the map cannot target an existing instance.</exception>
    public void Map<TSource, TTarget>(TSource source, TTarget target) {
        var key = new MapKey(typeof(TSource), typeof(TTarget), null);
        if (_compiledMapToExistingCache.TryGetValue(key, out var map)) {
            ((Action<TSource, TTarget>)map).Invoke(source, target);
            return;
        }

        var expression = GetRequiredRuntimeMap<TSource, TTarget>(null, _emptyParameters);

        if (expression.Body is not MemberInitExpression) {
            throw new NotSupportedException($"Mapping from TSource ({typeof(TSource).FullName}) to TTarget ({typeof(TTarget).FullName}) cannot map to an existing target instance because the map does not use an object initializer (x => new TTarget {{ ... }}). Use Map(source) instead.");
        }

        var compiled = CompileMapper(expression);
        _compiledMapToExistingCache[key] = compiled;
        compiled.Invoke(source, target);
    }

    /// <summary>
    /// Maps values from the source object to an existing target object using the default map and runtime parameters.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TTarget">The target type.</typeparam>
    /// <param name="source">The source object.</param>
    /// <param name="target">The target object to update.</param>
    /// <param name="parameters">Runtime parameters used by <see cref="MapifyProfile"/> <c>Parameter&lt;T&gt;(name)</c> markers.</param>
    /// <exception cref="ArgumentException">Thrown when no map is available.</exception>
    /// <exception cref="NotSupportedException">Thrown when the map cannot target an existing instance.</exception>
    public void Map<TSource, TTarget>(TSource source, TTarget target, IReadOnlyDictionary<string, object?> parameters) {
        ValidateRuntimeParameters(parameters);

        var expression = GetRequiredRuntimeMap<TSource, TTarget>(null, parameters);

        if (expression.Body is not MemberInitExpression) {
            throw new NotSupportedException($"Mapping from TSource ({typeof(TSource).FullName}) to TTarget ({typeof(TTarget).FullName}) cannot map to an existing target instance because the map does not use an object initializer (x => new TTarget {{ ... }}). Use Map(source, parameters) instead.");
        }

        var compiled = CompileMapper(expression);
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

        var expression = GetRequiredRuntimeMap<TSource, TTarget>(name, _emptyParameters);
        var compiled = expression.Compile();
        _compiledMapToNewCache[key] = compiled;
        return compiled.Invoke(source);
    }

    /// <summary>
    /// Maps the source object to a new target object using a named map and runtime parameters.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TTarget">The target type.</typeparam>
    /// <param name="source">The source object.</param>
    /// <param name="name">The map name.</param>
    /// <param name="parameters">Runtime parameters used by <see cref="MapifyProfile"/> <c>Parameter&lt;T&gt;(name)</c> markers.</param>
    /// <returns>A new mapped target object.</returns>
    /// <exception cref="ArgumentException">Thrown when the name is invalid or the named map is missing.</exception>
    public TTarget Map<TSource, TTarget>(TSource source, string name, IReadOnlyDictionary<string, object?> parameters) {
        if (string.IsNullOrWhiteSpace(name)) {
            throw new ArgumentException("Mapping name must not be null or whitespace.", nameof(name));
        }

        ValidateRuntimeParameters(parameters);

        var expression = GetRequiredRuntimeMap<TSource, TTarget>(name, parameters);
        var compiled = expression.Compile();
        return compiled.Invoke(source);
    }

    /// <summary>
    /// Maps the source object to a new target object using the default map.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TTarget">The target type.</typeparam>
    /// <param name="source">The source object.</param>
    /// <returns>A new mapped target object.</returns>
    /// <exception cref="ArgumentException">Thrown when no map is available.</exception>
    public TTarget Map<TSource, TTarget>(TSource source) {
        var key = new MapKey(typeof(TSource), typeof(TTarget), null);
        if (_compiledMapToNewCache.TryGetValue(key, out var map)) {
            return ((Func<TSource, TTarget>)map).Invoke(source);
        }

        var expression = GetRequiredRuntimeMap<TSource, TTarget>(null, _emptyParameters);
        var compiled = expression.Compile();
        _compiledMapToNewCache[key] = compiled;
        return compiled.Invoke(source);
    }

    /// <summary>
    /// Maps the source object to a new target object using the default map and runtime parameters.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TTarget">The target type.</typeparam>
    /// <param name="source">The source object.</param>
    /// <param name="parameters">Runtime parameters used by <see cref="MapifyProfile"/> <c>Parameter&lt;T&gt;(name)</c> markers.</param>
    /// <returns>A new mapped target object.</returns>
    /// <exception cref="ArgumentException">Thrown when no map is available.</exception>
    public TTarget Map<TSource, TTarget>(TSource source, IReadOnlyDictionary<string, object?> parameters) {
        ValidateRuntimeParameters(parameters);

        var expression = GetRequiredRuntimeMap<TSource, TTarget>(null, parameters);
        var compiled = expression.Compile();
        return compiled.Invoke(source);
    }

    private Expression<Func<TSource, TTarget>> GetRequiredRuntimeMap<TSource, TTarget>(
        string? name,
        IReadOnlyDictionary<string, object?> parameters
    ) {
        if (TryCreateRuntimeMapExpression<TSource, TTarget>(
                (sourceType, targetType, requestedMapName) => ResolveRuntimeMapCandidate(sourceType, targetType, requestedMapName ?? name, parameters),
                name,
                out var runtimeMapExpression)) {
            return runtimeMapExpression;
        }

        if (name == null && _useDefaultMapIfTypeMapIsMissing) {
            var defaultCacheKey = new Tuple<Type, Type>(typeof(TSource), typeof(TTarget));
            if (_defaultMapCache.TryGetValue(defaultCacheKey, out var existingDefaultMap)) {
                return ApplyParameters((Expression<Func<TSource, TTarget>>)existingDefaultMap, parameters);
            }

            var defaultMap = CreateMap<TSource, TTarget>(null, (sourceType, targetType, requestedMapName) => ResolveExistingMapForBuild(sourceType, targetType, requestedMapName));
            _defaultMapCache[defaultCacheKey] = defaultMap;
            return ApplyParameters(defaultMap, parameters);
        }

        if (name == null) {
            throw new ArgumentException($"Missing type map configuration for TSource ({typeof(TSource).FullName}) to TTarget ({typeof(TTarget).FullName})");
        }

        throw new ArgumentException($"Missing named type map configuration '{name}' for TSource ({typeof(TSource).FullName}) to TTarget ({typeof(TTarget).FullName})");
    }

    internal LambdaExpression GetRequiredRuntimeMapUntyped(
        Type sourceType,
        Type targetType,
        string? name,
        IReadOnlyDictionary<string, object?>? parameters
    ) {
        var genericMethod = typeof(Mapify)
            .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(m => m.Name == nameof(GetRequiredRuntimeMap)
                && m.IsGenericMethodDefinition
                && m.GetGenericArguments().Length == 2
                && m.GetParameters().Length == 2)
            .MakeGenericMethod(sourceType, targetType);

        var runtimeParameters = parameters ?? _emptyParameters;
        try {
            return (LambdaExpression)genericMethod.Invoke(this, [name, runtimeParameters])!;
        } catch (TargetInvocationException ex) when (ex.InnerException != null) {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;
        }
    }

    private LambdaExpression? ResolveRuntimeMapCandidate(
        Type sourceType,
        Type targetType,
        string? name,
        IReadOnlyDictionary<string, object?> parameters
    ) {
        var key = new MapKey(sourceType, targetType, name);
        if (_converters.TryGetValue(key, out var existingConverter)) {
            return ApplyParameters(existingConverter, parameters);
        }

        return null;
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

    private sealed class UseMapDepthMarkerVisitor : ExpressionVisitor {
        public bool HasUseMapMarkers { get; private set; }

        public bool HasDepthlessUseMapMarkers { get; private set; }

        public int MaxExplicitDepth { get; private set; }

        public static UseMapDepthMarkerVisitor Extract(LambdaExpression expression) {
            var visitor = new UseMapDepthMarkerVisitor();
            visitor.Visit(expression.Body);
            return visitor;
        }

        protected override Expression VisitMethodCall(MethodCallExpression node) {
            if (IsUseMapMarker(node.Method)) {
                HasUseMapMarkers = true;

                var methodDefinition = node.Method.GetGenericMethodDefinition();
                var parameters = methodDefinition.GetParameters();

                if (parameters.Length == 1
                    || (parameters.Length == 2 && parameters[0].ParameterType == typeof(string))) {
                    HasDepthlessUseMapMarkers = true;
                }

                if (parameters.Length == 2 && parameters[1].ParameterType == typeof(int)) {
                    MaxExplicitDepth = Math.Max(MaxExplicitDepth, ExtractDepth(node.Arguments[1]));
                }

                if (parameters.Length == 3 && parameters[2].ParameterType == typeof(int)) {
                    MaxExplicitDepth = Math.Max(MaxExplicitDepth, ExtractDepth(node.Arguments[2]));
                }
            }

            return base.VisitMethodCall(node);
        }

        private static int ExtractDepth(Expression expression) {
            if (expression is ConstantExpression constant && constant.Value is int depth && depth > 0) {
                return depth;
            }

            throw new InvalidOperationException("UseMap depth argument must be a constant positive integer.");
        }

        private static bool IsUseMapMarker(MethodInfo method) {
            if (!method.IsGenericMethod || method.DeclaringType != typeof(MapifyProfile)) {
                return false;
            }

            var genericDefinition = method.GetGenericMethodDefinition();
            return string.Equals(genericDefinition.Name, "UseMap", StringComparison.Ordinal)
                   && genericDefinition.GetGenericArguments().Length == 2;
        }
    }
}
