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

    [Fact]
    public void InstanceMapify_UseMap_ShouldWorkInEfCoreProjection_ForArrayAndListCollections() {
        var options = new DbContextOptionsBuilder<EfCoreMapifyContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        using var db = new EfCoreMapifyContext(options);

        db.People.AddRange(
            new EfCorePerson {
                FirstName = "Grace",
                LastName = "Hopper",
                HomeAddress = new EfCoreAddress { City = "New York" },
                Phones = new List<EfCorePhone> {
                    new EfCorePhone { Number = "+1-300" },
                    new EfCorePhone { Number = "+1-301" }
                }
            },
            new EfCorePerson {
                FirstName = "Alan",
                LastName = "Turing",
                HomeAddress = new EfCoreAddress { City = "London" },
                Phones = new List<EfCorePhone> {
                    new EfCorePhone { Number = "+44-200" }
                }
            }
        );

        db.SaveChanges();

        var mapify = new Mapify(new IMapifyProfile[] {
            new EfCorePhoneProfile(),
            new EfCorePersonCollectionsProfile()
        });

        var mapExpr = mapify.GetMap<EfCorePerson, EfCorePersonCollectionsDto>();

        var result = db.People
            .OrderBy(x => x.Id)
            .Select(mapExpr)
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
                Phones = new List<EfCorePhone> {
                    new EfCorePhone { Number = "+1-300" },
                    new EfCorePhone { Number = "+1-301" }
                }
            },
            new EfCorePerson {
                FirstName = "Alan",
                LastName = "Turing",
                HomeAddress = new EfCoreAddress { City = "London" },
                Phones = new List<EfCorePhone> {
                    new EfCorePhone { Number = "+44-200" }
                }
            }
        );

        db.SaveChanges();

        var map = Mapper.CreateMap<EfCorePrimitiveCollectionsSource, EfCorePrimitiveCollectionsDto>();

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
                Phones = new List<EfCorePhone> {
                    new EfCorePhone { Number = "+44-100" },
                    new EfCorePhone { Number = "+44-101" }
                }
            },
            new EfCorePerson {
                FirstName = "Alan",
                LastName = "Turing",
                HomeAddress = new EfCoreAddress { City = "Manchester" },
                Phones = new List<EfCorePhone> {
                    new EfCorePhone { Number = "+44-200" }
                }
            }
        );

        db.SaveChanges();

        var mapify = new Mapify(new IMapifyProfile[] {
            new EfCoreAddressProfile(),
            new EfCorePhoneProfile(),
            new EfCorePersonImplicitNestedAndArrayProfile()
        });

        var mapExpr = mapify.GetMap<EfCorePerson, EfCorePersonImplicitNestedAndArrayDto>();

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

    [Fact]
    public void InstanceMapify_UseMap_ShouldAcceptFilterExpressions_InEfCoreProjection() {
        var options = new DbContextOptionsBuilder<EfCoreMapifyContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        using var db = new EfCoreMapifyContext(options);

        db.People.AddRange(
            new EfCorePerson {
                FirstName = "Ada",
                LastName = "Lovelace",
                HomeAddress = new EfCoreAddress { City = "London" },
                Phones = new List<EfCorePhone> {
                    new EfCorePhone { Number = "+44-100" },
                    new EfCorePhone { Number = "+44-101" },
                    new EfCorePhone { Number = "+1-300" }
                }
            },
            new EfCorePerson {
                FirstName = "Alan",
                LastName = "Turing",
                HomeAddress = new EfCoreAddress { City = "Manchester" },
                Phones = new List<EfCorePhone> {
                    new EfCorePhone { Number = "+44-200" },
                    new EfCorePhone { Number = "+1-999" }
                }
            }
        );

        db.SaveChanges();

        var mapify = new Mapify(new IMapifyProfile[] {
            new EfCorePhoneProfile(),
            new EfCorePersonFilteredPhonesProfile()
        });

        var mapExpr = mapify.GetMap<EfCorePerson, EfCorePersonFilteredPhonesDto>();

        var result = db.People
            .OrderBy(x => x.Id)
            .Select(mapExpr)
            .ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(new[] { "+44-100", "+44-101" }, result[0].Students.Select(x => x.Number).ToArray());
        Assert.Equal(new[] { "+44-200" }, result[1].Students.Select(x => x.Number).ToArray());
    }

    [Fact]
    public void InstanceMapify_UseMap_ShouldSupportNamedMappings_InEfCoreProjection() {
        var options = new DbContextOptionsBuilder<EfCoreMapifyContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        using var db = new EfCoreMapifyContext(options);

        db.People.Add(new EfCorePerson {
            FirstName = "Ada",
            LastName = "Lovelace",
            HomeAddress = new EfCoreAddress { City = "London" },
            Phones = new List<EfCorePhone> {
                new EfCorePhone { Number = "+44-100" },
                new EfCorePhone { Number = "+44-101" }
            }
        });

        db.SaveChanges();

        var mapify = new Mapify(new IMapifyProfile[] {
            new EfCoreNamedPhoneProfile(),
            new EfCoreNamedPersonProfile()
        });

        var mapExpr = mapify.GetMap<EfCorePerson, EfCoreNamedPhonesDto>();

        var result = db.People
            .OrderBy(x => x.Id)
            .Select(mapExpr)
            .Single();

        Assert.Equal(new[] { "+44-100", "+44-101" }, result.PhonesRaw.Select(x => x.Number).ToArray());
        Assert.Equal(new[] { "+44-100 [MASKED]", "+44-101 [MASKED]" }, result.PhonesMasked.Select(x => x.Number).ToArray());
    }

    [Fact]
    public void InstanceMapify_UseMap_ShouldAllowChaining_InEfCoreProjection() {
        var options = new DbContextOptionsBuilder<EfCoreMapifyContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        using var db = new EfCoreMapifyContext(options);

        db.People.Add(new EfCorePerson {
            FirstName = "Ada",
            LastName = "Lovelace",
            HomeAddress = new EfCoreAddress { City = "London" },
            Phones = new List<EfCorePhone> {
                new EfCorePhone { Number = "+44-300" },
                new EfCorePhone { Number = "+44-100" },
                new EfCorePhone { Number = "+44-200" }
            }
        });

        db.SaveChanges();

        var mapify = new Mapify(new IMapifyProfile[] {
            new EfCorePhoneProfile(),
            new EfCorePersonChainedPhonesProfile()
        });

        var mapExpr = mapify.GetMap<EfCorePerson, EfCorePersonChainedPhonesDto>();

        var result = db.People
            .Select(mapExpr)
            .Single();

        Assert.Equal(new[] { "+44-100", "+44-200", "+44-300" }, result.PhonesOrdered.Select(x => x.Number).ToArray());
    }

    [Fact]
    public void InstanceMapify_UseMapNamed_ShouldAllowChaining_InEfCoreProjection() {
        var options = new DbContextOptionsBuilder<EfCoreMapifyContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        using var db = new EfCoreMapifyContext(options);

        db.People.Add(new EfCorePerson {
            FirstName = "Ada",
            LastName = "Lovelace",
            HomeAddress = new EfCoreAddress { City = "London" },
            Phones = new List<EfCorePhone> {
                new EfCorePhone { Number = "+44-300" },
                new EfCorePhone { Number = "+44-100" }
            }
        });

        db.SaveChanges();

        var mapify = new Mapify(new IMapifyProfile[] {
            new EfCoreNamedPhoneProfile(),
            new EfCoreNamedPersonChainedProfile()
        });

        var mapExpr = mapify.GetMap<EfCorePerson, EfCorePersonChainedPhonesDto>();

        var result = db.People
            .Select(mapExpr)
            .Single();

        Assert.Equal(new[] { "+44-100 [MASKED]", "+44-300 [MASKED]" }, result.PhonesOrdered.Select(x => x.Number).ToArray());
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

    private sealed class EfCorePersonCollectionsDto {
        public string FullName { get; set; } = string.Empty;
        public EfCorePhoneDto[] PhonesArray { get; set; } = Array.Empty<EfCorePhoneDto>();
        public List<EfCorePhoneDto> PhonesList { get; set; } = new List<EfCorePhoneDto>();
    }

    private sealed class EfCorePrimitiveCollectionsSource {
        public int[] Numbers { get; set; } = Array.Empty<int>();
        public ICollection<string> Texts { get; set; } = new List<string>();
    }

    private sealed class EfCorePrimitiveCollectionsDto {
        public List<int> Numbers { get; set; } = new List<int>();
        public string[] Texts { get; set; } = Array.Empty<string>();
    }

    private sealed class EfCorePersonImplicitNestedAndArrayDto {
        public string FullName { get; set; } = string.Empty;
        public EfCoreAddressDto HomeAddress { get; set; } = null!;
        public EfCorePhoneDto[] Phones { get; set; } = Array.Empty<EfCorePhoneDto>();
    }

    private sealed class EfCorePersonFilteredPhonesDto {
        public IEnumerable<EfCorePhoneDto> Students { get; set; } = Enumerable.Empty<EfCorePhoneDto>();
    }

    private sealed class EfCoreNamedPhonesDto {
        public IEnumerable<EfCorePhoneDto> PhonesRaw { get; set; } = Enumerable.Empty<EfCorePhoneDto>();
        public IEnumerable<EfCorePhoneDto> PhonesMasked { get; set; } = Enumerable.Empty<EfCorePhoneDto>();
    }

    private sealed class EfCorePersonChainedPhonesDto {
        public IEnumerable<EfCorePhoneDto> PhonesOrdered { get; set; } = Enumerable.Empty<EfCorePhoneDto>();
    }

    private sealed class EfCorePhoneProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<EfCorePhone, EfCorePhoneDto>();
        }
    }

    private sealed class EfCoreAddressProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<EfCoreAddress, EfCoreAddressDto>();
        }
    }

    private sealed class EfCorePersonCollectionsProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<EfCorePerson, EfCorePersonCollectionsDto>(x => new EfCorePersonCollectionsDto {
                FullName = x.FirstName + " " + x.LastName,
                PhonesArray = UseMap<ICollection<EfCorePhone>, EfCorePhoneDto[]>(x.Phones),
                PhonesList = UseMap<ICollection<EfCorePhone>, List<EfCorePhoneDto>>(x.Phones)
            });
        }
    }

    private sealed class EfCorePersonImplicitNestedAndArrayProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<EfCorePerson, EfCorePersonImplicitNestedAndArrayDto>(x => new EfCorePersonImplicitNestedAndArrayDto {
                FullName = x.FirstName + " " + x.LastName
            });
        }
    }

    private sealed class EfCorePersonFilteredPhonesProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<EfCorePerson, EfCorePersonFilteredPhonesDto>(x => new EfCorePersonFilteredPhonesDto {
                Students = UseMap<IEnumerable<EfCorePhone>, IEnumerable<EfCorePhoneDto>>(x.Phones.Where(s => s.Number.StartsWith("+44")))
            });
        }
    }

    private sealed class EfCoreNamedPhoneProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<EfCorePhone, EfCorePhoneDto>("Raw", x => new EfCorePhoneDto {
                Number = x.Number
            });

            CreateMap<EfCorePhone, EfCorePhoneDto>("Masked", x => new EfCorePhoneDto {
                Number = x.Number + " [MASKED]"
            });
        }
    }

    private sealed class EfCoreNamedPersonProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<EfCorePerson, EfCoreNamedPhonesDto>(x => new EfCoreNamedPhonesDto {
                PhonesRaw = UseMap<IEnumerable<EfCorePhone>, IEnumerable<EfCorePhoneDto>>("Raw", x.Phones),
                PhonesMasked = UseMap<IEnumerable<EfCorePhone>, IEnumerable<EfCorePhoneDto>>("Masked", x.Phones)
            });
        }
    }

    private sealed class EfCorePersonChainedPhonesProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<EfCorePerson, EfCorePersonChainedPhonesDto>(x => new EfCorePersonChainedPhonesDto {
                PhonesOrdered = UseMap<IEnumerable<EfCorePhone>, IEnumerable<EfCorePhoneDto>>(x.Phones)
                    .OrderBy(dto => dto.Number)
                    .ToList()
            });
        }
    }

    private sealed class EfCoreNamedPersonChainedProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<EfCorePerson, EfCorePersonChainedPhonesDto>(x => new EfCorePersonChainedPhonesDto {
                PhonesOrdered = UseMap<IEnumerable<EfCorePhone>, IEnumerable<EfCorePhoneDto>>("Masked", x.Phones)
                    .OrderBy(dto => dto.Number)
                    .ToList()
            });
        }
    }
}
