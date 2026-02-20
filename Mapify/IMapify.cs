using System.Linq.Expressions;

namespace Mapify.NET {
    public interface IMapify {
        void UseDefaultMapIfTypeMapIsMissing(bool value);

        public Expression<Func<TSource, TTarget>> GetMap<TSource, TTarget>(bool useDefaultMapIfTypeMapIsMissing = false);

        void Map<TSource, TTarget>(TSource source, TTarget target, bool useDefaultMapIfTypeMapIsMissing = false);

        TTarget Map<TSource, TTarget>(TSource source, bool useDefaultMapIfTypeMapIsMissing = false);
    }
}
