namespace Mapify.NET;

public partial class Mapify {
    private static readonly IReadOnlyDictionary<string, object?> _emptyParameters = new Dictionary<string, object?>();

    private static void ValidateRuntimeParameters(IReadOnlyDictionary<string, object?> parameters) {
        if (parameters == null) {
            throw new ArgumentNullException(nameof(parameters));
        }

        if (parameters.Count == 0 && !ReferenceEquals(parameters, _emptyParameters)) {
            throw new ArgumentException("At least one runtime parameter must be provided when using a parameterized overload.", nameof(parameters));
        }
    }
}
