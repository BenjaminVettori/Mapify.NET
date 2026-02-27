using System.Linq.Expressions;

namespace Mapify.NET; 
internal interface IMapifyConfigurator {
    void CreateMap<TSource, TTarget>(Expression<Func<TSource, TTarget>>? partial = null);

    void CreateMap<TSource, TTarget>(string name, Expression<Func<TSource, TTarget>>? partial = null);
}

public abstract class MapifyProfile {
    private IMapifyConfigurator? _configurator;

    internal void Apply(IMapifyConfigurator configurator) {
        _configurator = configurator;
        try {
            Configure();
        } finally {
            _configurator = null;
        }
    }

    protected abstract void Configure();

    protected void CreateMap<TSource, TTarget>(Expression<Func<TSource, TTarget>>? partial = null) {
        if (_configurator == null) {
            throw new InvalidOperationException("CreateMap can only be called while configuring a profile.");
        }

        _configurator.CreateMap(partial);
    }

    protected void CreateMap<TSource, TTarget>(string name, Expression<Func<TSource, TTarget>>? partial = null) {
        if (_configurator == null) {
            throw new InvalidOperationException("CreateMap can only be called while configuring a profile.");
        }

        if (string.IsNullOrWhiteSpace(name)) {
            throw new ArgumentException("Mapping name must not be null or whitespace.", nameof(name));
        }

        _configurator.CreateMap(name, partial);
    }

    protected static TTarget UseMap<TSource, TTarget>(TSource source) {
        throw new InvalidOperationException($"{nameof(UseMap)} can only be used as a marker inside a mapping expression during profile configuration.");
    }

    protected static TTarget UseMap<TSource, TTarget>(string name, TSource source) {
        throw new InvalidOperationException($"{nameof(UseMap)} can only be used as a marker inside a mapping expression during profile configuration.");
    }
}
