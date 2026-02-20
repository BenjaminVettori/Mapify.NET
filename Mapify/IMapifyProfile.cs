using System.Linq.Expressions;

namespace Mapify.NET {
    public interface IMapifyProfile {
    }

    internal interface IMapifyConfigurator {
        Expression<Func<TSource, TTarget>> CreateMap<TSource, TTarget>(Expression<Func<TSource, TTarget>>? partial = null);
    }

    public abstract class MapifyProfile : IMapifyProfile {
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

        protected Expression<Func<TSource, TTarget>> CreateMap<TSource, TTarget>(Expression<Func<TSource, TTarget>>? partial = null) {
            if (_configurator == null) {
                throw new InvalidOperationException("CreateMap can only be called while configuring a profile.");
            }

            return _configurator.CreateMap(partial);
        }
    }
}
