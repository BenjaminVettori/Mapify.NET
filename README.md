# Mapify.NET

[![Tests](https://github.com/BenjaminVettori/Mapify.NET/actions/workflows/tests.yml/badge.svg?branch=main)](https://github.com/BenjaminVettori/Mapify.NET/actions/workflows/tests.yml)
[![Build](https://github.com/BenjaminVettori/Mapify.NET/actions/workflows/build.yml/badge.svg?branch=main)](https://github.com/BenjaminVettori/Mapify.NET/actions/workflows/build.yml)

Mapify is a lightweight .NET library for creating static mapping expressions for C# objects. It bridges the gap between in-memory object mapping and LINQ projections (e.g., Entity Framework), allowing you to reuse the same mapping logic consistently across your application.

## Features ✨

*   **Zero Boilerplate**: Automatically maps properties with compatible names and types.
*   **Entity Framework Compatible**: Generates expression trees compatible with `IQueryable` projections.
*   **Safe**: Handles null checks and type conversions automatically.
*   **Flexible**: Supports explicit overrides and partial mappings.
*   **Performance**: Caches compiled delegates for in-memory mapping.

## Installation 📦

Install via NuGet:

```bash
dotnet add package Mapify.NET
```

## Supported Frameworks 📋

*   .NET 8.0, 9.0, 10.0
*   .NET Standard 2.0, 2.1
*   .NET Framework 4.6.2+

## Getting Started 🚀

### 1. Define your Classes

```csharp
public class Address {
    public string Street { get; set; }
    public string City { get; set; }
}

public class AddressDto {
    public string Street { get; set; }
    public string City { get; set; }
}

public class Person {
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public Address MainAddress { get; set; }
    public ICollection<Address> Addresses { get; set; }
}

public class PersonDto {
    public string Name { get; set; }
    public AddressDto MainAddress { get; set; }
    public ICollection<AddressDto> Addresses { get; set; }
}
```

### 2. Create Profiles (recommended)

The recommended approach is profile + instance mapper (`IMapify`).
It keeps mapping configuration explicit and is easier to test.

```csharp
using Mapify.NET;
using Microsoft.Extensions.DependencyInjection;

public class PersonProfile : MapifyProfile {
    protected override void Configure() {
        CreateMap<Person, PersonDto>();
    }
}

// Use UseMap<TSource, TTarget>(sourceMember) to explicitly mark that
// a property should be mapped via an existing registered map.
public class QueryProfile : MapifyProfile {
    protected override void Configure() {
        CreateMap<Address, AddressDto>();

        CreateMap<Person, PersonDto>(p => new PersonDto {
            Name = p.FirstName + " " + p.LastName,
            MainAddress = UseMap<Address, AddressDto>(p.MainAddress)
        });
    }
}

// Named maps: same source/target pair, different map names.
public class PersonValueProfile : MapifyProfile {
    protected override void Configure() {
        // default map for Person -> string
        CreateMap<Person, string>(x => x.FirstName);

        // named maps for the same Person -> string type pair
        CreateMap<Person, string>("FullName", x => x.FirstName + " " + x.LastName);
        CreateMap<Person, string>("Initials", x => x.FirstName.Substring(0, 1) + x.LastName.Substring(0, 1));
    }
}

// UseMap also supports named maps in profile initializers.
public class StudentProfile : MapifyProfile {
    protected override void Configure() {
        CreateMap<Student, StudentDto>("Raw", x => new StudentDto { Name = x.Name });
        CreateMap<Student, StudentDto>("Upper", x => new StudentDto { Name = x.Name.ToUpper() });

        CreateMap<Classroom, ClassroomDto>(x => new ClassroomDto {
            Students = UseMap<IEnumerable<Student>, IEnumerable<StudentDto>>("Upper", x.Students)
        });
    }
}

// If source and destination property names differ, pass the source explicitly.
public class NumberProfile : MapifyProfile {
    protected override void Configure() {
        CreateMap<NumberSource, NumberDto>();

        CreateMap<Order, OrderDto>(x => new OrderDto {
            // Order.SourceNumber -> OrderDto.Number
            Number = UseMap<NumberSource, NumberDto>(x.SourceNumber)
        });
    }
}

var services = new ServiceCollection();

// Scans the given assemblies for all IMapifyProfile implementations,
// registers them, and registers IMapify as singleton.
services.AddMapify(typeof(PersonProfile).Assembly);

// Scans the given assemblies for all IMapifyProfile implementations,
// registers them, and registers IMapify as Scoped.
services.AddMapify(ServiceLifetime.Scoped, typeof(PersonProfile).Assembly);

// Manually add profiles of the given assembly
// registers IMapify as Scoped without adding profiles
services.AddMapifyProfiles(typeof(PersonProfile).Assembly);
services.AddMapify(ServiceLifetime.Scoped);

// Register a specific profile manually
services.AddMapifyProfile<PersonProfile>();
services.AddMapify(ServiceLifetime.Scoped);

// Named mapper with isolated profile set
services.AddMapifyProfile<PersonProfile>("queries");
services.AddMapifyNamed("queries", ServiceLifetime.Transient);

var provider = services.BuildServiceProvider();
var defaultMapper = provider.GetRequiredService<IMapify>();
var queryMapper = provider.GetMapify("queries");
```

`CreateMap<TSource, TTarget>(...)` inside `MapifyProfile` is registration-only.
Map building is deferred until all profiles are registered, so unordered registrations are supported.

When you need explicit nested map usage in a profile initializer, use `UseMap<TSource, TTarget>(x.SourceMember)`.
During build, Mapify resolves the dependency to the registered map (including nullable variants).

To force a specific named map, use `UseMap<TSource, TTarget>("Name", x.SourceMember)`.

To explicitly ignore a destination property in a partial map, use `Ignore<T>()`:

```csharp
CreateMap<Person, PersonDto>(x => new PersonDto {
    Name = x.FirstName + " " + x.LastName,
    InternalCode = Ignore<string>()
});
```

Ignore behavior:

- For **new object mapping** (`Map(source)` / projections), ignored properties are not mapped from source.
- If an ignored destination property is marked `required`, Mapify emits `default(T)` for that property.
- For **map-to-existing** (`Map(source, existing)`), ignored properties are left unchanged on the target instance.

You can also chain LINQ operators after `UseMap`, for example:

```csharp
CreateMap<Person, PersonDto>(x => new PersonDto {
    Addresses = UseMap<IEnumerable<Address>, IEnumerable<AddressDto>>(x.Addresses)
        .OrderBy(dto => dto.StreetName)
});
```

`UseMap` also supports arrays and enumerable types. If a map exists for element types (`TSrc -> TDest`),
you can use it for collection shapes like `TSrc[] -> TDest[]` and `IEnumerable<TSrc> -> IEnumerable<TDest>`.

This also applies to **named** maps: if `UseMap` is called with a name for a collection shape,
Mapify resolves the named map for the individual element types.

```csharp
CreateMap<Address, AddressDto>("Postal", x => new AddressDto {
    StreetName = x.StreetName
});

CreateMap<Person, PersonDto>(x => new PersonDto {
    Addresses = UseMap<IEnumerable<Address>, IEnumerable<AddressDto>>("Postal", x.Addresses)
        .OrderBy(dto => dto.StreetName)
});
```

In this example, Mapify applies the named element map `Address -> AddressDto` with name `"Postal"` for each item.

### 3. Use the instance mapper in-memory

```csharp
var mapper = provider.GetRequiredService<IMapify>();

var dto = mapper.Map<Person, PersonDto>(person);

// named map execution
var fullName = mapper.Map<Person, string>(person, "FullName");

var existing = new PersonDto();
mapper.Map(person, existing);

// named map-to-existing execution
mapper.Map(person, existing, "SomeNamedMap");
```

### 4. Use the instance mapper with Entity Framework (`IQueryable`)

`IMapify.GetMap<TSource, TTarget>()` returns the expression for projections, or `null` if missing (and default-map fallback is disabled).
Use `IMapify.GetRequiredMap<TSource, TTarget>()` when missing maps should throw.

The static `Mapper` API follows the same pattern:

- `Mapper.GetMap<TSource, TTarget>()` may return `null`
- `Mapper.GetRequiredMap<TSource, TTarget>()` throws when missing

For convenience, you can also call `ProjectTo<TTarget>()` directly on an `IQueryable`.
Use the overload with `IMapify` to resolve instance-based profile maps:

```csharp
var people = await _dbContext.Persons
    .ProjectTo<PersonDto>(_mapify)
    .OrderBy(x => x.Name)
    .ToArrayAsync(cancellationToken);
```

Named map projections are supported as well:

```csharp
var people = await _dbContext.Persons
    .ProjectTo<PersonDto>(_mapify, "Masked")
    .ToArrayAsync(cancellationToken);
```

If you use the static `Mapper` API (`Mapper.AddMap(...)`), you can call:

```csharp
var people = _dbContext.Persons
    .ProjectTo<PersonDto>()
    .OrderBy(x => x.Name)
    .ToArray();
```

For named maps, both APIs follow the same rule:

- `GetMap<TSource, TTarget>("Name")` may return `null` when the named map is missing
- `GetRequiredMap<TSource, TTarget>("Name")` throws when the named map is missing

`UseMap` works with expressions too (not only direct properties). For example, you can filter children before mapping:

```csharp
public class StudentProfile : MapifyProfile {
    protected override void Configure() {
        CreateMap<Student, StudentDto>();

        CreateMap<Classroom, ClassroomDto>(x => new ClassroomDto {
            Students = UseMap<IEnumerable<Student>, IEnumerable<StudentDto>>(
                x.Students.Where(s => s.Name != null)
            )
        });
    }
}
```

```csharp
public async Task<IEnumerable<PersonDto>> GetPersonDtosAsync(int skip, int take, CancellationToken cancellationToken = default) {
    var mapExpr = _mapify.GetMap<Person, PersonDto>();

    return await _dbContext.Persons
        .Select(mapExpr)
        .OrderBy(x => x.Name)
        .Skip(skip)
        .Take(take)
        .ToArrayAsync(cancellationToken);
}
```

The same pattern works for named maps as well:

```csharp
Students = UseMap<IEnumerable<Student>, IEnumerable<StudentDto>>(
    "Upper",
    x.Students.Where(s => s.Name != null)
)
```

`UseMap` can also be used inside calculations. For example, map measurements first,
then take the maximum Fahrenheit value and convert it to Celsius:

```csharp
public class MeasurementProfile : MapifyProfile {
    protected override void Configure() {
        CreateMap<Measurement, MeasurementDto>();

        CreateMap<WeatherSample, WeatherSampleDto>(x => new WeatherSampleDto {
            MaxTemperatureCelsius = (
                UseMap<IEnumerable<Measurement>, IEnumerable<MeasurementDto>>(x.Measurements)
                    .OrderBy(m => m.Fahrenheit)
                    .Max(m => m.Fahrenheit)
                - 32m) * 5m / 9m
        });
    }
}
```

Chaining also works for named maps:

```csharp
Addresses = UseMap<IEnumerable<Address>, IEnumerable<AddressDto>>("Postal", x.Addresses)
    .OrderBy(dto => dto.StreetName)
```

For EF/EF Core projections, if you apply ordering or other sequence operators after `UseMap`, materialize the sequence for stable provider translation:

```csharp
Addresses = UseMap<IEnumerable<Address>, IEnumerable<AddressDto>>(x.Addresses)
    .OrderBy(dto => dto.StreetName)
    .ToList()
```

Inside `CreateMap` expressions you can also use `ProjectTo<TTarget>()` on enumerable members.
Mapify rewrites this marker to the same internal behavior as `UseMap`.

```csharp
CreateMap<Person, PersonDto>(x => new PersonDto {
    Addresses = x.Addresses.ProjectTo<AddressDto>().ToArray()
});
```

The marker also supports named mappings:

```csharp
CreateMap<Person, PersonDto>("Masked", x => new PersonDto {
    Addresses = x.Addresses.ProjectTo<AddressDto>("Postal").ToArray()
});
```

## Detailed Functionality 📚

### Implicit Mappings

When `CreateMap<TSource, TTarget>()` is called, Mapify automatically generates bindings for properties where:
1.  **Names Match**: Source and Destination property names are identical.
2.  **Types are Compatible**:
    *   Exact match.
    *   Target is assignable from Source.
    *   **Nullable Handling**:
        *   `T` -> `T?` (Implicit cast)
        *   `T?` -> `T` (Uses source value if not null, otherwise default(T))

    If a map already exists for a same-name property type pair (e.g. `Address -> AddressDto`),
    Mapify uses that map implicitly before falling back to direct assignment.

### Named Mappings

Named mappings let you register multiple mappings for the same source/target type pair.

```csharp
public class PersonValueProfile : MapifyProfile {
    protected override void Configure() {
        CreateMap<Person, string>("FullName", x => x.FirstName + " " + x.LastName);
        CreateMap<Person, string>("Initials", x => x.FirstName.Substring(0, 1) + x.LastName.Substring(0, 1));
    }
}

var fullName = mapify.Map<Person, string>(person, "FullName");
var initials = mapify.Map<Person, string>(person, "Initials");
```

Rules:
* A default map and named maps can coexist for the same `TSource -> TTarget`.
* Each `(TSource, TTarget, Name)` combination must be unique.
* `UseMap` can target named maps via `UseMap<TSource, TTarget>("Name", sourceExpression)`.

### Static API (advanced scenarios)

The static `Mapper` API is still fully supported, but typically used in advanced scenarios.

#### Static map declarations

```csharp
using Mapify.NET;
using System.Linq.Expressions;

public static class PersonMappings {
    public static readonly Expression<Func<Person, PersonDto>> PersonToPersonDto =
        Mapper.CreateMap<Person, PersonDto>(p => new PersonDto {
            Name = $"{p.FirstName} {p.LastName}",
            MainAddress = AddressMappings.AddressToAddressDto.Invoke(p.MainAddress)
        });
}

public static class AddressMappings {
    public static readonly Expression<Func<Address, AddressDto>> AddressToAddressDto =
        Mapper.CreateMap<Address, AddressDto>();
}
```

#### Static API with Entity Framework + LINQKit

If your static expressions use `.Invoke(...)`, combine with LINQKit and `.AsExpandable()`.

```csharp
public async Task<IEnumerable<PersonDto>> GetPersonDtosAsync(int skip, int take, CancellationToken cancellationToken = default) {
    return await _dbContext.Persons
        .AsExpandable()
        .Select(PersonMappings.PersonToPersonDto)
        .OrderBy(x => x.Name)
        .Skip(skip)
        .Take(take)
        .ToArrayAsync(cancellationToken);
}
```

`AsExpandable()` is the key piece that lets EF translate expressions that use `.Invoke()`.

Use the package that matches your scenario:

- `LinqKit.Core`: expression composition utilities (`PredicateBuilder`, `Invoke`, `Expand`) without EF integration.
- `LinqKit` or `LinqKit.EntityFramework`: for Entity Framework 6.x.
- `LinqKit.Microsoft.EntityFrameworkCore`: for Entity Framework Core.

For EF Core, the package ID stays the same (`LinqKit.Microsoft.EntityFrameworkCore`), but the major version should match EF Core major.

### Global Configuration & Static Maps

You can register mappings globally to use the static `Mapper.Map` convenience methods.

```csharp
// Register a map globally
Mapper.AddMap(PersonMappings.PersonToPersonDto);

// Or create and add in one step
Mapper.CreateAndAddMap<Person, PersonDto>(p => new PersonDto { ... });

// Use the global map anywhere
var dto = Mapper.Map<Person, PersonDto>(person);
```

### Value Mappings (non-initializer)

Mapify also supports mappings where the expression returns a value directly (not `new TTarget { ... }`).

```csharp
Mapper.AddMap<Person, string>(x => x.FirstName);
Mapper.AddMap<SourceStatus, TargetStatus>(x => x == SourceStatus.Active ? TargetStatus.Enabled : TargetStatus.Disabled);

var name = Mapper.Map<Person, string>(person);
var status = Mapper.Map<SourceStatus, TargetStatus>(SourceStatus.Active);
```

> Note: value mappings are supported for `Map(source)` only. `Map(source, target)` requires an object-initializer mapping.

### Explicit Overrides & Coalescing

You can provide a partial initializer to override specific properties. Mapify also rewrites null-coalescing operators (`??`) to conditional expressions (`x != null ? x : y`) to ensure compatibility with all LINQ providers (some EF versions struggle with `??`).

```csharp
Mapper.CreateMap<Person, PersonDto>(p => new PersonDto {
    // Explicit override
    Name = p.FirstName + " " + p.LastName,
    
    // Coalescing is rewritten for EF compatibility
    Region = p.Region ?? "Unknown" 
});
```

## Caching & Performance ⚡

*   **Compiled Delegates**: Accessors are compiled to delegates.
*   **Strategy**:
    *   **Extension Method (`.Map()`)**: Caches the compiled delegate for that specific expression instance.
    *   **Static `Mapper.Map`**: Uses a global cache.
        *   **Priority**: Explicitly added maps (`AddMap`) take precedence over implicitly generated ones.
        *   **Auto-Cache**: If you use `Mapper.Map` with `useDefaultMapIfTypeMapIsMissing: true` *before* adding a custom map, a default map is generated and cached. However, calling `AddMap` later **will overwrite** this cache with your custom definition, so you can safely upgrade from default to custom maps at runtime.

## Advanced Usage 🛠️

For advanced scenarios, Mapify exposes several lower-level methods.

### Compiling Mappers Manually

If you need high-performance bulk mapping to **existing** objects and want to manage the delegate lifecycle yourself (referencing `Action<TSource, TTarget>`), you can use `CompileMapper`.

```csharp
// Get the map expression
var mapExpr = PersonMappings.PersonToPersonDto;

// Compile to an Action<Person, PersonDto>
Action<Person, PersonDto> mapAction = Mapper.CompileMapper(mapExpr);

// Use it in a hot loop (zero dictionary lookups)
var target = new PersonDto();
foreach (var item in largeCollection) {
    mapAction(item, target);
    // ...
}
```

### Retrieving Maps

You can retrieve registered maps using `GetMap`. This is useful in generic or dynamic contexts where you don't have direct access to the static field.
`GetMap` returns `null` if no map exists and default-map fallback is disabled.
If you want throwing behavior, use `GetRequiredMap`.

```csharp
// Retrieve a registered map (or null)
var mapExpr = Mapper.GetMap<Person, PersonDto>();

// Or retrieve with fallback to default map generation
var fallbackMapExpr = Mapper.GetMap<Person, PersonDto>(useDefaultMapIfTypeMapIsMissing: true);

// Throw if the map is missing
var requiredMapExpr = Mapper.GetRequiredMap<Person, PersonDto>();
```

### Strict Mode Configuration

By default, strict mode is **enabled** for global maps, meaning `Mapper.Map<S, T>(src)` throws if no map is registered. You can disable this to allow automatic fallback to default maps globally, though explicit registration is recommended for performance and control.

```csharp
// Allow implicit generation of default maps if no custom map is found
Mapper.UseDefaultMapIfTypeMapIsMissing(true);
```

## Contributing 🤝

Contributions are welcome! Please feel free to submit a Pull Request.

## License 📄

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
