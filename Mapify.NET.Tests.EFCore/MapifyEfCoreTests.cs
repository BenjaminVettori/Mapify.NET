using Microsoft.EntityFrameworkCore;
using LinqKit;

namespace Mapify.NET.Tests.EFCore;

public class MapifyEfCoreTests {
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
                Phones = new List<EfCorePhone> {
                    new EfCorePhone { Number = "+1-300" },
                    new EfCorePhone { Number = "+1-301" }
                }
            },
            new EfCorePerson {
                FirstName = "Katherine",
                LastName = "Johnson",
                HomeAddress = katherineAddress,
                Phones = new List<EfCorePhone> {
                    new EfCorePhone { Number = "+1-400" }
                }
            }
        );

        db.SaveChanges();

        var addressMap = Mapper.CreateMap<EfCoreAddress, EfCoreAddressDto>();
        var phoneMap = Mapper.CreateMap<EfCorePhone, EfCorePhoneDto>();

        var map = Mapper.CreateMap<EfCorePerson, EfCorePersonDto>(x => new EfCorePersonDto {
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
        Assert.Equal("Grace Hopper", result[0].FullName);
        Assert.Equal("New York", result[0].HomeAddress.City);
        Assert.Equal(2, result[0].Phones.Count);
        Assert.Equal("Katherine Johnson", result[1].FullName);
        Assert.Equal("White Sulphur Springs", result[1].HomeAddress.City);
        Assert.Single(result[1].Phones);
    }

    private sealed class EfCoreMapifyContext(DbContextOptions<EfCoreMapifyContext> options) : DbContext(options) {
        public DbSet<EfCorePerson> People => Set<EfCorePerson>();
        public DbSet<EfCoreAddress> Addresses => Set<EfCoreAddress>();
        public DbSet<EfCorePhone> Phones => Set<EfCorePhone>();
    }

    private sealed class EfCorePerson {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public int? HomeAddressId { get; set; }
        public EfCoreAddress HomeAddress { get; set; } = null!;
        public ICollection<EfCorePhone> Phones { get; set; } = new List<EfCorePhone>();
    }

    private sealed class EfCoreAddress {
        public int Id { get; set; }
        public string City { get; set; } = string.Empty;
    }

    private sealed class EfCorePhone {
        public int Id { get; set; }
        public string Number { get; set; } = string.Empty;
        public int PersonId { get; set; }
        public EfCorePerson Person { get; set; } = null!;
    }

    private sealed class EfCorePersonDto {
        public string FullName { get; set; } = string.Empty;
        public EfCoreAddressDto HomeAddress { get; set; } = null!;
        public List<EfCorePhoneDto> Phones { get; set; } = new List<EfCorePhoneDto>();
    }

    private sealed class EfCoreAddressDto {
        public string City { get; set; } = string.Empty;
    }

    private sealed class EfCorePhoneDto {
        public string Number { get; set; } = string.Empty;
    }
}
