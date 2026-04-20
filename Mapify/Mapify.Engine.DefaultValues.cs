using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace Mapify.NET;

public partial class Mapify {
    private static readonly ConcurrentDictionary<Tuple<Type, Type, string>, bool> _initializedPropertyCache = new();

    private static bool IsRequiredMember(MemberInfo member)
        => member.CustomAttributes.Any(x => string.Equals(x.AttributeType.FullName, "System.Runtime.CompilerServices.RequiredMemberAttribute", StringComparison.Ordinal));

    private static Expression CreatePropertyDefaultValueExpression(PropertyInfo property) {
        if (IsCollectionLikeType(property.PropertyType)
            && ShouldUseEmptyCollectionFallback(property)
            && TryCreateEmptyCollectionExpression(property.PropertyType, out var emptyCollectionExpression)) {
            return emptyCollectionExpression;
        }

        return CreateDefaultValueExpression(property.PropertyType);
    }

    private static bool ShouldUseEmptyCollectionFallback(PropertyInfo property)
        => IsRequiredMember(property) || !IsPropertyDeclaredNullable(property);

    private static bool IsPropertyDeclaredNullable(PropertyInfo property) {
        if (property.PropertyType.IsValueType) {
            return Nullable.GetUnderlyingType(property.PropertyType) != null;
        }

        var nullabilityContextType = Type.GetType("System.Reflection.NullabilityInfoContext");
        if (nullabilityContextType != null) {
            try {
                var nullabilityContext = Activator.CreateInstance(nullabilityContextType);
                var createMethod = nullabilityContextType.GetMethod("Create", [typeof(PropertyInfo)]);
                var nullabilityInfo = createMethod?.Invoke(nullabilityContext, [property]);
                var writeState = nullabilityInfo?.GetType().GetProperty("WriteState")?.GetValue(nullabilityInfo);
                if (writeState != null) {
                    var stateName = writeState.ToString();
                    if (string.Equals(stateName, "Nullable", StringComparison.Ordinal)) {
                        return true;
                    }

                    if (string.Equals(stateName, "NotNull", StringComparison.Ordinal)) {
                        return false;
                    }
                }
            } catch {
            }
        }

        var propertyNullableFlag = TryGetNullableAttributeFlag(property.CustomAttributes);
        if (propertyNullableFlag.HasValue) {
            return propertyNullableFlag.Value == 2;
        }

        var contextNullableFlag = TryGetNullableContextFlag(property);
        if (contextNullableFlag.HasValue) {
            return contextNullableFlag.Value == 2;
        }

        return true;
    }

    private static byte? TryGetNullableAttributeFlag(IEnumerable<CustomAttributeData> attributes) {
        foreach (var attribute in attributes) {
            if (!string.Equals(attribute.AttributeType.FullName, "System.Runtime.CompilerServices.NullableAttribute", StringComparison.Ordinal)) {
                continue;
            }

            if (attribute.ConstructorArguments.Count == 1) {
                var argument = attribute.ConstructorArguments[0];
                if (argument.ArgumentType == typeof(byte)) {
                    return (byte)argument.Value!;
                }

                if (argument.ArgumentType == typeof(byte[])
                    && argument.Value is IReadOnlyCollection<CustomAttributeTypedArgument> values
                    && values.Count > 0) {
                    return (byte)values.First().Value!;
                }
            }
        }

        return null;
    }

    private static byte? TryGetNullableContextFlag(PropertyInfo property) {
        for (Type? currentType = property.DeclaringType; currentType != null; currentType = currentType.DeclaringType) {
            var contextFlag = TryGetNullableContextFlag(currentType.CustomAttributes);
            if (contextFlag.HasValue) {
                return contextFlag;
            }
        }

        return null;
    }

    private static byte? TryGetNullableContextFlag(IEnumerable<CustomAttributeData> attributes) {
        foreach (var attribute in attributes) {
            if (!string.Equals(attribute.AttributeType.FullName, "System.Runtime.CompilerServices.NullableContextAttribute", StringComparison.Ordinal)) {
                continue;
            }

            if (attribute.ConstructorArguments.Count == 1
                && attribute.ConstructorArguments[0].ArgumentType == typeof(byte)) {
                return (byte)attribute.ConstructorArguments[0].Value!;
            }
        }

        return null;
    }

    private static bool TryCreateEmptyCollectionExpression(Type type, out Expression expression) {
        expression = null!;

        if (!IsCollectionLikeType(type) || !TryGetEnumerableElementType(type, out var elementType)) {
            return false;
        }

        if (type.IsArray) {
            expression = Expression.NewArrayInit(elementType);
            return true;
        }

        if (type.IsInterface && type.IsGenericType) {
            var genericDefinition = type.GetGenericTypeDefinition();
            if (genericDefinition == typeof(IEnumerable<>)
                || genericDefinition == typeof(ICollection<>)
                || genericDefinition == typeof(IList<>)
                || genericDefinition == typeof(IReadOnlyCollection<>)
                || genericDefinition == typeof(IReadOnlyList<>)) {
                expression = Expression.New(typeof(List<>).MakeGenericType(elementType));
                return true;
            }
        }

        var parameterlessConstructor = type.GetConstructor(Type.EmptyTypes);
        if (parameterlessConstructor != null) {
            expression = Expression.New(parameterlessConstructor);
            return true;
        }

        var enumerableConstructor = type.GetConstructor([typeof(IEnumerable<>).MakeGenericType(elementType)]);
        if (enumerableConstructor != null) {
            var emptyEnumerable = Expression.Call(
                typeof(Enumerable),
                nameof(Enumerable.Empty),
                [elementType]
            );
            expression = Expression.New(enumerableConstructor, emptyEnumerable);
            return true;
        }

        return false;
    }

    private static bool IsPropertyInitializedOnFreshInstance(PropertyInfo property, Type destinationType) {
        if (!property.CanRead || property.DeclaringType == null) {
            return false;
        }

        var cacheKey = Tuple.Create(destinationType, property.DeclaringType, property.Name);
        return _initializedPropertyCache.GetOrAdd(cacheKey, _ => {
            try {
                var instance = Activator.CreateInstance(destinationType);
                if (instance == null) {
                    return false;
                }

                var value = property.GetValue(instance);
                return !IsDefaultValue(value, property.PropertyType);
            } catch {
                return false;
            }
        });
    }

    private static bool IsDefaultValue(object? value, Type type) {
        if (value == null) {
            return true;
        }

        if (!type.IsValueType) {
            return false;
        }

        var defaultValue = Activator.CreateInstance(type);
        return Equals(value, defaultValue);
    }

    private static Expression CreateDefaultValueExpression(Type type)
        => CanBeNull(type)
            ? Expression.Constant(null, type)
            : Expression.Default(type);
}
