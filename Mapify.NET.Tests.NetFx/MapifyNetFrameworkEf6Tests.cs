using System.Data.Common;
using System.Data.Entity;
using LinqKit;
using Microsoft.Extensions.DependencyInjection;

namespace Mapify.NET.Tests.NetFx;

public class MapifyNetFrameworkEf6Tests {
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
        using (var connection = Effort.DbConnectionFactory.CreateTransient())
        using (var db = new Ef6MapifyContext(connection)) {
            db.Database.CreateIfNotExists();

            var adaAddress = new Ef6Address { City = "London" };
            var alanAddress = new Ef6Address { City = "Manchester" };

            db.People.Add(new Ef6Person {
                FirstName = "Ada",
                LastName = "Lovelace",
                HomeAddress = adaAddress,
                Phones = new List<Ef6Phone> {
                    new Ef6Phone { Number = "+44-100" },
                    new Ef6Phone { Number = "+44-101" }
                }
            });

            db.People.Add(new Ef6Person {
                FirstName = "Alan",
                LastName = "Turing",
                HomeAddress = alanAddress,
                Phones = new List<Ef6Phone> {
                    new Ef6Phone { Number = "+44-200" }
                }
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
    }

    public class Ef6MapifyContext : DbContext {
        public Ef6MapifyContext(DbConnection connection)
            : base(connection, true) {
            Database.SetInitializer<Ef6MapifyContext>(null);
        }

        public DbSet<Ef6Person> People { get; set; } = null!;
        public DbSet<Ef6Address> Addresses { get; set; } = null!;
        public DbSet<Ef6Phone> Phones { get; set; } = null!;
    }

    public class Ef6Person {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public int? HomeAddressId { get; set; }
        public Ef6Address HomeAddress { get; set; } = null!;
        public ICollection<Ef6Phone> Phones { get; set; } = new List<Ef6Phone>();
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
        public List<Ef6PhoneDto> Phones { get; set; } = new List<Ef6PhoneDto>();
    }

    public class Ef6AddressDto {
        public string City { get; set; } = string.Empty;
    }

    public class Ef6PhoneDto {
        public string Number { get; set; } = string.Empty;
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
