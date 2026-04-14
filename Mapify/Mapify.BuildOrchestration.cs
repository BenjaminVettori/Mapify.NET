using System.Linq.Expressions;

namespace Mapify.NET;

public partial class Mapify {
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
                    created = CreateMapFromPending(key.SourceType, key.TargetType, key.Name, pending);
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

    private LambdaExpression CreateMapFromPending(Type sourceType, Type targetType, string? mapName, PendingMapRegistration pending) {
        var method = typeof(Mapify).GetMethod(nameof(CreateMapFromPendingGeneric), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        var generic = method.MakeGenericMethod(sourceType, targetType);
        return (LambdaExpression)generic.Invoke(this, [mapName, pending])!;
    }

    private Expression<Func<TSource, TTarget>> CreateMapFromPendingGeneric<TSource, TTarget>(string? mapName, PendingMapRegistration pending)
        => CreateMap((Expression<Func<TSource, TTarget>>?)pending.Partial, pending.Bindings, (sourceType, targetType, requestedMapName) => ResolveExistingMapForBuild(sourceType, targetType, requestedMapName ?? mapName));

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
}
