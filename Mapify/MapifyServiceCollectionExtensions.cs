using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection {
    public static class MapifyServiceCollectionExtensions {
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
                            .Select(x => (Mapify.NET.IMapifyProfile)sp.GetRequiredService(x.ProfileType))
                            .ToArray();

                        return new Mapify.NET.Mapify(registrations);
                    },
                    lifecycle
                );

                services.Add(descriptor);
            }

            return services;
        }

        public static IServiceCollection AddMapify(this IServiceCollection services, ServiceLifetime lifecycle, params Assembly[] profileAssemblies) {
            if (services == null) {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddMapifyProfiles(profileAssemblies);
            services.AddMapify(lifecycle);
            return services;
        }

        public static IServiceCollection AddMapify(this IServiceCollection services, params Assembly[] profileAssemblies) {
            return services.AddMapify(ServiceLifetime.Singleton, profileAssemblies);
        }

        public static IServiceCollection AddMapifyProfile<TProfile>(this IServiceCollection services)
            where TProfile : class, Mapify.NET.IMapifyProfile {
            return services.AddMapifyProfile<TProfile>(null);
        }

        public static IServiceCollection AddMapifyProfile<TProfile>(this IServiceCollection services, string? mapperName)
            where TProfile : class, Mapify.NET.IMapifyProfile {
            if (services == null) {
                throw new ArgumentNullException(nameof(services));
            }

            if (mapperName != null && string.IsNullOrWhiteSpace(mapperName)) {
                throw new ArgumentException("Mapper name must not be empty when provided.", nameof(mapperName));
            }

            RegisterProfileType(services, mapperName, typeof(TProfile));
            return services;
        }

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
                            .Select(x => (Mapify.NET.IMapifyProfile)sp.GetRequiredService(x.ProfileType))
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

        public static IServiceCollection AddMapifyNamed(this IServiceCollection services, string name, ServiceLifetime lifecycle, params Assembly[] profileAssemblies) {
            if (services == null) {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddMapifyProfiles(name, profileAssemblies);
            services.AddMapifyNamed(name, lifecycle);
            return services;
        }

        public static IServiceCollection AddMapifyProfiles(this IServiceCollection services, params Assembly[] profileAssemblies) {
            return services.AddMapifyProfiles(null, profileAssemblies);
        }

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
                    if (!typeof(Mapify.NET.IMapifyProfile).IsAssignableFrom(type) || type.IsAbstract || type.IsInterface) {
                        continue;
                    }

                    RegisterProfileType(services, mapperName, type);
                }
            }

            return services;
        }

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
