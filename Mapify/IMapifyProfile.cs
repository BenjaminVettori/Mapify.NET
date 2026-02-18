using System.Linq.Expressions;

namespace Mapify.NET {
    public interface IMapifyProfile {
    }

    internal interface IMapifyConfigurator {
        void CreateAndAddMap<TSource, TTarget>(Expression<Func<TSource, TTarget>>? partial = null);

        void AddMap<TSource, TTarget>(Expression<Func<TSource, TTarget>> mappingExpression);
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

        protected void CreateMap<TSource, TTarget>(Expression<Func<TSource, TTarget>>? partial = null) {
            if (_configurator == null) {
                throw new InvalidOperationException("CreateMap can only be called while configuring a profile.");
            }

            _configurator.CreateAndAddMap(partial);
        }

        protected void AddMap<TSource, TTarget>(Expression<Func<TSource, TTarget>> mappingExpression) {
            if (_configurator == null) {
                throw new InvalidOperationException("AddMap can only be called while configuring a profile.");
            }

            _configurator.AddMap(mappingExpression);
        }
    }
}
