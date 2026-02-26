using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection {
    /// <summary>
    /// Dependency injection registration helpers for Mapify profiles and mapper instances.
    /// </summary>
    public static class MapifyServiceCollectionExtensions {
        /// <summary>
        /// Registers the default <see cref="Mapify.NET.IMapify"/> mapper using already-registered profile types.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="lifecycle">The mapper lifetime.</param>
        /// <returns>The same service collection for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="lifecycle"/> is not a valid <see cref="ServiceLifetime"/> value.</exception>
        public static IServiceCollection AddMapify(this IServiceCollection services, ServiceLifetime lifecycle = ServiceLifetime.Singleton) {
            if (services == null) {
                throw new ArgumentNullException(nameof(services));
            }

            if (!Enum.IsDefined(typeof(ServiceLifetime), lifecycle)) {
                throw new ArgumentOutOfRangeException(nameof(lifecycle));
            }

            if (!services.Any(x => x.ServiceType == typeof(Mapify.NET.IMapify))) {
                var descriptor = new ServiceDescriptor(
                    typeof(Mapify.NET.IMapify),
                    sp => {
                        var registrations = sp.GetServices<Mapify.NET.MapifyProfileTypeRegistration>()
                            .Where(x => x.MapperName == null)
                            .Select(x => (Mapify.NET.MapifyProfile)sp.GetRequiredService(x.ProfileType))
                            .ToArray();

                        return new Mapify.NET.Mapify(registrations);
                    },
                    lifecycle
                );

                services.Add(descriptor);
            }

            return services;
        }

        /// <summary>
        /// Registers profiles from the provided assemblies and then registers the default <see cref="Mapify.NET.IMapify"/> mapper.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="lifecycle">The mapper lifetime.</param>
        /// <param name="profileAssemblies">Assemblies to scan for <see cref="Mapify.NET.MapifyProfile"/> implementations.</param>
        /// <returns>The same service collection for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
        public static IServiceCollection AddMapify(this IServiceCollection services, ServiceLifetime lifecycle, params Assembly[] profileAssemblies) {
            if (services == null) {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddMapifyProfiles(profileAssemblies);
            services.AddMapify(lifecycle);
            return services;
        }

        /// <summary>
        /// Registers profiles from the provided assemblies and then registers the default mapper with singleton lifetime.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="profileAssemblies">Assemblies to scan for <see cref="Mapify.NET.MapifyProfile"/> implementations.</param>
        /// <returns>The same service collection for chaining.</returns>
        public static IServiceCollection AddMapify(this IServiceCollection services, params Assembly[] profileAssemblies) {
            return services.AddMapify(ServiceLifetime.Singleton, profileAssemblies);
        }

        /// <summary>
        /// Registers a specific profile type for the default mapper.
        /// </summary>
        /// <typeparam name="TProfile">The profile type to register.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <returns>The same service collection for chaining.</returns>
        public static IServiceCollection AddMapifyProfile<TProfile>(this IServiceCollection services)
            where TProfile : Mapify.NET.MapifyProfile {
            return services.AddMapifyProfile<TProfile>(null);
        }

        /// <summary>
        /// Registers a specific profile type for a named mapper context (or default when name is null).
        /// </summary>
        /// <typeparam name="TProfile">The profile type to register.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="mapperName">The optional mapper name. Use null for the default mapper.</param>
        /// <returns>The same service collection for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="mapperName"/> is whitespace.</exception>
        public static IServiceCollection AddMapifyProfile<TProfile>(this IServiceCollection services, string? mapperName)
            where TProfile : Mapify.NET.MapifyProfile {
            if (services == null) {
                throw new ArgumentNullException(nameof(services));
            }

            if (mapperName != null && string.IsNullOrWhiteSpace(mapperName)) {
                throw new ArgumentException("Mapper name must not be empty when provided.", nameof(mapperName));
            }

            RegisterProfileType(services, mapperName, typeof(TProfile));
            return services;
        }

        /// <summary>
        /// Registers a named mapper entry that can later be resolved via <see cref="GetMapify(IServiceProvider, string)"/>.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="name">The mapper name.</param>
        /// <param name="lifecycle">The mapper lifetime.</param>
        /// <returns>The same service collection for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null, empty, or whitespace.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="lifecycle"/> is not a valid <see cref="ServiceLifetime"/> value.</exception>
        public static IServiceCollection AddMapifyNamed(this IServiceCollection services, string name, ServiceLifetime lifecycle = ServiceLifetime.Singleton) {
            if (services == null) {
                throw new ArgumentNullException(nameof(services));
            }

            if (string.IsNullOrWhiteSpace(name)) {
                throw new ArgumentException("Mapper name must not be null or whitespace.", nameof(name));
            }

            if (!Enum.IsDefined(typeof(ServiceLifetime), lifecycle)) {
                throw new ArgumentOutOfRangeException(nameof(lifecycle));
            }

            if (!services.Any(x => x.ServiceType == typeof(Mapify.NET.NamedMapifyRegistration)
                && x.ImplementationInstance is Mapify.NET.NamedMapifyRegistration existing
                && string.Equals(existing.Name, name, StringComparison.Ordinal))) {
                services.AddSingleton(new Mapify.NET.NamedMapifyRegistration(name, lifecycle));

                var descriptor = new ServiceDescriptor(
                    typeof(Mapify.NET.INamedMapify),
                    sp => {
                        var registrations = sp.GetServices<Mapify.NET.MapifyProfileTypeRegistration>()
                            .Where(x => string.Equals(x.MapperName, name, StringComparison.Ordinal))
                            .Select(x => (Mapify.NET.MapifyProfile)sp.GetRequiredService(x.ProfileType))
                            .ToArray();

                        var mapper = new Mapify.NET.Mapify(registrations);
                        return new Mapify.NET.NamedMapify(name, mapper);
                    },
                    lifecycle
                );

                services.Add(descriptor);
            }

            return services;
        }

        /// <summary>
        /// Registers profiles from the provided assemblies and then registers a named mapper.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="name">The mapper name.</param>
        /// <param name="lifecycle">The mapper lifetime.</param>
        /// <param name="profileAssemblies">Assemblies to scan for <see cref="Mapify.NET.MapifyProfile"/> implementations.</param>
        /// <returns>The same service collection for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
        public static IServiceCollection AddMapifyNamed(this IServiceCollection services, string name, ServiceLifetime lifecycle, params Assembly[] profileAssemblies) {
            if (services == null) {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddMapifyProfiles(name, profileAssemblies);
            services.AddMapifyNamed(name, lifecycle);
            return services;
        }

        /// <summary>
        /// Scans assemblies for profile types and registers them for the default mapper.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="profileAssemblies">Assemblies to scan for <see cref="Mapify.NET.MapifyProfile"/> implementations.</param>
        /// <returns>The same service collection for chaining.</returns>
        public static IServiceCollection AddMapifyProfiles(this IServiceCollection services, params Assembly[] profileAssemblies) {
            return services.AddMapifyProfiles(null, profileAssemblies);
        }

        /// <summary>
        /// Scans assemblies for profile types and registers them for a named mapper context (or default when name is null).
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="mapperName">The optional mapper name. Use null for the default mapper.</param>
        /// <param name="profileAssemblies">Assemblies to scan for <see cref="Mapify.NET.MapifyProfile"/> implementations.</param>
        /// <returns>The same service collection for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> or <paramref name="profileAssemblies"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="mapperName"/> is whitespace.</exception>
        public static IServiceCollection AddMapifyProfiles(this IServiceCollection services, string? mapperName, params Assembly[] profileAssemblies) {
            if (services == null) {
                throw new ArgumentNullException(nameof(services));
            }

            if (mapperName != null && string.IsNullOrWhiteSpace(mapperName)) {
                throw new ArgumentException("Mapper name must not be empty when provided.", nameof(mapperName));
            }

            if (profileAssemblies == null) {
                throw new ArgumentNullException(nameof(profileAssemblies));
            }

            foreach (var assembly in profileAssemblies.Distinct()) {
                foreach (var type in GetLoadableTypes(assembly)) {
                    if (!typeof(Mapify.NET.MapifyProfile).IsAssignableFrom(type) || type.IsAbstract || type.IsInterface) {
                        continue;
                    }

                    RegisterProfileType(services, mapperName, type);
                }
            }

            return services;
        }

        /// <summary>
        /// Resolves a named mapper instance from the service provider.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <param name="name">The mapper name.</param>
        /// <returns>The resolved mapper instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="serviceProvider"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null, empty, or whitespace.</exception>
        /// <exception cref="InvalidOperationException">Thrown when no mapper is registered for the given name.</exception>
        public static Mapify.NET.IMapify GetMapify(this IServiceProvider serviceProvider, string name) {
            if (serviceProvider == null) {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            if (string.IsNullOrWhiteSpace(name)) {
                throw new ArgumentException("Mapper name must not be null or whitespace.", nameof(name));
            }

            var named = serviceProvider.GetServices<Mapify.NET.INamedMapify>()
                .SingleOrDefault(x => string.Equals(x.Name, name, StringComparison.Ordinal));

            if (named == null) {
                throw new InvalidOperationException($"No named Mapify mapper registered with name '{name}'.");
            }

            return named.Mapper;
        }

        private static void RegisterProfileType(IServiceCollection services, string? mapperName, Type type) {
            if (services.Any(d => d.ServiceType == typeof(Mapify.NET.MapifyProfileTypeRegistration)
                && d.ImplementationInstance is Mapify.NET.MapifyProfileTypeRegistration existing
                && string.Equals(existing.MapperName, mapperName, StringComparison.Ordinal)
                && existing.ProfileType == type)) {
                return;
            }

            if (!services.Any(d => d.ServiceType == type)) {
                services.AddTransient(type);
            }

            services.AddSingleton(new Mapify.NET.MapifyProfileTypeRegistration(mapperName, type));
        }

        private static IEnumerable<Type> GetLoadableTypes(Assembly assembly) {
            try {
                return assembly.GetTypes();
            } catch (ReflectionTypeLoadException ex) {
                return ex.Types.Where(t => t != null).Cast<Type>();
            }
        }
    }
}
