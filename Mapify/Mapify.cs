using System.Linq.Expressions;

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

    MapifyMapBuilder<TSource, TTarget> IMapifyConfigurator.CreateMap<TSource, TTarget>(Expression<Func<TSource, TTarget>>? partial) {
        return AddPendingMap(null, partial);
    }

    MapifyMapBuilder<TSource, TTarget> IMapifyConfigurator.CreateMap<TSource, TTarget>(string name, Expression<Func<TSource, TTarget>>? partial) {
        if (string.IsNullOrWhiteSpace(name)) {
            throw new ArgumentException("Mapping name must not be null or whitespace.", nameof(name));
        }

        return AddPendingMap(name, partial);
    }

    private MapifyMapBuilder<TSource, TTarget> AddPendingMap<TSource, TTarget>(string? name, Expression<Func<TSource, TTarget>>? partial) {
        var key = new MapKey(typeof(TSource), typeof(TTarget), name);
        if (_pendingRegistrations.ContainsKey(key) || _converters.ContainsKey(key)) {
            var mappingScope = name == null ? "default" : $"named '{name}'";
            throw new ArgumentException($"There already exists a {mappingScope} mapping from TSource ({typeof(TSource).FullName}) to TTarget ({typeof(TTarget).FullName}). There can only be one mapping per name and source/target combination.");
        }

        var registration = new PendingMapRegistration(partial);
        _pendingRegistrations[key] = registration;

        return new MapifyMapBuilder<TSource, TTarget>((targetExpression, sourceExpression) => {
            registration.AddBinding(targetExpression, sourceExpression);
        });
    }
}

