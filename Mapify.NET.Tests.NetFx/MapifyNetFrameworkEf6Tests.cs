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

    [Fact]
    public void InstanceMapify_UseMap_ShouldWorkInEf6Projection_ForEnumerableCollections() {
        using (var connection = Effort.DbConnectionFactory.CreateTransient())
        using (var db = new Ef6MapifyContext(connection)) {
            db.Database.CreateIfNotExists();

            db.People.Add(new Ef6Person {
                FirstName = "Ada",
                LastName = "Lovelace",
                HomeAddress = new Ef6Address { City = "London" },
                Phones = new List<Ef6Phone> {
                    new Ef6Phone { Number = "+44-100" },
                    new Ef6Phone { Number = "+44-101" }
                }
            });

            db.People.Add(new Ef6Person {
                FirstName = "Alan",
                LastName = "Turing",
                HomeAddress = new Ef6Address { City = "Manchester" },
                Phones = new List<Ef6Phone> {
                    new Ef6Phone { Number = "+44-200" }
                }
            });

            db.SaveChanges();

            var mapify = new Mapify(new IMapifyProfile[] {
                new Ef6PhoneProfile(),
                new Ef6PersonCollectionsProfile()
            });

            var mapExpr = mapify.GetMap<Ef6Person, Ef6PersonCollectionsDto>();

            var result = db.People
                .OrderBy(x => x.Id)
                .Select(mapExpr)
                .ToList();

            Assert.Equal(2, result.Count);
            Assert.Equal("Ada Lovelace", result[0].FullName);
            Assert.Equal(new[] { "+44-100", "+44-101" }, result[0].PhonesList.Select(x => x.Number).ToArray());
            Assert.Equal(new[] { "+44-100", "+44-101" }, result[0].PhonesEnumerable.Select(x => x.Number).ToArray());

            Assert.Equal("Alan Turing", result[1].FullName);
            Assert.Equal(new[] { "+44-200" }, result[1].PhonesList.Select(x => x.Number).ToArray());
            Assert.Equal(new[] { "+44-200" }, result[1].PhonesEnumerable.Select(x => x.Number).ToArray());
        }
    }

    [Fact]
    public void CreateMap_ShouldImplicitlyMapPrimitiveEnumerableCollections_InEf6Projection() {
        using (var connection = Effort.DbConnectionFactory.CreateTransient())
        using (var db = new Ef6MapifyContext(connection)) {
            db.Database.CreateIfNotExists();

            db.People.Add(new Ef6Person {
                FirstName = "Ada",
                LastName = "Lovelace",
                HomeAddress = new Ef6Address { City = "London" },
                Phones = new List<Ef6Phone> {
                    new Ef6Phone { Number = "+44-100" },
                    new Ef6Phone { Number = "+44-101" }
                }
            });

            db.People.Add(new Ef6Person {
                FirstName = "Alan",
                LastName = "Turing",
                HomeAddress = new Ef6Address { City = "Manchester" },
                Phones = new List<Ef6Phone> {
                    new Ef6Phone { Number = "+44-200" }
                }
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
            Assert.Equal(new[] { "+44-100", "+44-101" }, result[0].Texts);
            Assert.Equal(new[] { 3 }, result[1].Numbers);
            Assert.Equal(new[] { "+44-200" }, result[1].Texts);
        }
    }

    [Fact]
    public void InstanceMapify_ShouldImplicitlyUseExistingMapsForNestedAndCollectionMembers_InEf6Projection() {
        using (var connection = Effort.DbConnectionFactory.CreateTransient())
        using (var db = new Ef6MapifyContext(connection)) {
            db.Database.CreateIfNotExists();

            db.People.Add(new Ef6Person {
                FirstName = "Ada",
                LastName = "Lovelace",
                HomeAddress = new Ef6Address { City = "London" },
                Phones = new List<Ef6Phone> {
                    new Ef6Phone { Number = "+44-100" },
                    new Ef6Phone { Number = "+44-101" }
                }
            });

            db.People.Add(new Ef6Person {
                FirstName = "Alan",
                LastName = "Turing",
                HomeAddress = new Ef6Address { City = "Manchester" },
                Phones = new List<Ef6Phone> {
                    new Ef6Phone { Number = "+44-200" }
                }
            });

            db.SaveChanges();

            var mapify = new Mapify(new IMapifyProfile[] {
                new Ef6AddressProfile(),
                new Ef6PhoneProfile(),
                new Ef6PersonImplicitNestedAndCollectionsProfile()
            });

            var mapExpr = mapify.GetMap<Ef6Person, Ef6PersonImplicitNestedAndCollectionsDto>();

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
    }

    [Fact]
    public void InstanceMapify_UseMap_ShouldAcceptFilterExpressions_InEf6Projection() {
        using (var connection = Effort.DbConnectionFactory.CreateTransient())
        using (var db = new Ef6MapifyContext(connection)) {
            db.Database.CreateIfNotExists();

            db.People.Add(new Ef6Person {
                FirstName = "Ada",
                LastName = "Lovelace",
                HomeAddress = new Ef6Address { City = "London" },
                Phones = new List<Ef6Phone> {
                    new Ef6Phone { Number = "+44-100" },
                    new Ef6Phone { Number = "+44-101" },
                    new Ef6Phone { Number = "+1-999" }
                }
            });

            db.People.Add(new Ef6Person {
                FirstName = "Alan",
                LastName = "Turing",
                HomeAddress = new Ef6Address { City = "Manchester" },
                Phones = new List<Ef6Phone> {
                    new Ef6Phone { Number = "+44-200" },
                    new Ef6Phone { Number = "+1-123" }
                }
            });

            db.SaveChanges();

            var mapify = new Mapify(new IMapifyProfile[] {
                new Ef6PhoneProfile(),
                new Ef6PersonFilteredPhonesProfile()
            });

            var mapExpr = mapify.GetMap<Ef6Person, Ef6PersonFilteredPhonesDto>();

            var result = db.People
                .OrderBy(x => x.Id)
                .Select(mapExpr)
                .ToList();

            Assert.Equal(2, result.Count);
            Assert.Equal(new[] { "+44-100", "+44-101" }, result[0].Students.Select(x => x.Number).ToArray());
            Assert.Equal(new[] { "+44-200" }, result[1].Students.Select(x => x.Number).ToArray());
        }
    }

    [Fact]
    public void InstanceMapify_UseMap_ShouldSupportNamedMappings_InEf6Projection() {
        using (var connection = Effort.DbConnectionFactory.CreateTransient())
        using (var db = new Ef6MapifyContext(connection)) {
            db.Database.CreateIfNotExists();

            db.People.Add(new Ef6Person {
                FirstName = "Ada",
                LastName = "Lovelace",
                HomeAddress = new Ef6Address { City = "London" },
                Phones = new List<Ef6Phone> {
                    new Ef6Phone { Number = "+44-100" },
                    new Ef6Phone { Number = "+44-101" }
                }
            });

            db.SaveChanges();

            var mapify = new Mapify(new IMapifyProfile[] {
                new Ef6NamedPhoneProfile(),
                new Ef6NamedPersonProfile()
            });

            var mapExpr = mapify.GetMap<Ef6Person, Ef6NamedPhonesDto>();

            var result = db.People
                .OrderBy(x => x.Id)
                .Select(mapExpr)
                .Single();

            Assert.Equal(new[] { "+44-100", "+44-101" }, result.PhonesRaw.Select(x => x.Number).ToArray());
            Assert.Equal(new[] { "+44-100 [MASKED]", "+44-101 [MASKED]" }, result.PhonesMasked.Select(x => x.Number).ToArray());
        }
    }

    [Fact]
    public void InstanceMapify_UseMap_ShouldAllowChaining_InEf6Projection() {
        using (var connection = Effort.DbConnectionFactory.CreateTransient())
        using (var db = new Ef6MapifyContext(connection)) {
            db.Database.CreateIfNotExists();

            db.People.Add(new Ef6Person {
                FirstName = "Ada",
                LastName = "Lovelace",
                HomeAddress = new Ef6Address { City = "London" },
                Phones = new List<Ef6Phone> {
                    new Ef6Phone { Number = "+44-300" },
                    new Ef6Phone { Number = "+44-100" },
                    new Ef6Phone { Number = "+44-200" }
                }
            });

            db.SaveChanges();

            var mapify = new Mapify(new IMapifyProfile[] {
                new Ef6PhoneProfile(),
                new Ef6PersonChainedPhonesProfile()
            });

            var mapExpr = mapify.GetMap<Ef6Person, Ef6PersonChainedPhonesDto>();

            var result = db.People
                .Select(mapExpr)
                .Single();

            Assert.Equal(new[] { "+44-100", "+44-200", "+44-300" }, result.PhonesOrdered.Select(x => x.Number).ToArray());
        }
    }

    [Fact]
    public void InstanceMapify_UseMapNamed_ShouldAllowChaining_InEf6Projection() {
        using (var connection = Effort.DbConnectionFactory.CreateTransient())
        using (var db = new Ef6MapifyContext(connection)) {
            db.Database.CreateIfNotExists();

            db.People.Add(new Ef6Person {
                FirstName = "Ada",
                LastName = "Lovelace",
                HomeAddress = new Ef6Address { City = "London" },
                Phones = new List<Ef6Phone> {
                    new Ef6Phone { Number = "+44-300" },
                    new Ef6Phone { Number = "+44-100" }
                }
            });

            db.SaveChanges();

            var mapify = new Mapify(new IMapifyProfile[] {
                new Ef6NamedPhoneProfile(),
                new Ef6NamedPersonChainedProfile()
            });

            var mapExpr = mapify.GetMap<Ef6Person, Ef6NamedPersonChainedPhonesDto>();

            var result = db.People
                .Select(mapExpr)
                .Single();

            Assert.Equal(new[] { "+44-100 [MASKED]", "+44-300 [MASKED]" }, result.PhonesOrdered.Select(x => x.Number).ToArray());
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

    public class Ef6PersonCollectionsDto {
        public string FullName { get; set; } = string.Empty;
        public List<Ef6PhoneDto> PhonesList { get; set; } = new List<Ef6PhoneDto>();
        public IEnumerable<Ef6PhoneDto> PhonesEnumerable { get; set; } = Enumerable.Empty<Ef6PhoneDto>();
    }

    public class Ef6PrimitiveCollectionsSource {
        public ICollection<int> Numbers { get; set; } = new List<int>();
        public ICollection<string> Texts { get; set; } = new List<string>();
    }

    public class Ef6PrimitiveCollectionsDto {
        public List<int> Numbers { get; set; } = new List<int>();
        public IEnumerable<string> Texts { get; set; } = Enumerable.Empty<string>();
    }

    public class Ef6PersonImplicitNestedAndCollectionsDto {
        public string FullName { get; set; } = string.Empty;
        public Ef6AddressDto HomeAddress { get; set; } = null!;
        public List<Ef6PhoneDto> Phones { get; set; } = new List<Ef6PhoneDto>();
    }

    public class Ef6PersonFilteredPhonesDto {
        public IEnumerable<Ef6PhoneDto> Students { get; set; } = Enumerable.Empty<Ef6PhoneDto>();
    }

    public class Ef6NamedPhonesDto {
        public IEnumerable<Ef6PhoneDto> PhonesRaw { get; set; } = Enumerable.Empty<Ef6PhoneDto>();
        public IEnumerable<Ef6PhoneDto> PhonesMasked { get; set; } = Enumerable.Empty<Ef6PhoneDto>();
    }

    public class Ef6PersonChainedPhonesDto {
        public IEnumerable<Ef6PhoneDto> PhonesOrdered { get; set; } = Enumerable.Empty<Ef6PhoneDto>();
    }

    public class Ef6NamedPersonChainedPhonesDto {
        public IEnumerable<Ef6PhoneDto> PhonesOrdered { get; set; } = Enumerable.Empty<Ef6PhoneDto>();
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

    private class Ef6PersonImplicitNestedAndCollectionsProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<Ef6Person, Ef6PersonImplicitNestedAndCollectionsDto>(x => new Ef6PersonImplicitNestedAndCollectionsDto {
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
