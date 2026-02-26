using System.Linq.Expressions;
using System.Reflection;

namespace Mapify.NET {
    public static class Mapper {

        private const string UseMapMarkerName = "UseMap";

        private const string ProjectToMarkerName = "ProjectTo";

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

        /// <summary>
        /// Gets the registered map expression for the source and target types.
        /// Returns <c>null</c> when no map exists and default-map fallback is disabled.
        /// </summary>
        /// <typeparam name="TSource">The source type.</typeparam>
        /// <typeparam name="TTarget">The target type.</typeparam>
        /// <param name="useDefaultMapIfTypeMapIsMissing">Whether to allow automatic default-map creation for this call.</param>
        /// <returns>The mapping expression, or <c>null</c> if not found and fallback is disabled.</returns>
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

        /// <summary>
        /// Gets the registered map expression for the source and target types, throwing if none is available.
        /// </summary>
        /// <typeparam name="TSource">The source type.</typeparam>
        /// <typeparam name="TTarget">The target type.</typeparam>
        /// <param name="useDefaultMapIfTypeMapIsMissing">Whether to allow automatic default-map creation for this call.</param>
        /// <returns>The required mapping expression.</returns>
        /// <exception cref="ArgumentException">Thrown when no map is available.</exception>
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

        /// <summary>
        /// Creates a mapping expression for the source and destination types.
        /// </summary>
        /// <typeparam name="TSource">The source type.</typeparam>
        /// <typeparam name="TDestination">The destination type.</typeparam>
        /// <param name="partial">Optional partial initializer that overrides selected destination bindings.</param>
        /// <returns>A full mapping expression that can be compiled or registered.</returns>
        public static Expression<Func<TSource, TDestination>> CreateMap<TSource, TDestination>(
            Expression<Func<TSource, TDestination>>? partial = null
        ) {
            return CreateMap(partial, TryGetRegisteredMap);
        }

        internal static Expression<Func<TSource, TDestination>> CreateMap<TSource, TDestination>(
            Expression<Func<TSource, TDestination>>? partial,
            Func<Type, Type, string?, LambdaExpression?>? existingMapResolver
        ) {
            var baseParam = Expression.Parameter(typeof(TSource), "x");

            // get all public instance properties of the source type that can be read from
            var sourceProperties = typeof(TSource).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead)
                .ToDictionary(p => p.Name);

            var existingBindings = new Dictionary<string, MemberBinding>();
            if (partial != null) {
                // update the parameter name of the partial expression to "x"
                var partialUpdated = (MemberInitExpression)new ParameterReplaceVisitor(partial.Parameters[0], baseParam)
                    .Visit(partial.Body);

                // copy existing bindings from the partial expression
                foreach (var partialBinding in partialUpdated.Bindings.OfType<MemberAssignment>()) {
                    MemberAssignment binding = MapPartialBinding(partialBinding, existingMapResolver);
                    existingBindings[binding.Member.Name] = binding;
                }
            }

            // get all public instance properties of the destination type that can be written to
            var destinationProperties = typeof(TDestination).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite);

            var allBindings = new List<MemberBinding>(existingBindings.Values);

            foreach (var destProp in destinationProperties) {
                // skip properties that are already bound in the partial expression
                if (existingBindings.ContainsKey(destProp.Name))
                    continue;

                var sourceProp = GetSourceProperty(sourceProperties, destProp);

                // If there is a source property with the same name, prefer an existing map for sourceType -> targetType.
                // If no map exists, fallback to default implicit assignment when types are compatible.
                if (sourceProp != null) {
                    if (TryGetBindingFromExistingMap(baseParam, sourceProp, destProp, existingMapResolver, out var mappedBinding)) {
                        // If a map for sourceProp.Type -> destProp.Type exists, prefer it over direct assignment
                        allBindings.Add(mappedBinding);
                    } else if (TryGetImplicitBinding(baseParam, sourceProp, destProp, out var implicitBinding)) {
                        allBindings.Add(implicitBinding);
                    }
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
        private static MemberAssignment MapPartialBinding(
            MemberAssignment partialBinding,
            Func<Type, Type, string?, LambdaExpression?>? existingMapResolver
        ) {
            var expr = partialBinding.Expression;

            if (TryResolveUseMapMarker(partialBinding, existingMapResolver, out var mappedBinding)) {
                return mappedBinding;
            }

            if (existingMapResolver != null) {
                expr = new UseMapMarkerReplaceVisitor(existingMapResolver).Visit(expr)!;
            }

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

        private sealed class UseMapMarkerReplaceVisitor(
            Func<Type, Type, string?, LambdaExpression?> existingMapResolver
        ) : ExpressionVisitor {
            protected override Expression VisitMethodCall(MethodCallExpression node) {
                if (IsUseMapMarker(node.Method)) {
                    if (!TryResolveUseMapCall(node, existingMapResolver, out var replacement)) {
                        var genericArgs = node.Method.GetGenericArguments();
                        throw new InvalidOperationException($"No mapping found for {genericArgs[0].FullName} -> {genericArgs[1].FullName} required by {UseMapMarkerName}.");
                    }

                    return replacement;
                }

                if (IsProjectToMarker(node.Method)) {
                    if (!TryResolveProjectToCall(node, existingMapResolver, out var replacement)) {
                        throw new InvalidOperationException($"No mapping found for nested {ProjectToMarkerName} call from '{node.Arguments[0].Type.FullName}' to '{node.Type.FullName}'.");
                    }

                    return replacement;
                }

                return base.VisitMethodCall(node);
            }
        }

        private static bool TryResolveProjectToCall(
            MethodCallExpression methodCall,
            Func<Type, Type, string?, LambdaExpression?> existingMapResolver,
            out Expression resolvedExpression
        ) {
            resolvedExpression = null!;

            string? markerMapName = null;
            Expression sourceAccess;

            if (methodCall.Arguments.Count == 1) {
                sourceAccess = methodCall.Arguments[0];
            } else if (methodCall.Arguments.Count == 2) {
                if (methodCall.Arguments[1].Type == typeof(bool)) {
                    sourceAccess = methodCall.Arguments[0];
                } else {
                    if (methodCall.Arguments[1] is not ConstantExpression nameConstant || nameConstant.Value is not string mapName || string.IsNullOrWhiteSpace(mapName)) {
                        throw new InvalidOperationException($"{ProjectToMarkerName} name argument must be a non-empty constant string.");
                    }

                    markerMapName = mapName;
                    sourceAccess = methodCall.Arguments[0];
                }
            } else {
                return false;
            }

            if (!TryBuildMappedExpression(sourceAccess, sourceAccess.Type, methodCall.Type, existingMapResolver, markerMapName, out var mappedBody, out var sourceNullCheck)) {
                return false;
            }

            if (sourceNullCheck != null) {
                mappedBody = Expression.Condition(
                    sourceNullCheck,
                    mappedBody,
                    CreateDefaultValueExpression(methodCall.Type)
                );
            }

            resolvedExpression = mappedBody;
            return true;
        }

        private static bool TryResolveUseMapCall(
            MethodCallExpression methodCall,
            Func<Type, Type, string?, LambdaExpression?> existingMapResolver,
            out Expression resolvedExpression
        ) {
            resolvedExpression = null!;

            var genericArgs = methodCall.Method.GetGenericArguments();
            var markerSourceType = genericArgs[0];
            var markerTargetType = genericArgs[1];

            string? markerMapName = null;
            Expression sourceAccess;

            if (methodCall.Arguments.Count == 1) {
                sourceAccess = methodCall.Arguments[0];
            } else if (methodCall.Arguments.Count == 2) {
                if (methodCall.Arguments[0] is not ConstantExpression nameConstant || nameConstant.Value is not string mapName || string.IsNullOrWhiteSpace(mapName)) {
                    throw new InvalidOperationException($"{UseMapMarkerName} name argument must be a non-empty constant string.");
                }

                markerMapName = mapName;
                sourceAccess = methodCall.Arguments[1];
            } else {
                throw new InvalidOperationException($"{UseMapMarkerName} requires an explicit source argument. Use {UseMapMarkerName}<TSource, TTarget>(x.Property). For same-name properties you can omit {UseMapMarkerName} and rely on implicit nested map resolution.");
            }

            if (!TryBuildMappedExpression(sourceAccess, markerSourceType, markerTargetType, existingMapResolver, markerMapName, out var mappedBody, out var sourceNullCheck)) {
                return false;
            }

            if (!TryAdaptMappedResult(mappedBody, markerTargetType, out var adaptedResult)) {
                throw new InvalidOperationException($"{UseMapMarkerName} target type '{markerTargetType.FullName}' is not compatible with resolved map output type '{mappedBody.Type.FullName}'.");
            }

            if (sourceNullCheck != null) {
                adaptedResult = Expression.Condition(
                    sourceNullCheck,
                    adaptedResult,
                    CreateDefaultValueExpression(markerTargetType)
                );
            }

            resolvedExpression = adaptedResult;
            return true;
        }

        private static bool TryResolveUseMapMarker(
            MemberAssignment partialBinding,
            Func<Type, Type, string?, LambdaExpression?>? existingMapResolver,
            out MemberAssignment mappedBinding
        ) {
            mappedBinding = null!;

            var markerCandidate = UnwrapConvert(partialBinding.Expression);
            if (markerCandidate is not MethodCallExpression methodCall) {
                return false;
            }

            if (!IsUseMapMarker(methodCall.Method)) {
                return false;
            }

            if (partialBinding.Member is not PropertyInfo destProp) {
                throw new InvalidOperationException($"{UseMapMarkerName} marker can only be used for property bindings.");
            }

            if (existingMapResolver == null) {
                throw new InvalidOperationException($"{UseMapMarkerName} marker requires a map resolver.");
            }

            var genericArgs = methodCall.Method.GetGenericArguments();
            var markerSourceType = genericArgs[0];
            var markerTargetType = genericArgs[1];

            string? markerMapName = null;
            Expression sourceAccess;

            if (methodCall.Arguments.Count == 1) {
                sourceAccess = methodCall.Arguments[0];
            } else if (methodCall.Arguments.Count == 2) {
                if (methodCall.Arguments[0] is not ConstantExpression nameConstant || nameConstant.Value is not string mapName || string.IsNullOrWhiteSpace(mapName)) {
                    throw new InvalidOperationException($"{UseMapMarkerName} name argument must be a non-empty constant string.");
                }

                markerMapName = mapName;
                sourceAccess = methodCall.Arguments[1];
            } else {
                throw new InvalidOperationException($"{UseMapMarkerName} requires an explicit source argument. Use {UseMapMarkerName}<TSource, TTarget>(x.Property). For same-name properties you can omit {UseMapMarkerName} and rely on implicit nested map resolution.");
            }

            if (!TryBuildMappedExpression(sourceAccess, markerSourceType, markerTargetType, existingMapResolver, markerMapName, out var mappedBody, out var sourceNullCheck)) {
                throw new InvalidOperationException($"No mapping found for {markerSourceType.FullName} -> {markerTargetType.FullName} required by {UseMapMarkerName} on property '{destProp.Name}'.");
            }

            if (!TryAdaptMappedResult(mappedBody, destProp.PropertyType, out var adaptedResult)) {
                throw new InvalidOperationException($"{UseMapMarkerName} target type '{destProp.PropertyType.FullName}' is not compatible with map target type '{markerTargetType.FullName}' for property '{destProp.Name}'.");
            }

            if (sourceNullCheck != null) {
                adaptedResult = Expression.Condition(
                    sourceNullCheck,
                    adaptedResult,
                    CreateDefaultValueExpression(destProp.PropertyType)
                );
            }

            mappedBinding = Expression.Bind(destProp, adaptedResult);
            return true;
        }

        private static Expression UnwrapConvert(Expression expression) {
            var current = expression;
            while (current is UnaryExpression unary
                   && (unary.NodeType == ExpressionType.Convert || unary.NodeType == ExpressionType.ConvertChecked)) {
                current = unary.Operand;
            }

            return current;
        }

        private static bool IsUseMapMarker(MethodInfo method) {
            if (!method.IsGenericMethod || method.DeclaringType != typeof(MapifyProfile)) {
                return false;
            }

            var genericDefinition = method.GetGenericMethodDefinition();
            if (!string.Equals(genericDefinition.Name, UseMapMarkerName, StringComparison.Ordinal)) {
                return false;
            }

            if (genericDefinition.GetGenericArguments().Length != 2) {
                return false;
            }

            var parameterCount = genericDefinition.GetParameters().Length;
            return parameterCount == 1 || parameterCount == 2;
        }

        private static bool IsProjectToMarker(MethodInfo method) {
            if (!method.IsGenericMethod || method.DeclaringType != typeof(MapifyProjectToExtensions)) {
                return false;
            }

            var genericDefinition = method.GetGenericMethodDefinition();
            if (!string.Equals(genericDefinition.Name, ProjectToMarkerName, StringComparison.Ordinal)) {
                return false;
            }

            if (genericDefinition.GetGenericArguments().Length != 1) {
                return false;
            }

            var parameters = genericDefinition.GetParameters();
            if (parameters.Length < 1 || parameters.Length > 2) {
                return false;
            }

            if (!typeof(System.Collections.IEnumerable).IsAssignableFrom(parameters[0].ParameterType)
                && !typeof(IQueryable).IsAssignableFrom(parameters[0].ParameterType)) {
                return false;
            }

            if (parameters.Length == 2 && parameters[1].ParameterType != typeof(string)) {
                return parameters[1].ParameterType == typeof(bool);
            }

            return true;
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
        private static PropertyInfo? GetSourceProperty(IDictionary<string, PropertyInfo> sourceTypeProperties, PropertyInfo destProp) {
            // Try to find a matching property with the same name
            if (!sourceTypeProperties.TryGetValue(destProp.Name, out var sourceProp)) {
                return null;
            }

            return sourceProp;
        }

        private static bool TryGetImplicitBinding(
            ParameterExpression baseParam,
            PropertyInfo sourceProp,
            PropertyInfo destProp,
            out MemberAssignment binding
        ) {
            binding = null!;

            var sourceNullableType = Nullable.GetUnderlyingType(sourceProp.PropertyType);

            var isCompatibleType =
                destProp.PropertyType.IsAssignableFrom(sourceProp.PropertyType)
                || (sourceNullableType != null && destProp.PropertyType.IsAssignableFrom(sourceNullableType));

            if (!isCompatibleType) {
                return false;
            }

            binding = GetImplicitBinding(baseParam, sourceProp, destProp);
            return true;
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

        private static bool TryGetBindingFromExistingMap(
            ParameterExpression baseParam,
            PropertyInfo sourceProp,
            PropertyInfo destProp,
            Func<Type, Type, string?, LambdaExpression?>? existingMapResolver,
            out MemberAssignment binding
        ) {
            binding = null!;

            if (existingMapResolver == null) {
                return false;
            }

            var sourceAccess = Expression.Property(baseParam, sourceProp);

            if (!TryBuildMappedExpression(sourceAccess, sourceProp.PropertyType, destProp.PropertyType, existingMapResolver, null, out var adaptedResult, out var sourceNullCheck)) {
                return false;
            }

            // If source is nullable (or reference) and null, map to default(target).
            if (sourceNullCheck != null) {
                adaptedResult = Expression.Condition(
                    sourceNullCheck,
                    adaptedResult,
                    CreateDefaultValueExpression(destProp.PropertyType)
                );
            }

            binding = Expression.Bind(destProp, adaptedResult);
            return true;
        }

        private static bool TryBuildMappedExpression(
            Expression sourceAccess,
            Type sourceType,
            Type destinationType,
            Func<Type, Type, string?, LambdaExpression?> resolver,
            string? preferredMapName,
            out Expression mappedResult,
            out Expression? sourceNullCheck
        ) {
            mappedResult = null!;
            sourceNullCheck = null;

            if (TryBuildDirectMappedExpression(sourceAccess, sourceType, destinationType, resolver, preferredMapName, out mappedResult, out sourceNullCheck)) {
                return true;
            }

            if (TryBuildEnumerableMappedExpression(sourceAccess, sourceType, destinationType, resolver, preferredMapName, out mappedResult, out sourceNullCheck)) {
                return true;
            }

            return false;
        }

        private static bool TryBuildDirectMappedExpression(
            Expression sourceAccess,
            Type sourceType,
            Type destinationType,
            Func<Type, Type, string?, LambdaExpression?> resolver,
            string? preferredMapName,
            out Expression mappedResult,
            out Expression? sourceNullCheck
        ) {
            mappedResult = null!;
            sourceNullCheck = null;

            var mapExpr = ResolveMapForNullableVariants(sourceType, destinationType, resolver, preferredMapName);
            if (mapExpr == null || mapExpr.Parameters.Count != 1 || mapExpr.ReturnType == typeof(void)) {
                return false;
            }

            if (!TryAdaptSourceForMap(sourceAccess, mapExpr.Parameters[0].Type, out var adaptedSource, out sourceNullCheck)) {
                return false;
            }

            var mappedBody = new ParameterReplaceVisitor(mapExpr.Parameters[0], adaptedSource).Visit(mapExpr.Body)!;

            if (!TryAdaptMappedResult(mappedBody, destinationType, out mappedResult)) {
                return false;
            }

            return true;
        }

        private static bool TryBuildEnumerableMappedExpression(
            Expression sourceAccess,
            Type sourceType,
            Type destinationType,
            Func<Type, Type, string?, LambdaExpression?> resolver,
            string? preferredMapName,
            out Expression mappedResult,
            out Expression? sourceNullCheck
        ) {
            mappedResult = null!;
            sourceNullCheck = null;

            if (!TryGetEnumerableElementType(sourceType, out var sourceElementType)
                || !TryGetEnumerableElementType(destinationType, out var destinationElementType)
                || !TryGetEnumerableElementType(sourceAccess.Type, out var sourceAccessElementType)) {
                return false;
            }

            var elementMapExpr = ResolveMapForNullableVariants(sourceElementType, destinationElementType, resolver, preferredMapName);
            var itemParam = Expression.Parameter(sourceAccessElementType, "e");
            Expression adaptedItemResult;
            Expression? itemNullCheck;

            if (elementMapExpr != null && elementMapExpr.Parameters.Count == 1 && elementMapExpr.ReturnType != typeof(void)) {
                if (!TryAdaptSourceForMap(itemParam, elementMapExpr.Parameters[0].Type, out var adaptedItem, out itemNullCheck)) {
                    return false;
                }

                var mappedItemBody = new ParameterReplaceVisitor(elementMapExpr.Parameters[0], adaptedItem).Visit(elementMapExpr.Body)!;
                if (!TryAdaptMappedResult(mappedItemBody, destinationElementType, out adaptedItemResult)) {
                    return false;
                }
            } else {
                if (!TryBuildImplicitEnumerableElementProjection(itemParam, destinationElementType, out adaptedItemResult, out itemNullCheck)) {
                    return false;
                }
            }

            if (itemNullCheck != null) {
                adaptedItemResult = Expression.Condition(
                    itemNullCheck,
                    adaptedItemResult,
                    CreateDefaultValueExpression(destinationElementType)
                );
            }

            var selector = Expression.Lambda(adaptedItemResult, itemParam);
            var selectExpr = Expression.Call(
                typeof(Enumerable),
                nameof(Enumerable.Select),
                [sourceAccessElementType, destinationElementType],
                sourceAccess,
                selector
            );

            if (!TryMaterializeEnumerable(selectExpr, destinationType, destinationElementType, out mappedResult)) {
                return false;
            }

            if (CanBeNull(sourceAccess.Type) && !IsCollectionLikeType(sourceAccess.Type)) {
                sourceNullCheck = Expression.NotEqual(sourceAccess, Expression.Constant(null, sourceAccess.Type));
            }

            return true;
        }

        private static bool TryBuildImplicitEnumerableElementProjection(
            ParameterExpression itemParam,
            Type destinationElementType,
            out Expression projection,
            out Expression? itemNullCheck
        ) {
            projection = null!;
            itemNullCheck = null;

            if (itemParam.Type == destinationElementType) {
                projection = itemParam;
                return true;
            }

            var sourceNullableType = Nullable.GetUnderlyingType(itemParam.Type);
            var destinationNullableType = Nullable.GetUnderlyingType(destinationElementType);

            if (destinationNullableType != null && itemParam.Type == destinationNullableType) {
                projection = Expression.Convert(itemParam, destinationElementType);
                return true;
            }

            if (destinationNullableType == null && sourceNullableType != null && destinationElementType == sourceNullableType) {
                itemNullCheck = Expression.NotEqual(itemParam, Expression.Constant(null, itemParam.Type));
                projection = Expression.Property(itemParam, "Value");
                return true;
            }

            if (destinationElementType.IsAssignableFrom(itemParam.Type)) {
                projection = itemParam;
                return true;
            }

            return false;
        }

        private static bool TryMaterializeEnumerable(
            Expression enumerableExpression,
            Type destinationType,
            Type destinationElementType,
            out Expression materialized
        ) {
            materialized = null!;

            if (destinationType.IsArray) {
                materialized = Expression.Call(
                    typeof(Enumerable),
                    nameof(Enumerable.ToArray),
                    [destinationElementType],
                    enumerableExpression
                );
                return true;
            }

            if (destinationType.IsAssignableFrom(enumerableExpression.Type)) {
                materialized = enumerableExpression;
                return true;
            }

            var toListExpr = Expression.Call(
                typeof(Enumerable),
                nameof(Enumerable.ToList),
                [destinationElementType],
                enumerableExpression
            );

            if (destinationType.IsAssignableFrom(toListExpr.Type)) {
                materialized = destinationType == toListExpr.Type
                    ? toListExpr
                    : Expression.Convert(toListExpr, destinationType);
                return true;
            }

            var ienumerableOfTarget = typeof(IEnumerable<>).MakeGenericType(destinationElementType);
            var ctor = destinationType.GetConstructor([ienumerableOfTarget]);
            if (ctor != null) {
                materialized = Expression.New(ctor, enumerableExpression);
                return true;
            }

            return false;
        }

        private static bool TryGetEnumerableElementType(Type type, out Type elementType) {
            elementType = null!;

            if (type == typeof(string)) {
                return false;
            }

            if (type.IsArray) {
                elementType = type.GetElementType()!;
                return true;
            }

            var enumerableInterface = type
                .GetInterfaces()
                .Concat(new[] { type })
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

            if (enumerableInterface == null) {
                return false;
            }

            elementType = enumerableInterface.GetGenericArguments()[0];
            return true;
        }

        private static LambdaExpression? ResolveMapForNullableVariants(
            Type sourceType,
            Type destinationType,
            Func<Type, Type, string?, LambdaExpression?> resolver,
            string? preferredMapName
        ) {
            var sourceCoreType = Nullable.GetUnderlyingType(sourceType) ?? sourceType;
            var destinationCoreType = Nullable.GetUnderlyingType(destinationType) ?? destinationType;

            // Prefer exact first, then lifted variants.
            return resolver(sourceType, destinationType, preferredMapName)
                ?? resolver(sourceType, destinationCoreType, preferredMapName)
                ?? resolver(sourceCoreType, destinationType, preferredMapName)
                ?? resolver(sourceCoreType, destinationCoreType, preferredMapName);
        }

        private static bool TryAdaptSourceForMap(
            Expression sourceAccess,
            Type mapSourceType,
            out Expression adaptedSource,
            out Expression? sourceHasValueCheck
        ) {
            adaptedSource = sourceAccess;
            sourceHasValueCheck = null;

            if (sourceAccess.Type == mapSourceType) {
                // For reference/nullable values, keep null fallback behavior.
                if (CanBeNull(sourceAccess.Type) && !IsCollectionLikeType(sourceAccess.Type)) {
                    sourceHasValueCheck = Expression.NotEqual(sourceAccess, Expression.Constant(null, sourceAccess.Type));
                }
                return true;
            }

            var sourceNullableUnderlying = Nullable.GetUnderlyingType(sourceAccess.Type);
            var mapNullableUnderlying = Nullable.GetUnderlyingType(mapSourceType);

            // T? -> T
            if (sourceNullableUnderlying != null && sourceNullableUnderlying == mapSourceType) {
                sourceHasValueCheck = Expression.NotEqual(sourceAccess, Expression.Constant(null, sourceAccess.Type));
                adaptedSource = Expression.Property(sourceAccess, "Value");
                return true;
            }

            // T -> T?
            if (mapNullableUnderlying != null && sourceAccess.Type == mapNullableUnderlying) {
                adaptedSource = Expression.Convert(sourceAccess, mapSourceType);
                return true;
            }

            if (mapSourceType.IsAssignableFrom(sourceAccess.Type)) {
                if (CanBeNull(sourceAccess.Type) && !IsCollectionLikeType(sourceAccess.Type)) {
                    sourceHasValueCheck = Expression.NotEqual(sourceAccess, Expression.Constant(null, sourceAccess.Type));
                }
                adaptedSource = sourceAccess;
                return true;
            }

            return false;
        }

        private static bool TryAdaptMappedResult(Expression mappedBody, Type targetType, out Expression adaptedResult) {
            adaptedResult = mappedBody;

            if (mappedBody.Type == targetType) {
                return true;
            }

            var targetNullableUnderlying = Nullable.GetUnderlyingType(targetType);
            var mappedNullableUnderlying = Nullable.GetUnderlyingType(mappedBody.Type);

            // T -> T?
            if (targetNullableUnderlying != null && mappedBody.Type == targetNullableUnderlying) {
                adaptedResult = Expression.Convert(mappedBody, targetType);
                return true;
            }

            // T? -> T (keep default fallback)
            if (mappedNullableUnderlying != null && targetType == mappedNullableUnderlying) {
                var hasValue = Expression.NotEqual(mappedBody, Expression.Constant(null, mappedBody.Type));
                var value = Expression.Property(mappedBody, "Value");
                adaptedResult = Expression.Condition(hasValue, value, Expression.Default(targetType));
                return true;
            }

            if (targetType.IsAssignableFrom(mappedBody.Type)) {
                adaptedResult = mappedBody;
                return true;
            }

            return false;
        }

        private static bool CanBeNull(Type type)
            => !type.IsValueType || Nullable.GetUnderlyingType(type) != null;

        private static bool IsCollectionLikeType(Type type)
            => type != typeof(string)
               && typeof(System.Collections.IEnumerable).IsAssignableFrom(type)
               && type != typeof(byte[]);

        private static Expression CreateDefaultValueExpression(Type type)
            => CanBeNull(type)
                ? Expression.Constant(null, type)
                : Expression.Default(type);

        private static LambdaExpression? TryGetRegisteredMap(Type sourceType, Type destinationType, string? name) {
            if (!string.IsNullOrWhiteSpace(name)) {
                return null;
            }

            var key = new Tuple<Type, Type>(sourceType, destinationType);
            return Converters.TryGetValue(key, out var existingConverter) ? existingConverter : null;
        }
    }
}
