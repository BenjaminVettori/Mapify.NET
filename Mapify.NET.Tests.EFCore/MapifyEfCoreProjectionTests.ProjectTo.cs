using Microsoft.EntityFrameworkCore;

namespace Mapify.NET.Tests.EFCore;

public partial class MapifyEfCoreProjectionTests {
    [Fact]
    public void InstanceMapify_ProjectTo_ShouldWorkInEfCoreProjection() {
        var options = new DbContextOptionsBuilder<EfCoreMapifyContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        using var db = new EfCoreMapifyContext(options);

        db.People.AddRange(
            new EfCorePerson {
                FirstName = "Grace",
                LastName = "Hopper",
                HomeAddress = new EfCoreAddress { City = "New York" },
                Phones = [
                    new EfCorePhone { Number = "+1-300" },
                    new EfCorePhone { Number = "+1-301" }
                ]
            },
            new EfCorePerson {
                FirstName = "Alan",
                LastName = "Turing",
                HomeAddress = new EfCoreAddress { City = "London" },
                Phones = [
                    new EfCorePhone { Number = "+44-200" }
                ]
            }
        );

        db.SaveChanges();

        var mapify = new Mapify([
            new EfCorePhoneProfile(),
            new EfCorePersonCollectionsProfile()
        ]);

        var result = db.People
            .OrderBy(x => x.Id)
            .ProjectTo<EfCorePersonCollectionsDto>(mapify)
            .ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal("Grace Hopper", result[0].FullName);
        Assert.Equal(new[] { "+1-300", "+1-301" }, result[0].PhonesArray.Select(x => x.Number).ToArray());
        Assert.Equal(new[] { "+1-300", "+1-301" }, result[0].PhonesList.Select(x => x.Number).ToArray());

        Assert.Equal("Alan Turing", result[1].FullName);
        Assert.Equal(new[] { "+44-200" }, result[1].PhonesArray.Select(x => x.Number).ToArray());
        Assert.Equal(new[] { "+44-200" }, result[1].PhonesList.Select(x => x.Number).ToArray());
    }

    [Fact]
    public void InstanceMapify_ProjectToNamed_ShouldWorkInEfCoreProjection() {
        var options = new DbContextOptionsBuilder<EfCoreMapifyContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        using var db = new EfCoreMapifyContext(options);

        db.People.Add(new EfCorePerson {
            FirstName = "Ada",
            LastName = "Lovelace",
            HomeAddress = new EfCoreAddress { City = "London" },
            Phones = [
                new EfCorePhone { Number = "+44-100" },
                new EfCorePhone { Number = "+44-101" }
            ]
        });

        db.SaveChanges();

        var mapify = new Mapify([
            new EfCoreNamedPhoneProfile(),
            new EfCoreNamedProjectToPersonProfile()
        ]);

        var result = db.People
            .ProjectTo<EfCoreProjectToNamedPhonesDto>(mapify, "Masked")
            .Single();

        Assert.Equal(new[] { "+44-100 [MASKED]", "+44-101 [MASKED]" }, result.Phones.Select(x => x.Number).ToArray());
    }

    [Fact]
    public void InstanceMapify_ProjectToWithParameters_ShouldWorkInEfCoreProjection() {
        var options = new DbContextOptionsBuilder<EfCoreMapifyContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        using var db = new EfCoreMapifyContext(options);
        db.Database.OpenConnection();
        db.Database.EnsureCreated();

        db.People.AddRange(
            new EfCorePerson {
                FirstName = "Grace",
                LastName = "Hopper",
                HomeAddress = new EfCoreAddress { City = "New York" }
            },
            new EfCorePerson {
                FirstName = "Katherine",
                LastName = "Johnson",
                HomeAddress = new EfCoreAddress { City = "White Sulphur Springs" }
            }
        );

        db.SaveChanges();

        var mapify = new Mapify([
            new EfCorePersonRuntimeParameterProfile()
        ]);

        var result = db.People
            .OrderBy(x => x.Id)
            .ProjectTo<EfCorePersonRuntimeParameterDto>(mapify, new Dictionary<string, object?> { ["offset"] = 50 })
            .ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(new[] { 51, 52 }, result.Select(x => x.AdjustedId).ToArray());
    }

    [Fact]
    public void InstanceMapify_NestedNamedProjectToMarker_ShouldWorkInEfCoreProjection() {
        var options = new DbContextOptionsBuilder<EfCoreMapifyContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        using var db = new EfCoreMapifyContext(options);

        db.People.Add(new EfCorePerson {
            FirstName = "Ada",
            LastName = "Lovelace",
            HomeAddress = new EfCoreAddress { City = "London" },
            Phones = [
                new EfCorePhone { Number = "+44-300" },
                new EfCorePhone { Number = "+44-100" }
            ]
        });

        db.SaveChanges();

        var mapify = new Mapify([
            new EfCoreNamedPhoneProfile(),
            new EfCoreNamedNestedProjectToPersonProfile()
        ]);

        var mapExpr = mapify.GetRequiredMap<EfCorePerson, EfCoreProjectToNamedPhonesDto>("Masked");

        var result = db.People
            .Select(mapExpr)
            .Single();

        Assert.Equal(new[] { "+44-100 [MASKED]", "+44-300 [MASKED]" }, result.Phones.Select(x => x.Number).ToArray());
    }
}
