# Mapify

Mapify provides utils to create static mapping expressions for c# objects.
It builds mapping expressions using initializers and implicit mappings for properties with the same name and compatible types.
If combined with [LINQKit](https://github.com/scottksmith95/LINQKit), they can be used to map objects with entity framework before they are loaded in-memory.

## Create a mapping

Mapping expressions can be created with the ``Mapper.CreateMap<TSrc, TDest>()`` method.
This method takes an optional lambda expression with a constructor and initializers.


```csharp
public static PersonMappings {

    public static readonly Expression<Func<Person, PersonDto>> PersonToPersonDto =
        Mapper.CreateMap<Person, PersonDto>(person => new PersonDto {
            Name = $"{person.FirstName} {person.LastName}"
        });
}
```

## Implicit mappings

Properties in the destination type which have the same name as a property in the source type are mapped with the following rules:

* The destination type must be assignable from the source type
* If the source type is nullable, the underlying type must be assignable to the destination type
* The default value of an underlying type is used as fallback if the source value is null and the destination type is not nullable
* Coalesce operators (??) in the initializer are converted to ternary operators by the mapper.

Thus, creating a mapping can be as simple as the following, if all types are compatible

```csharp
public static PersonMappings {

    public static readonly Expression<Func<Person, PersonDto>> PersonToPersonDto =
        Mapper.CreateMap<Person, PersonDto>();
}
```

## Using a mapping in-memory

To use a static mapping expression, use the ``Invoke`` method of Linqkit

```csharp
public PersonDto GetPersonDto(long id) {
    var person = LoadPerson(id); // e.g. from DB

    // There are two extension methods that can be used to map in-memory.
    // 1. Map to a new Object
    var personDto = PersonMappings.PersonToPersonDto.Map(person);

    // 2. Map to an existing Object
    // This will build an action with assignment expressions for each initializer binding.
    // Then it will call the action with the given objects.
    // The action will be compiled on the first call and then cached in a static dictionary afterwards.
    var personDto = new PersonDto();
    PersonMappings.PersonToPersonDto.Map(person, personDto);
}
```

## Using a mapping with EntityFramework 

With Linqkit it is possible to use static expressions in Queryable Select calls.
Thus, it is possible to map Objects in the database and even perform paging or sorting after mapping

```csharp
public IEnumerable<PersonDto> GetPersonDtos(int skip, int take) {
    return _dbContext.Persons
        // see Linqkit (this allows the use of static expressions)
        .AsExpandable() 
        .Select(PersonMappings.PersonToPersonDto)
        .OrderBy(x => x.Name) // order by PersonDto.Name
        .Skip(skip)
        .Take(take)
        .ToArrayAsync();
}
```

## Static Map methods & Default Mappings

There are static methods, which allow to configure mappings that are stored in a static dictionary

```csharp
// Allows to configure globally if default mappings are used for missing type mappings
Mapper.UseDefaultMapIfTypeMapIsMissing(true / false);
// Adds a new mapping for person to PersonDto
Mapper.AddMap<Person, PersonDto>(Mapper.CreateMap<Person, PersonDto>(p => new PersonDto { ... }));
// Or use the shortcut that calls AddMap and CreateMap internally
Mapper.CreateAndAddMap<Person, PersonDto>(p => new PersonDto { ... });
// Gets the mapping Expression for Person to PersonDto
Mapper.GetMap<Person, PersonDto>();
// Uses the default map created with Mapper.CreateMap if no mapping exists
Mapper.GetMap<Person, PersonDto>(useDefaultMapIfTypeMapIsMissing: true);
```

These mappings can be used by static Map methods

```csharp
var person = new Person();
// use a previously configured mapping for Person -> PersonDto
Mapper.Map<Person, PersonDto>(person);
// or use the default if there is no mapping.
// the default mapping is also used if UseDefaultMapIfTypeMapIsMissing is set to true beforehand
Mapper.Map<Person, PersonDto>(person, useDefaultMapIfTypeMapIsMissing: true);

var dto = new PersonDto();
// It is also possible to map to existing objects
Mapper.Map(person, dto);
// and to use a default mapping in this case
Mapper.Map(person, dto, useDefaultMapIfTypeMapIsMissing: true);

```

## How it Works

The ``CreateMap`` Method performs the following steps:

* Build a list of initializer bindings from the partial mappings
    * Replace x ?? y with x != null ? x : y
* For all suitable properties with matching types
    * Create a binding in the form Prop = x.Prop
    * If one of the types is nullable, cast it or use the default value as fallback where necessary


This allows to create mappings like

```csharp
public static PersonMappings {

    public static readonly Expression<Func<Person, PersonDto>> PersonToPersonDto =
        Mapper.CreateMap<Person, PersonDto>(person => {
            FullName = $"{person.Name} {person.LastName}",
            Address = AddressToAddressDto.Invoke(person.Address)
        });

    public static readonly Expression<Func<Address, AddressDto>> AddressToAddressDto =
        Mapper.CreateMap<Address, AddressDto>();
}
```

instead of

```csharp
public static PersonMappings {

    public static readonly Expression<Func<Person, PersonDto>> PersonToPersonDto = person => {
        Name = person.Name,
        LastName = person.LastName,
        FullName = $"{person.Name} {person.LastName}",
        BirthDate = person.BirthDate,
        Address = AddressToAddressDto.Invoke(person.Address)
    });
    
    public static readonly Expression<Func<Address, AddressDto>> AddressToAddressDto = address => new AddressDto {
        Street = address.StreetNumber
        StreetNumber = address.StreetNumber,
        Zip = address.Zip,
        City = address.City
    };
}
```

## Limitations

### Caching

For performance reasons, Maps are cached in static dictionaries after their first usage.
This means if ``GetMap<TSource, TTarget>(true)`` is used before any map is added, or if the global setting to use default maps for missing mapping configurations is enabled,
then the default map is cached and no other mapping can be used.

**Wrong**:
```csharp
// this will cache the default map for Person -> PersonDto
var x = Mapper.Map<Person, PersonDto>(person, true);

// this might work if no map was added before, but it will not be used anmyore
// since GetMap<Person, PersonDto> already cached the default map
Mapper.CreateAndAddMap<Person, PersonDto>(x => new PersonDto { ... });
```

**Right**:
```csharp

// this might work if no map was added before, but it will not be used anmyore
// since GetMap<Person, PersonDto> already cached the default map
Mapper.CreateAndAddMap<Person, PersonDto>(x => new PersonDto { ... });

// this will cache the compiled map for Person -> PersonDto which was added before
var x = Mapper.Map<Person, PersonDto>(person, true);
```

The map extensions on expressions will only cache the compiled map of that expression though.

**Right**:
```csharp

// this might work if no map was added before, but it will not be used anmyore
// since GetMap<Person, PersonDto> already cached the default map
public static Expression<Func<Person, PersonDto>> PersonMap = Mapper.CreateMap<Person, PersonDto>(x => new PersonDto { ... });

...

// this will cache the compiled map for Person -> PersonDto which was added before
var x = PersonMap.Map(person);

```

However, do not use inline expressions, as they would 


* Inheritance
* Sorting with inheritance
* etc (TBD)