using LinqKit;

namespace Mapify.NET.Tests.NetFx;

public partial class MapifyNetFrameworkEf6ProjectionTests {
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
}
