# Mapify.NET

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

`UseMap` also supports arrays and enumerable types. If a map exists for element types (`TSrc -> TDest`),
you can use it for collection shapes like `TSrc[] -> TDest[]` and `IEnumerable<TSrc> -> IEnumerable<TDest>`.

### 3. Use the instance mapper in-memory

```csharp
var mapper = provider.GetRequiredService<IMapify>();

var dto = mapper.Map<Person, PersonDto>(person);

var existing = new PersonDto();
mapper.Map(person, existing);
```

### 4. Use the instance mapper with Entity Framework (`IQueryable`)

`IMapify.GetMap<TSource, TTarget>()` returns the expression for projections.

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
