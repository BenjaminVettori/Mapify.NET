namespace Mapify.NET {
    public interface IMapify {
        void UseDefaultMapIfTypeMapIsMissing(bool value);

        void Map<TSource, TTarget>(TSource source, TTarget target, bool useDefaultMapIfTypeMapIsMissing = false);

        TTarget Map<TSource, TTarget>(TSource source, bool useDefaultMapIfTypeMapIsMissing = false);
    }
}
