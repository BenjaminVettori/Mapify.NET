using System.Linq.Expressions;
using System.Reflection;

namespace Mapify.NET {
    public static class Mapper {

        private static bool GlobalUseDefaultMapIfTypeMapIsMissing = false;

        /// <summary>
        /// Configures whether a default map should be used if no type map was added with <see cref="AddMap{TSource, TTarget}(Expression{Func{TSource, TTarget}})"/>.
        /// Be default, this is set to false, meaning that an exception will be thrown if no type map is found.
        /// </summary>
        public static void UseDefaultMapIfTypeMapIsMissing(bool value) {
            GlobalUseDefaultMapIfTypeMapIsMissing = value;
        }

        /// <summary>
        /// Clears all mappings and compiled delegates.
        /// </summary>
        public static void ClearMappings() {
            Converters.Clear();
            DefaultMapCache.Clear();
            CompiledMapToExistingCache.Clear();
            CompiledMapToNewCache.Clear();
            CompiledSpecificMapToExistingCache.Clear();
            CompiledSpecificMapToNewCache.Clear();
        }

        /// <summary>
        /// Stores converters added with <see cref="AddMap{TSource, TTarget}(Expression{Func{TSource, TTarget}})"/> which are then returned by <see cref="GetMap{TSource, TTarget}(bool)"/>
        /// </summary>
        private static IDictionary<Tuple<Type, Type>, LambdaExpression> Converters = new Dictionary<Tuple<Type, Type>, LambdaExpression>();

        /// <summary>
        /// Caches default mappings such that they do not have to be created multiple times when calling GetMap(useDefaultMapIfTypeMapIsMissing: true).
        /// </summary>
        private static IDictionary<Tuple<Type, Type>, LambdaExpression> DefaultMapCache = new Dictionary<Tuple<Type, Type>, LambdaExpression>();

        /// <summary>
        /// Caches compiled mapping expressions which map to existing objects
        /// </summary>
        private static IDictionary<Tuple<Type, Type>, Delegate> CompiledMapToExistingCache = new Dictionary<Tuple<Type, Type>, Delegate>();

        /// <summary>
        /// Caches compiled mapping expressions which create new target objects
        /// </summary>
        private static IDictionary<Tuple<Type, Type>, Delegate> CompiledMapToNewCache = new Dictionary<Tuple<Type, Type>, Delegate>();

        /// <summary>
        /// Caches compiled mapping expressions which map to existing objects
        /// </summary>
        private static IDictionary<Expression, Delegate> CompiledSpecificMapToExistingCache = new Dictionary<Expression, Delegate>();

        /// <summary>
        /// Caches compiled mapping expressions which create new target objects
        /// </summary>
        private static IDictionary<Expression, Delegate> CompiledSpecificMapToNewCache = new Dictionary<Expression, Delegate>();

        /// <summary>
        /// Creates a new mapping with the optional partial mapping expression and adds it to the mapping dictionary
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TTarget"></typeparam>
        /// <param name="partial">A lambda expression where the body is a initializer (x => new TTarget { ... })</param>
        /// <returns>The created mapping expression.</returns>
        public static Expression<Func<TSource, TTarget>> CreateAndAddMap<TSource, TTarget>(Expression<Func<TSource, TTarget>>? partial = null) {
            var map = CreateMap(partial);
            AddMap(map);
            return map;
        }

        /// <summary>
        /// Creates and adds a new mapping expression
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TTarget"></typeparam>
        /// <param name="mappingExpression">A full mapping expression (e.g. created with Mapper.CreateMap),where the body is a initializer (x => new TTarget { ... }).</param>
        /// <exception cref="ArgumentException"></exception>
        public static void AddMap<TSource, TTarget>(Expression<Func<TSource, TTarget>> mappingExpression) {
            var key = new Tuple<Type, Type>(typeof(TSource), typeof(TTarget));
            if (Converters.ContainsKey(key)) {
                throw new ArgumentException($"There already exists a mapping from TSource ({typeof(TSource).FullName}) to TTarget ({typeof(TTarget).FullName}). There can only be one mapping for each combination of TSource and TTarget.");
            }
            Converters[key] = mappingExpression;
            CompiledMapToNewCache[key] = mappingExpression.Compile();

            // Map-to-existing is only valid for member initializer expressions
            // (x => new TTarget { ... }). Value mappings (e.g. enum->enum, object->string)
            // are supported for Map(source) but not for Map(source, target).
            if (mappingExpression.Body is MemberInitExpression) {
                CompiledMapToExistingCache[key] = CompileMapper(mappingExpression);
            }
        }

        public static Expression<Func<TSource, TTarget>>? GetMap<TSource, TTarget>(bool useDefaultMapIfTypeMapIsMissing = false) {
            var key = new Tuple<Type, Type>(typeof(TSource), typeof(TTarget));
            if (Converters.TryGetValue(key, out var existingConverter)) {
                return (Expression<Func<TSource, TTarget>>)existingConverter;
            } else if ((!useDefaultMapIfTypeMapIsMissing && GlobalUseDefaultMapIfTypeMapIsMissing) || useDefaultMapIfTypeMapIsMissing) {
                if (DefaultMapCache.TryGetValue(key, out var map)) {
                    return (Expression<Func<TSource, TTarget>>)map;
                }
                var defaultMap = CreateMap<TSource, TTarget>();
                DefaultMapCache[key] = defaultMap;
                return defaultMap;
            }
            return null;
        }

        public static Expression<Func<TSource, TTarget>> GetRequiredMap<TSource, TTarget>(bool useDefaultMapIfTypeMapIsMissing = false) {
            var map = GetMap<TSource, TTarget>(useDefaultMapIfTypeMapIsMissing);
            if (map != null) {
                return map;
            }

            throw new ArgumentException($"Missing type map configuration for TSource ({typeof(TSource).FullName}) to TTarget ({typeof(TTarget).FullName})");
        }

        /// <summary>
        /// Maps the the source object of type TSource to an existing object of type TTarget.
        /// The mapping expression must contain an initializer (x => new TTarget { ... }).
        /// </summary>
        /// <typeparam name="TSource">The type to map from</typeparam>
        /// <typeparam name="TTarget">The target type to map to</typeparam>
        /// <param name="expression">An initializer expression of the form (x => new TTarget { ... })</param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="NotSupportedException"></exception>
        public static void Map<TSource, TTarget>(this Expression<Func<TSource, TTarget>> expression, TSource source, TTarget target, bool cache = false) {
            if (CompiledSpecificMapToExistingCache.TryGetValue(expression, out var map)) {
                ((Action<TSource, TTarget>)map).Invoke(source, target);
                return;
            }
            var compiled = CompileMapper(expression);
            if (cache) {
                CompiledSpecificMapToExistingCache[expression] = compiled;
            }
            compiled.Invoke(source, target);
        }

        /// <summary>
        /// Maps the the source object of type TSource to an existing object of type TTarget.
        /// </summary>
        /// <typeparam name="TSource">The type to map from</typeparam>
        /// <typeparam name="TTarget">The target type to map to</typeparam>
        /// <param name="useDefaultMapIfTypeMapIsMissing">If true, a default map will be used if none was added with <see cref="AddMap{TSource, TTarget}(Expression{Func{TSource, TTarget}})"> beforehand.</param>
        /// <returns>A new object of type <see cref="TTarget"/> with the mapped values</returns>
        public static void Map<TSource, TTarget>(TSource source, TTarget target, bool useDefaultMapIfTypeMapIsMissing = false) {
            var key = new Tuple<Type, Type>(typeof(TSource), typeof(TTarget));            
            if (CompiledMapToExistingCache.TryGetValue(key, out var map)) {
                ((Action<TSource, TTarget>)map).Invoke(source, target);
                return;
            }
            var expression = GetRequiredMap<TSource, TTarget>(useDefaultMapIfTypeMapIsMissing);

            if (expression.Body is not MemberInitExpression) {
                throw new NotSupportedException($"Mapping from TSource ({typeof(TSource).FullName}) to TTarget ({typeof(TTarget).FullName}) cannot map to an existing target instance because the map does not use an object initializer (x => new TTarget {{ ... }}). Use Map(source) instead.");
            }

            var compiled = CompileMapper(expression);
            CompiledMapToExistingCache[key] = compiled;
            compiled.Invoke(source, target);
        }

        /// <summary>
        /// Maps the the source object of type TSource to a new object of type TTarget.
        /// The mapping expression must contain an initializer (x => new TTarget { ... }).
        /// </summary>
        /// <typeparam name="TSource">The type to map from</typeparam>
        /// <typeparam name="TTarget">The target type to map to</typeparam>
        /// <param name="expression">An initializer expression of the form (x => new TTarget { ... })</param>
        /// <returns>A new object of type <see cref="TTarget"/> with the mapped values</returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="NotSupportedException"></exception>
        public static TTarget Map<TSource, TTarget>(this Expression<Func<TSource, TTarget>> expression, TSource source, bool cache = false) {
            if (CompiledSpecificMapToNewCache.TryGetValue(expression, out var map)) {
                return ((Func<TSource, TTarget>)map).Invoke(source);
            }
            var compiled = expression.Compile();
            if (cache) {
                CompiledSpecificMapToNewCache[expression] = compiled;
            }
            return compiled.Invoke(source);
        }

        /// <summary>
        /// Maps the the source object of type TSource to a new object of type TTarget.
        /// </summary>
        /// <typeparam name="TSource">The type to map from</typeparam>
        /// <typeparam name="TTarget">The target type to map to</typeparam>
        /// <param name="source"></param>
        /// <param name="useDefaultMapIfTypeMapIsMissing">If true, a default map will be used if none was added with <see cref="AddMap{TSource, TTarget}(Expression{Func{TSource, TTarget}})"> beforehand.</param>
        /// <returns>A new object of type <see cref="TTarget"/> with the mapped values</returns>
        public static TTarget Map<TSource, TTarget>(TSource source, bool useDefaultMapIfTypeMapIsMissing = false) {
            var key = new Tuple<Type, Type>(typeof(TSource), typeof(TTarget));
            if (CompiledMapToNewCache.TryGetValue(key, out var map)) {
                return ((Func<TSource, TTarget>)map).Invoke(source);
            }

            var expression = GetRequiredMap<TSource, TTarget>(useDefaultMapIfTypeMapIsMissing);
            var compiled = expression.Compile();
            CompiledMapToNewCache[key] = compiled;
            return compiled.Invoke(source);
        }

        /// <summary>
        /// Compiles a given mapping expression to an action which maps the values to an existing object instead.
        /// The mapping expression must contain an initializer (x => new TTarget { ... }).
        /// The initializer bindings are converted to assignment expressions.
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TTarget"></typeparam>
        /// <param name="expression"></param>
        /// <returns>An action that can be called to map an object of type <see cref="TSource"/> to an existing object of type <see cref="TTarget"/></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="NotSupportedException"></exception>
        public static Action<TSource, TTarget> CompileMapper<TSource, TTarget>(Expression<Func<TSource, TTarget>> expression) {
            if (expression.Body is not MemberInitExpression initExpr)
                throw new ArgumentException("Expression must be a member initializer (new TTarget { ... })");

            var sourceParam = Expression.Parameter(typeof(TSource), "src");
            var targetParam = Expression.Parameter(typeof(TTarget), "target");
            var assignments = new List<Expression>();

            foreach (var binding in initExpr.Bindings) {
                if (binding is not MemberAssignment ma)
                    throw new NotSupportedException("Only member assignments are supported");

                // ma.Member -> property on Target
                // ma.Expression -> expression using source (x.Firstname.ToLower())
                var replaced = new ParameterReplaceVisitor(expression.Parameters[0], sourceParam).Visit(ma.Expression);
                var assign = Expression.Assign(
                    Expression.PropertyOrField(targetParam, ma.Member.Name),
                    replaced
                );

                assignments.Add(assign);
            }

            var block = Expression.Block(assignments);
            var action = Expression.Lambda<Action<TSource, TTarget>>(block, sourceParam, targetParam).Compile();

            return action;
        }

        public static Expression<Func<TSource, TDestination>> CreateMap<TSource, TDestination>(
            Expression<Func<TSource, TDestination>>? partial = null
        ) {
            var baseParam = Expression.Parameter(typeof(TSource), "x");
            var existingBindings = new Dictionary<string, MemberBinding>();
            if (partial != null) {
                // update the parameter name of the partial expression to "x"
                var partialUpdated = (MemberInitExpression)new ParameterReplaceVisitor(partial.Parameters[0], baseParam)
                    .Visit(partial.Body);

                // copy existing bindings from the partial expression
                foreach (var partialBinding in partialUpdated.Bindings.OfType<MemberAssignment>()) {
                    MemberAssignment binding = MapPartialBinding(partialBinding);
                    existingBindings[binding.Member.Name] = binding;
                }
            }

            // get all public instance properties of the destination type that can be written to
            var destinationProperties = typeof(TDestination).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite);

            // get all public instance properties of the source type that can be read from
            var sourceProperties = typeof(TSource).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead)
                .ToDictionary(p => p.Name);

            var allBindings = new List<MemberBinding>(existingBindings.Values);

            foreach (var destProp in destinationProperties) {
                // skip properties that are already bound in the partial expression
                if (existingBindings.ContainsKey(destProp.Name))
                    continue;

                var sourceProp = GetCompatibleSourceProperty(sourceProperties, destProp);

                // If there is a source property with the same name and the types are compatible
                if (sourceProp != null) {
                    MemberAssignment binding;
                    binding = GetImplicitBinding(baseParam, sourceProp, destProp);

                    allBindings.Add(binding);
                }
            }

            var body = Expression.MemberInit(Expression.New(typeof(TDestination)), allBindings);
            return Expression.Lambda<Func<TSource, TDestination>>(body, baseParam);
        }

        /// <summary>
        /// Maps a member assignment to one that is compatible with EF.
        /// Casts nullable types where necessary and replaces coalesce operators with ternary operators.
        /// </summary>
        /// <param name="partialBinding"></param>
        /// <returns></returns>
        private static MemberAssignment MapPartialBinding(MemberAssignment partialBinding) {
            var expr = partialBinding.Expression;
            // replace the coalesce operator with a conditional expression
            if (expr is BinaryExpression binaryExpr &&
                binaryExpr.NodeType == ExpressionType.Coalesce) {
                // x ?? y => x != null ? x : y
                var test = Expression.NotEqual(binaryExpr.Left, Expression.Constant(null, binaryExpr.Left.Type));
                // if the left side is a nullable type and the right side is not equal to the null constant, use the .Value property
                // to get the underlying value, otherwise use the left side directly, since null is a valid fallback for a nullable type
                var value = Nullable.GetUnderlyingType(binaryExpr.Left.Type) != null
                     && (binaryExpr.Right is not ConstantExpression rightConst || rightConst.Value != null)
                    ? Expression.Property(binaryExpr.Left, "Value")
                    : binaryExpr.Left;
                expr = Expression.Condition(test, value, binaryExpr.Right);
            }
            var binding = Expression.Bind(partialBinding.Member, expr);
            return binding;
        }

        /// <summary>
        /// The target is compatible if:
        /// * the source property exists and
        /// * the target property type is assignable from the source property type, or
        /// * the source property is nullable and the underlying type is assignable to the target type.
        /// This allows for nullable to non-nullable assignments with a default value fallback.
        /// </summary>
        /// <param name="sourceProp">The property to map from</param>
        /// <param name="destProp">The property to map to</param>
        /// <returns></returns>
        private static PropertyInfo? GetCompatibleSourceProperty(IDictionary<string, PropertyInfo> sourceTypeProperties, PropertyInfo destProp) {
            // Try to find a matching property with the same name
            if (!sourceTypeProperties.TryGetValue(destProp.Name, out var sourceProp)) {
                return null;
            }

            // check if the property type is compatible. Also allow nullable sources if target is not nullable, because the mapper will use default values in this case.
            var isCompatibleType = sourceProp != null && (
                destProp.PropertyType.IsAssignableFrom(sourceProp.PropertyType) ||

                    Nullable.GetUnderlyingType(sourceProp.PropertyType) != null &&
                    destProp.PropertyType.IsAssignableFrom(Nullable.GetUnderlyingType(sourceProp.PropertyType))

            );

            return isCompatibleType ? sourceProp : null;
        }

        /// <summary>
        /// Create binding expression from source and dest property info.
        /// PropertyName = (T?)x.PropertyName if the dest is nullable and the source is not.
        /// PropertyName = x.PropertyName != null ? x.PropertyName.Value : default(T) if the source is nullable and the destination is not.
        /// PropertyName = x.PropertyName otherwise.
        /// </summary>
        /// <param name="baseParam">Parameter expression to be used for the final initializer</param>
        /// <param name="sourceProp">The property info of the source type property</param>
        /// <param name="destProp">The matching destination porperty info of the target type property</param>
        /// <returns></returns>
        private static MemberAssignment GetImplicitBinding(ParameterExpression baseParam, PropertyInfo sourceProp, PropertyInfo destProp) {
            MemberAssignment binding;
            var sourceAccess = Expression.Property(baseParam, sourceProp);

            // check if the destination property is nullable
            var sourceNullableType = Nullable.GetUnderlyingType(sourceProp.PropertyType);
            var destNullableType = Nullable.GetUnderlyingType(destProp.PropertyType);

            if (destNullableType != null && sourceProp.PropertyType == destNullableType) {
                // if dest is nullable and source is not, cast it for type compatibility:
                // destProp = (T?)source.Prop
                var nullableType = typeof(Nullable<>).MakeGenericType(destNullableType);
                var converted = Expression.Convert(sourceAccess, nullableType);
                binding = Expression.Bind(destProp, converted);
            } else if (destNullableType == null && sourceNullableType != null && destProp.PropertyType == sourceNullableType) {
                // if source is nullable and dest is not, use the default value as fallback:
                // destProp = sourceProp != null ? sourceProp.Value : default(destProp.PropertyType)
                var notNull = Expression.NotEqual(sourceAccess, Expression.Constant(null, sourceAccess.Type));
                var value = Expression.Property(sourceAccess, "Value");
                var defaultValue = Expression.Default(destProp.PropertyType);
                var conditional = Expression.Condition(notNull, value, defaultValue);
                binding = Expression.Bind(destProp, conditional);
            } else {
                // Both types are either nullable or non-nullable and target is assignable from source:
                // destProp = source.Prop
                binding = Expression.Bind(destProp, sourceAccess);
            }

            return binding;
        }
    }
}
