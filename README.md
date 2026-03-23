# Mapify.NET

[![Tests](https://github.com/BenjaminVettori/Mapify.NET/actions/workflows/tests.yml/badge.svg?branch=main)](https://github.com/BenjaminVettori/Mapify.NET/actions/workflows/tests.yml)
[![Build](https://github.com/BenjaminVettori/Mapify.NET/actions/workflows/build.yml/badge.svg?branch=main)](https://github.com/BenjaminVettori/Mapify.NET/actions/workflows/build.yml)

Mapify is a lightweight .NET library for creating mapping expressions for C# objects. It bridges the gap between in-memory object mapping and LINQ projections (e.g., Entity Framework), allowing you to reuse the same mapping logic consistently across your application.

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
- If an ignored destination property is marked `required`:
    - Mapify preserves an existing class/constructor initializer when present.
    - Otherwise, Mapify emits the configured fallback (`default(T)` for non-collections, empty fallback for non-nullable collections).
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

### Recursive `UseMap` depth

For self-referencing or cyclic object graphs, Mapify expands recursive `UseMap` calls to a finite depth.

- If no depth is specified, the default recursion depth is `6`.
- You can override depth per marker with:
    - `UseMap<TSource, TTarget>(source, maxDepth)`
    - `UseMap<TSource, TTarget>("Name", source, maxDepth)`
- `maxDepth` must be a constant positive integer in the expression.

Mapify also enforces a hard safety cap during map build:

- Default hard cap: `10`
- Configure per mapper instance: `mapify.UseMaxRecursiveMapBuildDepth(value)`
- If any marker depth exceeds the hard cap, map building throws `InvalidOperationException`.

`ProjectTo<TTarget>()` markers inside `CreateMap` expressions are rewritten to the same internal behavior, so the same recursive depth rules apply.

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

`IMapify.GetMap<TSource, TTarget>()` returns the expression for projections (or `null` when missing). Use `IMapify.GetRequiredMap<TSource, TTarget>()` when a missing map should throw.

To project with EF/EF Core using instance-based profiles, pass the mapper to `ProjectTo<TTarget>()`:

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

`ProjectTo` uses the same runtime fallback behavior as `Map`, including:

- collection-shape fallback (for example `IEnumerable<TSrc>` map reused for `List<TSrc>`)
- collection-to-single-object fallback (for example `IEnumerable<TSrc> -> Dto` reused for `List<TSrc> -> Dto`)
- collection-to-collection materialization fallback (for example `IEnumerable<TSrc> -> IEnumerable<TDest>` reused for `List<TSrc> -> IReadOnlyCollection<TDest>`)
- nested collection fallback at multiple depths when only element maps exist (for example `List<List<List<TSrc>>> -> List<List<List<TDest>>>` via `TSrc -> TDest`)

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

### 5. Runtime Parameters (instance mapper)

Runtime parameters are supported by the **instance mapper** (`IMapify`) and work in both:

- in-memory mapping (`Map(...)`)
- queryable projection (`ProjectTo(..., mapify, parameters)`)

Use `Parameter<T>(string name)` inside a profile map:

```csharp
public class MyProfile : MapifyProfile {
    protected override void Configure() {
        CreateMap<Request, Result>(r => new Result {
            ScoreCategory = r.Score >= Parameter<int>("minScore") ? "Pass" : "Fail"
        });
    }
}
```

Provide parameter values at execution time via `IMapify`:

- `GetMap<TSource, TTarget>(parameters)` / `GetRequiredMap<TSource, TTarget>(parameters)`
- `Map<TSource, TTarget>(source, parameters)`
- `Map<TSource, TTarget>(source, target, parameters)`
- `Map<TSource, TTarget>(source, name, parameters)`
- `Map<TSource, TTarget>(source, target, name, parameters)`
- `ProjectTo<TTarget>(mapify, parameters)` and named overloads

Example:

```csharp
var parameters = new Dictionary<string, object?> { ["minScore"] = 50 };

var expr = mapify.GetRequiredMap<Request, Result>(parameters);
var resultFromExpr = expr.Compile().Invoke(request);

var result = mapify.Map<Request, Result>(request, parameters);

var existing = new Result();
mapify.Map(request, existing, parameters);

var projected = dbContext.Requests
    .ProjectTo<Result>(mapify, parameters)
    .ToArray();
```

Notes: parameter names are matched by key and values must be convertible to the `T` used in `Parameter<T>(name)`.

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

### Mapping Type Resolution Precedence

Mapify uses the same runtime type-resolution rules for:

- `Map(source)` / `Map(source, target)`
- top-level `ProjectTo<TTarget>(...)`
- nested map resolution inside `CreateMap` (`UseMap` and `ProjectTo` markers)

When Mapify resolves a map candidate, it follows this precedence:

1. **Exact source/target type match**
2. **Nullable underlying-type fallback**
3. **Assignable collection-shape fallback** (source side, destination side, or both)
4. **Collection element-type fallback** (only when both sides are collection shapes)

#### 1) Exact map wins

If both are registered, exact type map is preferred.

```csharp
CreateMap<NumberSource, NumberDto>(x => new NumberDto { Value = x.Value + 1 });
CreateMap<NumberSource?, NumberDto?>(x => x == null ? null : new NumberDto { Value = x.Value.Value + 100 });

CreateMap<Order, OrderDto>(); // Order.Number: NumberSource? -> OrderDto.Number: NumberDto?
```

`Order.Number` uses `NumberSource? -> NumberDto?` (exact), not the non-nullable fallback map.

#### 2) Nullable fallback when exact nullable map is missing

If only `NumberSource -> NumberDto` exists, Mapify can lift/adapt it for nullable variants where compatible.

```csharp
CreateMap<NumberSource, NumberDto>(x => new NumberDto { Value = x.Value + 1 });
CreateMap<Order, OrderDto>(); // NumberSource? -> NumberDto?
```

Mapify uses the non-nullable map and injects null-safe adaptation.

#### 3) Collection map precedence

For collection-related resolution, Mapify resolves in this order:

1. exact collection pair (`SrcShape -> DstShape`)
2. assignable collection-shape candidates, using this hierarchy:
    1. `IList<T>`
    2. `ICollection<T>`
    3. `IReadOnlyList<T>`
    4. `IReadOnlyCollection<T>`
    5. `IEnumerable<T>`
3. element map fallback (`SrcElement -> DstElement`) when both sides are collections

Recommendation: when you want a single collection map to work across most concrete collection shapes, register it with `IEnumerable<T>` (for example `IEnumerable<TSrc> -> IEnumerable<TDest>` or `IEnumerable<TSrc> -> Dto`). This map can then be reused for sources like arrays, `List<T>`, `ICollection<T>`, and `IReadOnlyCollection<T>` via assignable collection-shape fallback. If a specific collection shape needs different behavior, register that specific map as well—exact matches and higher-priority shape candidates still win.

```csharp
CreateMap<Item, ItemDto>(x => new ItemDto { Value = x.Value + 1 });
CreateMap<List<Item>, List<ItemDto>>(x => x.Select(i => new ItemDto { Value = i.Value + 100 }).ToList());

CreateMap<Batch, BatchDto>(); // List<Item> -> List<ItemDto>
```

`Batch.Items` uses `List<Item> -> List<ItemDto>` (exact collection map).
If that map is removed but an `IEnumerable<Item> -> IEnumerable<ItemDto>` map exists, that map is used and materialized to the destination collection type.
If neither collection map exists, Mapify falls back to `Item -> ItemDto` per element.

Collection-to-single-object fallback is also supported. If only
`IEnumerable<Item> -> SummaryDto` is registered, Mapify can resolve
`List<Item> -> SummaryDto`, `ICollection<Item> -> SummaryDto`, etc.

Likewise, if only `IEnumerable<Item> -> IEnumerable<ItemDto>` is registered,
Mapify can resolve mixed collection shape combinations such as
`List<Item> -> IReadOnlyCollection<ItemDto>`.

For nullable collection members, Mapify preserves `null` safely for supported query/provider shapes.

When multiple assignable collection candidates match, Mapify picks the highest-ranked candidate from the hierarchy above.

#### 4) Collection fallback + initializer preservation

When Mapify needs a fallback value for a collection member (for example source is `null`, or destination member has no matching source member), it applies these rules:

1. If the destination member is already initialized in the class/constructor, that user-defined value is preserved.
2. Otherwise, if the destination collection is non-nullable (or marked `required`), Mapify uses an empty collection/array fallback where possible.
3. Otherwise (nullable destination collection), Mapify uses `null`.

For non-collection members, fallback remains `default(T)`.

In short:

- source member exists -> map from source value
- source member missing -> keep user initializer when present, otherwise fallback by nullability/required rules
- source value is `null` + non-nullable collection destination -> empty collection fallback

If a custom collection type implements multiple different `IEnumerable<T>` element types, Mapify throws a descriptive configuration error because element type inference would be ambiguous.

### `GetMap` / `GetRequiredMap` exact-type rule

`GetMap<TSource, TTarget>()` and `GetRequiredMap<TSource, TTarget>()` always resolve by the **exact requested pair**.
They do **not** return a different pair's map (for example, element maps or nullable underlying-type maps).

Examples:

- If only `Item -> ItemDto` is registered, `GetMap<List<Item>, List<ItemDto>>()` is `null` (unless default-map fallback is enabled, in which case Mapify creates a new exact `List<Item> -> List<ItemDto>` expression).
- If only `NumberSource -> NumberDto` is registered, `GetMap<NumberSource?, NumberDto?>()` is `null` (same default fallback behavior applies).

This keeps `GetMap`/`GetRequiredMap` type-safe: the returned expression always matches the exact generic types requested.

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

### Mapping to existing instances

`Map(source, target)` updates a provided target instance in-place only when the selected map expression is an object initializer (`MemberInit`).

```csharp
CreateMap<Person, PersonDto>(p => new PersonDto { Name = p.FirstName });

// instance mapper
var dto1 = new PersonDto();
mapper.Map(person, dto1);

```

Implementation note: Mapify converts initializer bindings into assignments (for example `target.Name = ...`) and compiles them as `Action<TSource, TTarget>`. If the map returns a value directly (for example `CreateMap<User, string>(u => "User" + u.Id)`), `Map(source, target)` throws `NotSupportedException`.

For value mappings, use `Map(source)` and assign the returned value.

### Value Mappings (non-initializer)

Mapify also supports mappings where the expression returns a value directly (not `new TTarget { ... }`).

```csharp
CreateMap<Person, string>(x => x.FirstName);
CreateMap<SourceStatus, TargetStatus>(x => x == SourceStatus.Active ? TargetStatus.Enabled : TargetStatus.Disabled);

var name = mapify.Map<Person, string>(person);
var status = mapify.Map<SourceStatus, TargetStatus>(SourceStatus.Active);
```

> Note: value mappings are supported for `Map(source)` only. `Map(source, target)` requires an object-initializer mapping.

### Explicit Overrides & Coalescing

You can provide a partial initializer to override specific properties. Mapify also rewrites null-coalescing operators (`??`) to conditional expressions (`x != null ? x : y`) to ensure compatibility with all LINQ providers (some EF versions struggle with `??`).

```csharp
CreateMap<Person, PersonDto>(p => new PersonDto {
    // Explicit override
    Name = p.FirstName + " " + p.LastName,
    
    // Coalescing is rewritten for EF compatibility
    Region = p.Region ?? "Unknown" 
});
```

## Caching & Performance ⚡

*   **Compiled Delegates**: Accessors are compiled to delegates.
*   **Strategy**:
    *   **Mapify instance**: Caches compiled delegates per mapper instance.
    *   **Expression extension (`expression.Map(...)`)**: Optional per-expression delegate caching.

## Advanced Usage 🛠️

For advanced scenarios, Mapify exposes several lower-level methods.

### Retrieving Maps

You can retrieve registered maps using `GetMap`. This is useful in generic or dynamic contexts.
`GetMap` returns `null` if no map exists and default-map fallback is disabled.
If you want throwing behavior, use `GetRequiredMap`.

```csharp
// Retrieve a registered map (or null)
var mapExpr = mapify.GetMap<Person, PersonDto>();

// Enable per-instance fallback (if desired)
mapify.UseDefaultMapIfTypeMapIsMissing(true);

// Then GetMap/GetRequiredMap can use generated default maps when missing
var fallbackMapExpr = mapify.GetMap<Person, PersonDto>();

// Throw if the map is missing
var requiredMapExpr = mapify.GetRequiredMap<Person, PersonDto>();
```

### Strict Mode Configuration

By default, strict mode is **enabled**, meaning `mapify.Map<S, T>(src)` throws if no map is registered. You can disable this per mapper instance to allow automatic fallback to default maps, though explicit registration is recommended for performance and control.

```csharp
// Allow implicit generation of default maps if no custom map is found
mapify.UseDefaultMapIfTypeMapIsMissing(true);
```

## Contributing 🤝

Contributions are welcome! Please feel free to submit a Pull Request.

## License 📄

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
