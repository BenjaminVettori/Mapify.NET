using Microsoft.EntityFrameworkCore;
using LinqKit;

namespace Mapify.NET.Tests.EFCore;

public partial class MapifyEfCoreProjectionTests {
    [Fact]
    public void CreateMap_ShouldWorkInEfCoreProjection_WithNestedInvokeForSingleAndCollection() {
        var options = new DbContextOptionsBuilder<EfCoreMapifyContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        using var db = new EfCoreMapifyContext(options);

        var graceAddress = new EfCoreAddress { City = "New York" };
        var katherineAddress = new EfCoreAddress { City = "White Sulphur Springs" };

        db.People.AddRange(
            new EfCorePerson {
                FirstName = "Grace",
                LastName = "Hopper",
                HomeAddress = graceAddress,
                Phones = [
                    new EfCorePhone { Number = "+1-300" },
                    new EfCorePhone { Number = "+1-301" }
                ]
            },
            new EfCorePerson {
                FirstName = "Katherine",
                LastName = "Johnson",
                HomeAddress = katherineAddress,
                Phones = [
                    new EfCorePhone { Number = "+1-400" }
                ]
            }
        );

        db.SaveChanges();

        var mapify = new Mapify([
            new EfCoreAddressProfile(),
            new EfCorePhoneProfile(),
            new EfCorePersonInvokeProfile()
        ]);

        var map = mapify.GetRequiredMap<EfCorePerson, EfCorePersonDto>();

        var result = db.People
            .AsExpandable()
            .OrderBy(x => x.Id)
            .Select(map)
            .ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal("Grace Hopper", result[0].FullName);
        Assert.Equal("New York", result[0].HomeAddress.City);
        Assert.Equal(2, result[0].Phones.Count);
        Assert.Equal("Katherine Johnson", result[1].FullName);
        Assert.Equal("White Sulphur Springs", result[1].HomeAddress.City);
        Assert.Single(result[1].Phones);
    }

    [Fact]
    public void CreateMap_ShouldImplicitlyMapPrimitiveArraysAndCollections_InEfCoreProjection() {
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

        var mapify = new Mapify([new EfCorePrimitiveCollectionsProfile()]);
        var map = mapify.GetRequiredMap<EfCorePrimitiveCollectionsSource, EfCorePrimitiveCollectionsDto>();

        var result = db.People
            .OrderBy(x => x.Id)
            .Select(x => new EfCorePrimitiveCollectionsSource {
                Numbers = x.Phones.OrderBy(p => p.Id).Select(p => p.Id).ToArray(),
                Texts = x.Phones.OrderBy(p => p.Id).Select(p => p.Number).ToList()
            })
            .Select(map)
            .ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(new[] { 1, 2 }, result[0].Numbers);
        Assert.Equal(new[] { "+1-300", "+1-301" }, result[0].Texts);
        Assert.Equal(new[] { 3 }, result[1].Numbers);
        Assert.Equal(new[] { "+44-200" }, result[1].Texts);
    }

    [Fact]
    public void InstanceMapify_ShouldImplicitlyUseExistingMapsForNestedAndArrayMembers_InEfCoreProjection() {
        var options = new DbContextOptionsBuilder<EfCoreMapifyContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        using var db = new EfCoreMapifyContext(options);

        db.People.AddRange(
            new EfCorePerson {
                FirstName = "Ada",
                LastName = "Lovelace",
                HomeAddress = new EfCoreAddress { City = "London" },
                Phones = [
                    new EfCorePhone { Number = "+44-100" },
                    new EfCorePhone { Number = "+44-101" }
                ]
            },
            new EfCorePerson {
                FirstName = "Alan",
                LastName = "Turing",
                HomeAddress = new EfCoreAddress { City = "Manchester" },
                Phones = [
                    new EfCorePhone { Number = "+44-200" }
                ]
            }
        );

        db.SaveChanges();

        var mapify = new Mapify([
            new EfCoreAddressProfile(),
            new EfCorePhoneProfile(),
            new EfCorePersonImplicitNestedAndArrayProfile()
        ]);

        var mapExpr = mapify.GetRequiredMap<EfCorePerson, EfCorePersonImplicitNestedAndArrayDto>();

        var result = db.People
            .OrderBy(x => x.Id)
            .Select(mapExpr)
            .ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal("Ada Lovelace", result[0].FullName);
        Assert.Equal("London", result[0].HomeAddress.City);
        Assert.Equal(new[] { "+44-100", "+44-101" }, result[0].Phones.Select(x => x.Number).ToArray());
        Assert.Equal("Alan Turing", result[1].FullName);
        Assert.Equal("Manchester", result[1].HomeAddress.City);
        Assert.Equal(new[] { "+44-200" }, result[1].Phones.Select(x => x.Number).ToArray());
    }

    private sealed class EfCorePersonInvokeProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<EfCorePerson, EfCorePersonDto>(x => new EfCorePersonDto {
                FullName = x.FirstName + " " + x.LastName,
                HomeAddress = UseMap<EfCoreAddress, EfCoreAddressDto>(x.HomeAddress),
                Phones = UseMap<IEnumerable<EfCorePhone>, IEnumerable<EfCorePhoneDto>>(x.Phones).ToList()
            });
        }
    }

    private sealed class EfCorePrimitiveCollectionsProfile : MapifyProfile {
        protected override void Configure() => CreateMap<EfCorePrimitiveCollectionsSource, EfCorePrimitiveCollectionsDto>();
    }
}
