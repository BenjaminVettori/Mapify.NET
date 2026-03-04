using System.Data.Common;
using System.Data.Entity;
using LinqKit;
using Microsoft.Extensions.DependencyInjection;

namespace Mapify.NET.Tests.NetFx;

public class MapifyNetFrameworkEf6Tests {
    [Fact]
    public void IgnoreMarker_ShouldExcludeIgnoredPropertyFromEf6SqlProjection() {
        using var connection = Effort.DbConnectionFactory.CreateTransient();
        using var db = new Ef6MapifyContext(connection);
        db.Database.CreateIfNotExists();

        db.Set<Ef6ProjectionIgnoreEntity>().Add(new Ef6ProjectionIgnoreEntity {
            Included = "included",
            IgnoredFromDb = "ignored-db"
        });
        db.SaveChanges();

        var mapify = new Mapify([
            new Ef6ProjectionIgnoreProfile()
        ]);

        var mapExpr = mapify.GetRequiredMap<Ef6ProjectionIgnoreEntity, Ef6ProjectionIgnoreDto>();
        var query = db.Set<Ef6ProjectionIgnoreEntity>().Select(mapExpr);
        var queryText = query.ToString();

        Assert.DoesNotContain("IgnoredFromDb", queryText, StringComparison.OrdinalIgnoreCase);

        var projected = query.Single();
        Assert.Equal("included", projected.Included);
        Assert.Null(projected.IgnoredFromDb);
    }

    [Fact]
    public void IgnoreMarker_ShouldExcludeIgnoredPropertyFromEf6SqlProjection_WhenUsingProjectTo() {
        using var connection = Effort.DbConnectionFactory.CreateTransient();
        using var db = new Ef6MapifyContext(connection);
        db.Database.CreateIfNotExists();

        db.Set<Ef6ProjectionIgnoreEntity>().Add(new Ef6ProjectionIgnoreEntity {
            Included = "included",
            IgnoredFromDb = "ignored-db"
        });
        db.SaveChanges();

        var mapify = new Mapify([
            new Ef6ProjectionIgnoreProfile()
        ]);

        var query = db.Set<Ef6ProjectionIgnoreEntity>().ProjectTo<Ef6ProjectionIgnoreDto>(mapify);
        var queryText = query.ToString();

        Assert.DoesNotContain("IgnoredFromDb", queryText, StringComparison.OrdinalIgnoreCase);

        var projected = query.Single();
        Assert.Equal("included", projected.Included);
        Assert.Null(projected.IgnoredFromDb);
    }

    [Fact]
    public void IgnoreMarker_ShouldExcludeIgnoredPropertyFromEf6SqlProjection_WhenUsingSelect() {
        using var connection = Effort.DbConnectionFactory.CreateTransient();
        using var db = new Ef6MapifyContext(connection);
        db.Database.CreateIfNotExists();

        db.Set<Ef6ProjectionIgnoreEntity>().Add(new Ef6ProjectionIgnoreEntity {
            Included = "included",
            IgnoredFromDb = "ignored-db"
        });
        db.SaveChanges();

        var mapify = new Mapify([
            new Ef6ProjectionIgnoreProfile()
        ]);

        var mapExpr = mapify.GetRequiredMap<Ef6ProjectionIgnoreEntity, Ef6ProjectionIgnoreDto>();
        var query = db.Set<Ef6ProjectionIgnoreEntity>().Select(mapExpr);
        var queryText = query.ToString();

        Assert.DoesNotContain("IgnoredFromDb", queryText, StringComparison.OrdinalIgnoreCase);

        var projected = query.Single();
        Assert.Equal("included", projected.Included);
        Assert.Null(projected.IgnoredFromDb);
    }

    [Fact]
    public void ServiceCollectionExtensions_ShouldRegisterMapperInNetFrameworkProject() {
        var services = new ServiceCollection();
        services.AddMapifyProfiles(typeof(Ef6DiProfile).Assembly);
        services.AddMapify();

        using var provider = services.BuildServiceProvider();
        var mapify = provider.GetRequiredService<IMapify>();

        var mapped = mapify.Map<Ef6DiSource, Ef6DiTarget>(new Ef6DiSource { Value = 5 });

        Assert.Equal(5, mapped.Value);
    }

    [Fact]
    public void CreateMap_ShouldWorkInEf6Projection_WithNestedInvokeForSingleAndCollection() {
        using var connection = Effort.DbConnectionFactory.CreateTransient();
        using var db = new Ef6MapifyContext(connection);
        db.Database.CreateIfNotExists();

        var adaAddress = new Ef6Address { City = "London" };
        var alanAddress = new Ef6Address { City = "Manchester" };

        db.People.Add(new Ef6Person {
            FirstName = "Ada",
            LastName = "Lovelace",
            HomeAddress = adaAddress,
            Phones = [
                new Ef6Phone { Number = "+44-100" },
                    new Ef6Phone { Number = "+44-101" }
            ]
        });

        db.People.Add(new Ef6Person {
            FirstName = "Alan",
            LastName = "Turing",
            HomeAddress = alanAddress,
            Phones = [
                new Ef6Phone { Number = "+44-200" }
            ]
        });

        db.SaveChanges();

        var addressMap = Mapper.CreateMap<Ef6Address, Ef6AddressDto>();
        var phoneMap = Mapper.CreateMap<Ef6Phone, Ef6PhoneDto>();

        var map = Mapper.CreateMap<Ef6Person, Ef6PersonDto>(x => new Ef6PersonDto {
            FullName = x.FirstName + " " + x.LastName,
            HomeAddress = addressMap.Invoke(x.HomeAddress),
            Phones = x.Phones.Select(p => phoneMap.Invoke(p)).ToList()
        });

        var result = db.People
            .AsExpandable()
            .OrderBy(x => x.Id)
            .Select(map)
            .ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal("Ada Lovelace", result[0].FullName);
        Assert.Equal("London", result[0].HomeAddress.City);
        Assert.Equal(2, result[0].Phones.Count);
        Assert.Equal("Alan Turing", result[1].FullName);
        Assert.Equal("Manchester", result[1].HomeAddress.City);
        Assert.Single(result[1].Phones);
    }

    [Fact]
    public void InstanceMapify_UseMap_ShouldWorkInEf6Projection_ForEnumerableCollections() {
        using var connection = Effort.DbConnectionFactory.CreateTransient();
        using var db = new Ef6MapifyContext(connection);
        db.Database.CreateIfNotExists();

        db.People.Add(new Ef6Person {
            FirstName = "Ada",
            LastName = "Lovelace",
            HomeAddress = new Ef6Address { City = "London" },
            Phones = [
                new Ef6Phone { Number = "+44-100" },
                    new Ef6Phone { Number = "+44-101" }
            ]
        });

        db.People.Add(new Ef6Person {
            FirstName = "Alan",
            LastName = "Turing",
            HomeAddress = new Ef6Address { City = "Manchester" },
            Phones = [
                new Ef6Phone { Number = "+44-200" }
            ]
        });

        db.SaveChanges();

        var mapify = new Mapify([
            new Ef6PhoneProfile(),
                new Ef6PersonCollectionsProfile()
        ]);

        var mapExpr = mapify.GetRequiredMap<Ef6Person, Ef6PersonCollectionsDto>();

        var result = db.People
            .OrderBy(x => x.Id)
            .Select(mapExpr)
            .ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal("Ada Lovelace", result[0].FullName);
        Assert.Equal(["+44-100", "+44-101"], result[0].PhonesList.Select(x => x.Number).ToArray());
        Assert.Equal(["+44-100", "+44-101"], result[0].PhonesEnumerable.Select(x => x.Number).ToArray());

        Assert.Equal("Alan Turing", result[1].FullName);
        Assert.Equal(["+44-200"], result[1].PhonesList.Select(x => x.Number).ToArray());
        Assert.Equal(["+44-200"], result[1].PhonesEnumerable.Select(x => x.Number).ToArray());
    }

    [Fact]
    public void InstanceMapify_UseMap_ShouldThrowForArrayProjection_InEf6Projection() {
        using var connection = Effort.DbConnectionFactory.CreateTransient();
        using var db = new Ef6MapifyContext(connection);
        db.Database.CreateIfNotExists();

        db.People.Add(new Ef6Person {
            FirstName = "Ada",
            LastName = "Lovelace",
            HomeAddress = new Ef6Address { City = "London" },
            Phones = [
                new Ef6Phone { Number = "+44-100" },
                    new Ef6Phone { Number = "+44-101" }
            ]
        });

        db.People.Add(new Ef6Person {
            FirstName = "Alan",
            LastName = "Turing",
            HomeAddress = new Ef6Address { City = "Manchester" },
            Phones = [
                new Ef6Phone { Number = "+44-200" }
            ]
        });

        db.SaveChanges();

        var mapify = new Mapify([
            new Ef6PhoneProfile(),
                new Ef6PersonArrayCollectionsProfile()
        ]);

        var mapExpr = mapify.GetMap<Ef6Person, Ef6PersonArrayCollectionsDto>();

        // EF6 cannot translate array materialization in projection pipelines.
        Assert.Throws<NotSupportedException>(() => db.People
            .OrderBy(x => x.Id)
            .Select(mapExpr)
            .ToList());
    }

    [Fact]
    public void CreateMap_ShouldImplicitlyMapPrimitiveEnumerableCollections_InEf6Projection() {
        using var connection = Effort.DbConnectionFactory.CreateTransient();
        using var db = new Ef6MapifyContext(connection);
        db.Database.CreateIfNotExists();

        db.People.Add(new Ef6Person {
            FirstName = "Ada",
            LastName = "Lovelace",
            HomeAddress = new Ef6Address { City = "London" },
            Phones = [
                new Ef6Phone { Number = "+44-100" },
                    new Ef6Phone { Number = "+44-101" }
            ]
        });

        db.People.Add(new Ef6Person {
            FirstName = "Alan",
            LastName = "Turing",
            HomeAddress = new Ef6Address { City = "Manchester" },
            Phones = [
                new Ef6Phone { Number = "+44-200" }
            ]
        });

        db.SaveChanges();

        var map = Mapper.CreateMap<Ef6PrimitiveCollectionsSource, Ef6PrimitiveCollectionsDto>();

        var result = db.People
            .OrderBy(x => x.Id)
            .Select(x => new Ef6PrimitiveCollectionsSource {
                Numbers = x.Phones.OrderBy(p => p.Id).Select(p => p.Id).ToList(),
                Texts = x.Phones.OrderBy(p => p.Id).Select(p => p.Number).ToList()
            })
            .Select(map)
            .ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(new[] { 1, 2 }, result[0].Numbers);
        Assert.Equal(["+44-100", "+44-101"], result[0].Texts);
        Assert.Equal(new[] { 3 }, result[1].Numbers);
        Assert.Equal(["+44-200"], result[1].Texts);
    }

    [Fact]
    public void CreateMap_ShouldThrowForPrimitiveArrayProjection_InEf6Projection() {
        using var connection = Effort.DbConnectionFactory.CreateTransient();
        using var db = new Ef6MapifyContext(connection);
        db.Database.CreateIfNotExists();

        db.People.Add(new Ef6Person {
            FirstName = "Ada",
            LastName = "Lovelace",
            HomeAddress = new Ef6Address { City = "London" },
            Phones = [
                new Ef6Phone { Number = "+44-100" },
                    new Ef6Phone { Number = "+44-101" }
            ]
        });

        db.People.Add(new Ef6Person {
            FirstName = "Alan",
            LastName = "Turing",
            HomeAddress = new Ef6Address { City = "Manchester" },
            Phones = [
                new Ef6Phone { Number = "+44-200" }
            ]
        });

        db.SaveChanges();

        var map = Mapper.CreateMap<Ef6PrimitiveArrayCollectionsSource, Ef6PrimitiveArrayCollectionsDto>();

        // EF6 cannot translate source-side ToArray() inside LINQ-to-Entities projections.
        Assert.Throws<NotSupportedException>(() => db.People
            .OrderBy(x => x.Id)
            .Select(x => new Ef6PrimitiveArrayCollectionsSource {
                Numbers = x.Phones.OrderBy(p => p.Id).Select(p => p.Id).ToArray(),
                Texts = x.Phones.OrderBy(p => p.Id).Select(p => p.Number).ToList()
            })
            .Select(map)
            .ToList());
    }

    [Fact]
    public void CreateMap_ShouldUseNullOrEmptyCollectionFallback_ForNullableAndRequiredTargets_InEf6Project() {
        var nullableMap = Mapper.CreateMap<Ef6NullCollectionSource, Ef6NullableCollectionTarget>();
        var requiredMap = Mapper.CreateMap<Ef6NullCollectionSource, Ef6NonNullableCollectionTarget>();

        var source = new Ef6NullCollectionSource {
            Numbers = null
        };

        var nullableResult = nullableMap.Map(source);
        var requiredResult = requiredMap.Map(source);

        Assert.Null(nullableResult.Numbers);
        Assert.NotNull(requiredResult.Numbers);
        Assert.Empty(requiredResult.Numbers);
    }

    [Fact]
    public void InstanceMapify_ShouldImplicitlyUseExistingMapsForNestedAndCollectionMembers_InEf6Projection() {
        using var connection = Effort.DbConnectionFactory.CreateTransient();
        using var db = new Ef6MapifyContext(connection);
        db.Database.CreateIfNotExists();

        db.People.Add(new Ef6Person {
            FirstName = "Ada",
            LastName = "Lovelace",
            HomeAddress = new Ef6Address { City = "London" },
            Phones = [
                new Ef6Phone { Number = "+44-100" },
                    new Ef6Phone { Number = "+44-101" }
            ]
        });

        db.People.Add(new Ef6Person {
            FirstName = "Alan",
            LastName = "Turing",
            HomeAddress = new Ef6Address { City = "Manchester" },
            Phones = [
                new Ef6Phone { Number = "+44-200" }
            ]
        });

        db.SaveChanges();

        var mapify = new Mapify([
            new Ef6AddressProfile(),
                new Ef6PhoneProfile(),
                new Ef6PersonImplicitNestedAndCollectionsProfile()
        ]);

        var mapExpr = mapify.GetRequiredMap<Ef6Person, Ef6PersonImplicitNestedAndCollectionsDto>();

        var result = db.People
            .OrderBy(x => x.Id)
            .Select(mapExpr)
            .ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal("Ada Lovelace", result[0].FullName);
        Assert.Equal("London", result[0].HomeAddress.City);
        Assert.Equal(["+44-100", "+44-101"], result[0].Phones.Select(x => x.Number).ToArray());

        Assert.Equal("Alan Turing", result[1].FullName);
        Assert.Equal("Manchester", result[1].HomeAddress.City);
        Assert.Equal(["+44-200"], result[1].Phones.Select(x => x.Number).ToArray());
    }

    [Fact]
    public void InstanceMapify_ShouldThrowForImplicitNestedArrayProjection_InEf6Projection() {
        using var connection = Effort.DbConnectionFactory.CreateTransient();
        using var db = new Ef6MapifyContext(connection);
        db.Database.CreateIfNotExists();

        db.People.Add(new Ef6Person {
            FirstName = "Ada",
            LastName = "Lovelace",
            HomeAddress = new Ef6Address { City = "London" },
            Phones = [
                new Ef6Phone { Number = "+44-100" },
                    new Ef6Phone { Number = "+44-101" }
            ]
        });

        db.People.Add(new Ef6Person {
            FirstName = "Alan",
            LastName = "Turing",
            HomeAddress = new Ef6Address { City = "Manchester" },
            Phones = [
                new Ef6Phone { Number = "+44-200" }
            ]
        });

        db.SaveChanges();

        var mapify = new Mapify([
            new Ef6AddressProfile(),
                new Ef6PhoneProfile(),
                new Ef6PersonImplicitNestedAndArrayProfile()
        ]);

        var mapExpr = mapify.GetMap<Ef6Person, Ef6PersonImplicitNestedAndArrayDto>();

        // EF6 cannot translate implicit nested mapping when target member is an array.
        Assert.Throws<NotSupportedException>(() => db.People
            .OrderBy(x => x.Id)
            .Select(mapExpr)
            .ToList());
    }

    [Fact]
    public void InstanceMapify_UseMap_ShouldAcceptFilterExpressions_InEf6Projection() {
        using var connection = Effort.DbConnectionFactory.CreateTransient();
        using var db = new Ef6MapifyContext(connection);
        db.Database.CreateIfNotExists();

        db.People.Add(new Ef6Person {
            FirstName = "Ada",
            LastName = "Lovelace",
            HomeAddress = new Ef6Address { City = "London" },
            Phones = [
                new Ef6Phone { Number = "+44-100" },
                    new Ef6Phone { Number = "+44-101" },
                    new Ef6Phone { Number = "+1-999" }
            ]
        });

        db.People.Add(new Ef6Person {
            FirstName = "Alan",
            LastName = "Turing",
            HomeAddress = new Ef6Address { City = "Manchester" },
            Phones = [
                new Ef6Phone { Number = "+44-200" },
                    new Ef6Phone { Number = "+1-123" }
            ]
        });

        db.SaveChanges();

        var mapify = new Mapify([
            new Ef6PhoneProfile(),
                new Ef6PersonFilteredPhonesProfile()
        ]);

        var mapExpr = mapify.GetRequiredMap<Ef6Person, Ef6PersonFilteredPhonesDto>();

        var result = db.People
            .OrderBy(x => x.Id)
            .Select(mapExpr)
            .ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(["+44-100", "+44-101"], result[0].Students.Select(x => x.Number).ToArray());
        Assert.Equal(["+44-200"], result[1].Students.Select(x => x.Number).ToArray());
    }

    [Fact]
    public void InstanceMapify_UseMap_ShouldSupportNamedMappings_InEf6Projection() {
        using var connection = Effort.DbConnectionFactory.CreateTransient();
        using var db = new Ef6MapifyContext(connection);
        db.Database.CreateIfNotExists();

        db.People.Add(new Ef6Person {
            FirstName = "Ada",
            LastName = "Lovelace",
            HomeAddress = new Ef6Address { City = "London" },
            Phones = [
                new Ef6Phone { Number = "+44-100" },
                    new Ef6Phone { Number = "+44-101" }
            ]
        });

        db.SaveChanges();

        var mapify = new Mapify([
            new Ef6NamedPhoneProfile(),
                new Ef6NamedPersonProfile()
        ]);

        var mapExpr = mapify.GetRequiredMap<Ef6Person, Ef6NamedPhonesDto>();

        var result = db.People
            .OrderBy(x => x.Id)
            .Select(mapExpr)
            .Single();

        Assert.Equal(["+44-100", "+44-101"], result.PhonesRaw.Select(x => x.Number).ToArray());
        Assert.Equal(["+44-100 [MASKED]", "+44-101 [MASKED]"], result.PhonesMasked.Select(x => x.Number).ToArray());
    }

    [Fact]
    public void InstanceMapify_UseMap_ShouldAllowChaining_InEf6Projection() {
        using var connection = Effort.DbConnectionFactory.CreateTransient();
        using var db = new Ef6MapifyContext(connection);
        db.Database.CreateIfNotExists();

        db.People.Add(new Ef6Person {
            FirstName = "Ada",
            LastName = "Lovelace",
            HomeAddress = new Ef6Address { City = "London" },
            Phones = [
                new Ef6Phone { Number = "+44-300" },
                    new Ef6Phone { Number = "+44-100" },
                    new Ef6Phone { Number = "+44-200" }
            ]
        });

        db.SaveChanges();

        var mapify = new Mapify([
            new Ef6PhoneProfile(),
                new Ef6PersonChainedPhonesProfile()
        ]);

        var mapExpr = mapify.GetRequiredMap<Ef6Person, Ef6PersonChainedPhonesDto>();

        var result = db.People
            .Select(mapExpr)
            .Single();

        Assert.Equal(["+44-100", "+44-200", "+44-300"], result.PhonesOrdered.Select(x => x.Number).ToArray());
    }

    [Fact]
    public void InstanceMapify_UseMapNamed_ShouldAllowChaining_InEf6Projection() {
        using var connection = Effort.DbConnectionFactory.CreateTransient();
        using var db = new Ef6MapifyContext(connection);
        db.Database.CreateIfNotExists();

        db.People.Add(new Ef6Person {
            FirstName = "Ada",
            LastName = "Lovelace",
            HomeAddress = new Ef6Address { City = "London" },
            Phones = [
                new Ef6Phone { Number = "+44-300" },
                    new Ef6Phone { Number = "+44-100" }
            ]
        });

        db.SaveChanges();

        var mapify = new Mapify([
            new Ef6NamedPhoneProfile(),
                new Ef6NamedPersonChainedProfile()
        ]);

        var mapExpr = mapify.GetRequiredMap<Ef6Person, Ef6NamedPersonChainedPhonesDto>();

        var result = db.People
            .Select(mapExpr)
            .Single();

        Assert.Equal(["+44-100 [MASKED]", "+44-300 [MASKED]"], result.PhonesOrdered.Select(x => x.Number).ToArray());
    }

    [Fact]
    public void InstanceMapify_UseMap_ShouldSupportCalculations_InEf6Projection() {
        using var connection = Effort.DbConnectionFactory.CreateTransient();
        using var db = new Ef6MapifyContext(connection);
        db.Database.CreateIfNotExists();

        db.People.Add(new Ef6Person {
            FirstName = "Ada",
            LastName = "Lovelace",
            HomeAddress = new Ef6Address { City = "London" }
        });

        db.People.Add(new Ef6Person {
            FirstName = "Alan",
            LastName = "Turing",
            HomeAddress = new Ef6Address { City = "Manchester" }
        });

        db.SaveChanges();

        var mapify = new Mapify([
            new Ef6IntIdentityProfile(),
                new Ef6PersonCalculationProfile()
        ]);

        var mapExpr = mapify.GetRequiredMap<Ef6Person, Ef6PersonCalculationDto>();

        var result = db.People
            .OrderBy(x => x.Id)
            .Select(mapExpr)
            .ToList();

        Assert.All(result, x => Assert.Equal(x.Id * 365, x.AgeInDays));
    }

    [Fact]
    public void InstanceMapify_ProjectTo_ShouldWorkInEf6Projection() {
        using var connection = Effort.DbConnectionFactory.CreateTransient();
        using var db = new Ef6MapifyContext(connection);
        db.Database.CreateIfNotExists();

        db.People.Add(new Ef6Person {
            FirstName = "Ada",
            LastName = "Lovelace",
            HomeAddress = new Ef6Address { City = "London" },
            Phones = [
                new Ef6Phone { Number = "+44-100" },
                    new Ef6Phone { Number = "+44-101" }
            ]
        });

        db.People.Add(new Ef6Person {
            FirstName = "Alan",
            LastName = "Turing",
            HomeAddress = new Ef6Address { City = "Manchester" },
            Phones = [
                new Ef6Phone { Number = "+44-200" }
            ]
        });

        db.SaveChanges();

        var mapify = new Mapify([
            new Ef6PhoneProfile(),
                new Ef6PersonCollectionsProfile()
        ]);

        var result = db.People
            .OrderBy(x => x.Id)
            .ProjectTo<Ef6PersonCollectionsDto>(mapify)
            .ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal("Ada Lovelace", result[0].FullName);
        Assert.Equal(["+44-100", "+44-101"], result[0].PhonesList.Select(x => x.Number).ToArray());
        Assert.Equal(["+44-100", "+44-101"], result[0].PhonesEnumerable.Select(x => x.Number).ToArray());

        Assert.Equal("Alan Turing", result[1].FullName);
        Assert.Equal(["+44-200"], result[1].PhonesList.Select(x => x.Number).ToArray());
        Assert.Equal(["+44-200"], result[1].PhonesEnumerable.Select(x => x.Number).ToArray());
    }

    [Fact]
    public void InstanceMapify_ProjectToNamed_ShouldWorkInEf6Projection() {
        using var connection = Effort.DbConnectionFactory.CreateTransient();
        using var db = new Ef6MapifyContext(connection);
        db.Database.CreateIfNotExists();

        db.People.Add(new Ef6Person {
            FirstName = "Ada",
            LastName = "Lovelace",
            HomeAddress = new Ef6Address { City = "London" },
            Phones = [
                new Ef6Phone { Number = "+44-100" },
                    new Ef6Phone { Number = "+44-101" }
            ]
        });

        db.SaveChanges();

        var mapify = new Mapify([
            new Ef6NamedPhoneProfile(),
                new Ef6NamedProjectToPersonProfile()
        ]);

        var result = db.People
            .ProjectTo<Ef6ProjectToNamedPhonesDto>(mapify, "Masked")
            .Single();

        Assert.Equal(["+44-100 [MASKED]", "+44-101 [MASKED]"], result.Phones.Select(x => x.Number).ToArray());
    }

    [Fact]
    public void InstanceMapify_NestedNamedProjectToMarker_ShouldWorkInEf6Projection() {
        using var connection = Effort.DbConnectionFactory.CreateTransient();
        using var db = new Ef6MapifyContext(connection);
        db.Database.CreateIfNotExists();

        db.People.Add(new Ef6Person {
            FirstName = "Ada",
            LastName = "Lovelace",
            HomeAddress = new Ef6Address { City = "London" },
            Phones = [
                new Ef6Phone { Number = "+44-300" },
                    new Ef6Phone { Number = "+44-100" }
            ]
        });

        db.SaveChanges();

        var mapify = new Mapify([
            new Ef6NamedPhoneProfile(),
                new Ef6NamedNestedProjectToPersonProfile()
        ]);

        var mapExpr = mapify.GetMap<Ef6Person, Ef6ProjectToNamedPhonesDto>("MaskedNested");

        var result = db.People
            .Select(mapExpr)
            .Single();

        Assert.Equal(["+44-100 [MASKED]", "+44-300 [MASKED]"], result.Phones.Select(x => x.Number).ToArray());
    }

    public class Ef6MapifyContext : DbContext {
        public Ef6MapifyContext(DbConnection connection)
            : base(connection, true) {
            Database.SetInitializer<Ef6MapifyContext>(null);
        }

        public DbSet<Ef6Person> People { get; set; } = null!;
        public DbSet<Ef6Address> Addresses { get; set; } = null!;
        public DbSet<Ef6Phone> Phones { get; set; } = null!;
        public DbSet<Ef6ProjectionIgnoreEntity> ProjectionIgnoreEntities { get; set; } = null!;
    }

    public class Ef6ProjectionIgnoreEntity {
        public int Id { get; set; }
        public string Included { get; set; } = string.Empty;
        public string IgnoredFromDb { get; set; } = string.Empty;
    }

    public class Ef6Person {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public int? HomeAddressId { get; set; }
        public Ef6Address HomeAddress { get; set; } = null!;
        public ICollection<Ef6Phone> Phones { get; set; } = [];
    }

    public class Ef6Address {
        public int Id { get; set; }
        public string City { get; set; } = string.Empty;
    }

    public class Ef6Phone {
        public int Id { get; set; }
        public string Number { get; set; } = string.Empty;
        public int PersonId { get; set; }
        public Ef6Person Person { get; set; } = null!;
    }

    public class Ef6PersonDto {
        public string FullName { get; set; } = string.Empty;
        public Ef6AddressDto HomeAddress { get; set; } = null!;
        public List<Ef6PhoneDto> Phones { get; set; } = [];
    }

    public class Ef6AddressDto {
        public string City { get; set; } = string.Empty;
    }

    public class Ef6PhoneDto {
        public string Number { get; set; } = string.Empty;
    }

    public class Ef6PersonCollectionsDto {
        public string FullName { get; set; } = string.Empty;
        public List<Ef6PhoneDto> PhonesList { get; set; } = [];
        public IEnumerable<Ef6PhoneDto> PhonesEnumerable { get; set; } = [];
    }

    public class Ef6PersonArrayCollectionsDto {
        public string FullName { get; set; } = string.Empty;
        public Ef6PhoneDto[] PhonesArray { get; set; } = [];
        public List<Ef6PhoneDto> PhonesList { get; set; } = [];
    }

    public class Ef6PrimitiveCollectionsSource {
        public ICollection<int> Numbers { get; set; } = [];
        public ICollection<string> Texts { get; set; } = [];
    }

    public class Ef6PrimitiveArrayCollectionsSource {
        public int[] Numbers { get; set; } = [];
        public ICollection<string> Texts { get; set; } = [];
    }

    public class Ef6PrimitiveCollectionsDto {
        public List<int> Numbers { get; set; } = [];
        public IEnumerable<string> Texts { get; set; } = [];
    }

    public class Ef6PrimitiveArrayCollectionsDto {
        public List<int> Numbers { get; set; } = [];
        public string[] Texts { get; set; } = [];
    }

    public class Ef6PersonImplicitNestedAndCollectionsDto {
        public string FullName { get; set; } = string.Empty;
        public Ef6AddressDto HomeAddress { get; set; } = null!;
        public List<Ef6PhoneDto> Phones { get; set; } = [];
    }

    public class Ef6PersonImplicitNestedAndArrayDto {
        public string FullName { get; set; } = string.Empty;
        public Ef6AddressDto HomeAddress { get; set; } = null!;
        public Ef6PhoneDto[] Phones { get; set; } = [];
    }

    public class Ef6PersonFilteredPhonesDto {
        public IEnumerable<Ef6PhoneDto> Students { get; set; } = [];
    }

    public class Ef6NamedPhonesDto {
        public IEnumerable<Ef6PhoneDto> PhonesRaw { get; set; } = [];
        public IEnumerable<Ef6PhoneDto> PhonesMasked { get; set; } = [];
    }

    public class Ef6PersonChainedPhonesDto {
        public IEnumerable<Ef6PhoneDto> PhonesOrdered { get; set; } = [];
    }

    public class Ef6NamedPersonChainedPhonesDto {
        public IEnumerable<Ef6PhoneDto> PhonesOrdered { get; set; } = [];
    }

    public class Ef6PersonCalculationDto {
        public int Id { get; set; }
        public int AgeInDays { get; set; }
    }

    public class Ef6ProjectToNamedPhonesDto {
        public IEnumerable<Ef6PhoneDto> Phones { get; set; } = [];
    }

    public class Ef6ProjectionIgnoreDto {
        public string Included { get; set; } = string.Empty;
        public string? IgnoredFromDb { get; set; }
    }

    public class Ef6NullCollectionSource {
        public List<int>? Numbers { get; set; }
    }

    public class Ef6NullableCollectionTarget {
        public List<int>? Numbers { get; set; }
    }

    public class Ef6NonNullableCollectionTarget {
        public List<int> Numbers { get; set; } = null!;
    }

    private class Ef6PhoneProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<Ef6Phone, Ef6PhoneDto>();
        }
    }

    private class Ef6AddressProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<Ef6Address, Ef6AddressDto>();
        }
    }

    private class Ef6PersonCollectionsProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<Ef6Person, Ef6PersonCollectionsDto>(x => new Ef6PersonCollectionsDto {
                FullName = x.FirstName + " " + x.LastName,
                PhonesList = UseMap<ICollection<Ef6Phone>, List<Ef6PhoneDto>>(x.Phones),
                PhonesEnumerable = UseMap<ICollection<Ef6Phone>, IEnumerable<Ef6PhoneDto>>(x.Phones)
            });
        }
    }

    private class Ef6PersonArrayCollectionsProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<Ef6Person, Ef6PersonArrayCollectionsDto>(x => new Ef6PersonArrayCollectionsDto {
                FullName = x.FirstName + " " + x.LastName,
                PhonesArray = UseMap<ICollection<Ef6Phone>, Ef6PhoneDto[]>(x.Phones),
                PhonesList = UseMap<ICollection<Ef6Phone>, List<Ef6PhoneDto>>(x.Phones)
            });
        }
    }

    private class Ef6PersonImplicitNestedAndCollectionsProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<Ef6Person, Ef6PersonImplicitNestedAndCollectionsDto>(x => new Ef6PersonImplicitNestedAndCollectionsDto {
                FullName = x.FirstName + " " + x.LastName
            });
        }
    }

    private class Ef6PersonImplicitNestedAndArrayProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<Ef6Person, Ef6PersonImplicitNestedAndArrayDto>(x => new Ef6PersonImplicitNestedAndArrayDto {
                FullName = x.FirstName + " " + x.LastName
            });
        }
    }

    private class Ef6PersonFilteredPhonesProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<Ef6Person, Ef6PersonFilteredPhonesDto>(x => new Ef6PersonFilteredPhonesDto {
                Students = UseMap<IEnumerable<Ef6Phone>, IEnumerable<Ef6PhoneDto>>(x.Phones.Where(s => s.Number.StartsWith("+44")))
            });
        }
    }

    private class Ef6NamedPhoneProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<Ef6Phone, Ef6PhoneDto>("Raw", x => new Ef6PhoneDto {
                Number = x.Number
            });

            CreateMap<Ef6Phone, Ef6PhoneDto>("Masked", x => new Ef6PhoneDto {
                Number = x.Number + " [MASKED]"
            });
        }
    }

    private class Ef6NamedPersonProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<Ef6Person, Ef6NamedPhonesDto>(x => new Ef6NamedPhonesDto {
                PhonesRaw = UseMap<IEnumerable<Ef6Phone>, IEnumerable<Ef6PhoneDto>>("Raw", x.Phones),
                PhonesMasked = UseMap<IEnumerable<Ef6Phone>, IEnumerable<Ef6PhoneDto>>("Masked", x.Phones)
            });
        }
    }

    private class Ef6PersonChainedPhonesProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<Ef6Person, Ef6PersonChainedPhonesDto>(x => new Ef6PersonChainedPhonesDto {
                PhonesOrdered = UseMap<IEnumerable<Ef6Phone>, IEnumerable<Ef6PhoneDto>>(x.Phones)
                    .OrderBy(dto => dto.Number)
                    .ToList()
            });
        }
    }

    private class Ef6NamedPersonChainedProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<Ef6Person, Ef6NamedPersonChainedPhonesDto>(x => new Ef6NamedPersonChainedPhonesDto {
                PhonesOrdered = UseMap<IEnumerable<Ef6Phone>, IEnumerable<Ef6PhoneDto>>("Masked", x.Phones)
                    .OrderBy(dto => dto.Number)
                    .ToList()
            });
        }
    }

    private class Ef6IntIdentityProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<int, int>(x => x);
        }
    }

    private class Ef6PersonCalculationProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<Ef6Person, Ef6PersonCalculationDto>(x => new Ef6PersonCalculationDto {
                AgeInDays = 365 * UseMap<int, int>(x.Id)
            });
        }
    }

    private class Ef6NamedProjectToPersonProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<Ef6Person, Ef6ProjectToNamedPhonesDto>("Raw", x => new Ef6ProjectToNamedPhonesDto {
                Phones = x.Phones.ProjectTo<Ef6PhoneDto>("Raw").ToList()
            });

            CreateMap<Ef6Person, Ef6ProjectToNamedPhonesDto>("Masked", x => new Ef6ProjectToNamedPhonesDto {
                Phones = x.Phones.ProjectTo<Ef6PhoneDto>("Masked").ToList()
            });
        }
    }

    private class Ef6NamedNestedProjectToPersonProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<Ef6Person, Ef6ProjectToNamedPhonesDto>("MaskedNested", x => new Ef6ProjectToNamedPhonesDto {
                Phones = x.Phones.ProjectTo<Ef6PhoneDto>("Masked")
                    .OrderBy(dto => dto.Number)
                    .ToList()
            });
        }
    }

    private class Ef6ProjectionIgnoreProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<Ef6ProjectionIgnoreEntity, Ef6ProjectionIgnoreDto>(x => new Ef6ProjectionIgnoreDto {
                IgnoredFromDb = Ignore<string>()
            });
        }
    }

    private class Ef6DiSource {
        public int Value { get; set; }
    }

    private class Ef6DiTarget {
        public int Value { get; set; }
    }

    private class Ef6DiProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<Ef6DiSource, Ef6DiTarget>();
        }
    }
}
