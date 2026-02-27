using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using LinqKit;

namespace Mapify.NET.Tests.EFCore;

public class MapifyEfCoreTests {
    [Fact]
    public void IgnoreMarker_ShouldExcludeIgnoredPropertyFromEfCoreSqlProjection() {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<EfCoreMapifyContext>()
            .UseSqlite(connection)
            .Options;

        using var db = new EfCoreMapifyContext(options);
        db.Database.EnsureCreated();

        db.Set<EfCoreProjectionIgnoreEntity>().Add(new EfCoreProjectionIgnoreEntity {
            Included = "included",
            IgnoredFromDb = "ignored-db"
        });
        db.SaveChanges();

        var mapify = new Mapify([
            new EfCoreProjectionIgnoreProfile()
        ]);

        var mapExpr = mapify.GetRequiredMap<EfCoreProjectionIgnoreEntity, EfCoreProjectionIgnoreDto>();
        var query = db.Set<EfCoreProjectionIgnoreEntity>().Select(mapExpr);
        var sql = query.ToQueryString();

        Assert.DoesNotContain("\"IgnoredFromDb\"", sql, StringComparison.Ordinal);

        var projected = query.Single();
        Assert.Equal("included", projected.Included);
        Assert.Null(projected.IgnoredFromDb);
    }

    [Fact]
    public void IgnoreMarker_ShouldExcludeIgnoredPropertyFromEfCoreSqlProjection_WhenUsingProjectTo() {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<EfCoreMapifyContext>()
            .UseSqlite(connection)
            .Options;

        using var db = new EfCoreMapifyContext(options);
        db.Database.EnsureCreated();

        db.Set<EfCoreProjectionIgnoreEntity>().Add(new EfCoreProjectionIgnoreEntity {
            Included = "included",
            IgnoredFromDb = "ignored-db"
        });
        db.SaveChanges();

        var mapify = new Mapify([
            new EfCoreProjectionIgnoreProfile()
        ]);

        var query = db.Set<EfCoreProjectionIgnoreEntity>().ProjectTo<EfCoreProjectionIgnoreDto>(mapify);
        var sql = query.ToQueryString();

        Assert.DoesNotContain("\"IgnoredFromDb\"", sql, StringComparison.Ordinal);

        var projected = query.Single();
        Assert.Equal("included", projected.Included);
        Assert.Null(projected.IgnoredFromDb);
    }

    [Fact]
    public void IgnoreMarker_ShouldExcludeIgnoredPropertyFromEfCoreSqlProjection_WhenUsingSelect() {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<EfCoreMapifyContext>()
            .UseSqlite(connection)
            .Options;

        using var db = new EfCoreMapifyContext(options);
        db.Database.EnsureCreated();

        db.Set<EfCoreProjectionIgnoreEntity>().Add(new EfCoreProjectionIgnoreEntity {
            Included = "included",
            IgnoredFromDb = "ignored-db"
        });
        db.SaveChanges();

        var mapify = new Mapify([
            new EfCoreProjectionIgnoreProfile()
        ]);

        var mapExpr = mapify.GetRequiredMap<EfCoreProjectionIgnoreEntity, EfCoreProjectionIgnoreDto>();
        var query = db.Set<EfCoreProjectionIgnoreEntity>().Select(mapExpr);
        var sql = query.ToQueryString();

        Assert.DoesNotContain("\"IgnoredFromDb\"", sql, StringComparison.Ordinal);

        var projected = query.Single();
        Assert.Equal("included", projected.Included);
        Assert.Null(projected.IgnoredFromDb);
    }

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

        var mapExpr = mapify.GetRequiredMap<EfCorePerson, EfCorePersonCollectionsDto>();

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
                Phones = [
                    new EfCorePhone { Number = "+44-100" },
                    new EfCorePhone { Number = "+44-101" },
                    new EfCorePhone { Number = "+1-300" }
                ]
            },
            new EfCorePerson {
                FirstName = "Alan",
                LastName = "Turing",
                HomeAddress = new EfCoreAddress { City = "Manchester" },
                Phones = [
                    new EfCorePhone { Number = "+44-200" },
                    new EfCorePhone { Number = "+1-999" }
                ]
            }
        );

        db.SaveChanges();

        var mapify = new Mapify([
            new EfCorePhoneProfile(),
            new EfCorePersonFilteredPhonesProfile()
        ]);

        var mapExpr = mapify.GetRequiredMap<EfCorePerson, EfCorePersonFilteredPhonesDto>();

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
            Phones = [
                new EfCorePhone { Number = "+44-100" },
                new EfCorePhone { Number = "+44-101" }
            ]
        });

        db.SaveChanges();

        var mapify = new Mapify([
            new EfCoreNamedPhoneProfile(),
            new EfCoreNamedPersonProfile()
        ]);

        var mapExpr = mapify.GetRequiredMap<EfCorePerson, EfCoreNamedPhonesDto>();

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
            Phones = [
                new EfCorePhone { Number = "+44-300" },
                new EfCorePhone { Number = "+44-100" },
                new EfCorePhone { Number = "+44-200" }
            ]
        });

        db.SaveChanges();

        var mapify = new Mapify([
            new EfCorePhoneProfile(),
            new EfCorePersonChainedPhonesProfile()
        ]);

        var mapExpr = mapify.GetRequiredMap<EfCorePerson, EfCorePersonChainedPhonesDto>();

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
            Phones = [
                new EfCorePhone { Number = "+44-300" },
                new EfCorePhone { Number = "+44-100" }
            ]
        });

        db.SaveChanges();

        var mapify = new Mapify([
            new EfCoreNamedPhoneProfile(),
            new EfCoreNamedPersonChainedProfile()
        ]);

        var mapExpr = mapify.GetRequiredMap<EfCorePerson, EfCorePersonChainedPhonesDto>();

        var result = db.People
            .Select(mapExpr)
            .Single();

        Assert.Equal(new[] { "+44-100 [MASKED]", "+44-300 [MASKED]" }, result.PhonesOrdered.Select(x => x.Number).ToArray());
    }

    [Fact]
    public void InstanceMapify_UseMap_ShouldSupportCalculations_InEfCoreProjection() {
        var options = new DbContextOptionsBuilder<EfCoreMapifyContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        using var db = new EfCoreMapifyContext(options);

        db.People.AddRange(
            new EfCorePerson {
                FirstName = "Ada",
                LastName = "Lovelace",
                HomeAddress = new EfCoreAddress { City = "London" }
            },
            new EfCorePerson {
                FirstName = "Alan",
                LastName = "Turing",
                HomeAddress = new EfCoreAddress { City = "Manchester" }
            }
        );

        db.SaveChanges();

        var mapify = new Mapify([
            new EfCoreIntIdentityProfile(),
            new EfCorePersonCalculationProfile()
        ]);

        var mapExpr = mapify.GetRequiredMap<EfCorePerson, EfCorePersonCalculationDto>();

        var result = db.People
            .OrderBy(x => x.Id)
            .Select(mapExpr)
            .ToList();

        Assert.All(result, x => Assert.Equal(x.Id * 365, x.AgeInDays));
    }

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

    private sealed class EfCoreMapifyContext(DbContextOptions<EfCoreMapifyContext> options) : DbContext(options) {
        public DbSet<EfCorePerson> People => Set<EfCorePerson>();
        public DbSet<EfCoreAddress> Addresses => Set<EfCoreAddress>();
        public DbSet<EfCorePhone> Phones => Set<EfCorePhone>();
        public DbSet<EfCoreProjectionIgnoreEntity> ProjectionIgnoreEntities => Set<EfCoreProjectionIgnoreEntity>();
    }

    private sealed class EfCoreProjectionIgnoreEntity {
        public int Id { get; set; }
        public string Included { get; set; } = string.Empty;
        public string IgnoredFromDb { get; set; } = string.Empty;
    }

    private sealed class EfCorePerson {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public int? HomeAddressId { get; set; }
        public EfCoreAddress HomeAddress { get; set; } = null!;
        public ICollection<EfCorePhone> Phones { get; set; } = [];
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
        public List<EfCorePhoneDto> Phones { get; set; } = [];
    }

    private sealed class EfCoreAddressDto {
        public string City { get; set; } = string.Empty;
    }

    private sealed class EfCorePhoneDto {
        public string Number { get; set; } = string.Empty;
    }

    private sealed class EfCorePersonCollectionsDto {
        public string FullName { get; set; } = string.Empty;
        public EfCorePhoneDto[] PhonesArray { get; set; } = [];
        public List<EfCorePhoneDto> PhonesList { get; set; } = [];
    }

    private sealed class EfCorePrimitiveCollectionsSource {
        public int[] Numbers { get; set; } = [];
        public ICollection<string> Texts { get; set; } = [];
    }

    private sealed class EfCorePrimitiveCollectionsDto {
        public List<int> Numbers { get; set; } = [];
        public string[] Texts { get; set; } = [];
    }

    private sealed class EfCorePersonImplicitNestedAndArrayDto {
        public string FullName { get; set; } = string.Empty;
        public EfCoreAddressDto HomeAddress { get; set; } = null!;
        public EfCorePhoneDto[] Phones { get; set; } = [];
    }

    private sealed class EfCorePersonFilteredPhonesDto {
        public IEnumerable<EfCorePhoneDto> Students { get; set; } = [];
    }

    private sealed class EfCoreNamedPhonesDto {
        public IEnumerable<EfCorePhoneDto> PhonesRaw { get; set; } = [];
        public IEnumerable<EfCorePhoneDto> PhonesMasked { get; set; } = [];
    }

    private sealed class EfCorePersonChainedPhonesDto {
        public IEnumerable<EfCorePhoneDto> PhonesOrdered { get; set; } = [];
    }

    private sealed class EfCorePersonCalculationDto {
        public int Id { get; set; }
        public int AgeInDays { get; set; }
    }

    private sealed class EfCoreProjectToNamedPhonesDto {
        public IEnumerable<EfCorePhoneDto> Phones { get; set; } = [];
    }

    private sealed class EfCoreProjectionIgnoreDto {
        public string Included { get; set; } = string.Empty;
        public string? IgnoredFromDb { get; set; }
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

    private sealed class EfCoreIntIdentityProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<int, int>(x => x);
        }
    }

    private sealed class EfCorePersonCalculationProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<EfCorePerson, EfCorePersonCalculationDto>(x => new EfCorePersonCalculationDto {
                AgeInDays = 365 * UseMap<int, int>(x.Id)
            });
        }
    }

    private sealed class EfCoreNamedProjectToPersonProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<EfCorePerson, EfCoreProjectToNamedPhonesDto>("Raw", x => new EfCoreProjectToNamedPhonesDto {
                Phones = x.Phones.ProjectTo<EfCorePhoneDto>("Raw").ToList()
            });

            CreateMap<EfCorePerson, EfCoreProjectToNamedPhonesDto>("Masked", x => new EfCoreProjectToNamedPhonesDto {
                Phones = x.Phones.ProjectTo<EfCorePhoneDto>("Masked").ToList()
            });
        }
    }

    private sealed class EfCoreNamedNestedProjectToPersonProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<EfCorePerson, EfCoreProjectToNamedPhonesDto>("Masked", x => new EfCoreProjectToNamedPhonesDto {
                Phones = x.Phones.ProjectTo<EfCorePhoneDto>("Masked")
                    .OrderBy(dto => dto.Number)
                    .ToList()
            });
        }
    }

    private sealed class EfCoreProjectionIgnoreProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<EfCoreProjectionIgnoreEntity, EfCoreProjectionIgnoreDto>(x => new EfCoreProjectionIgnoreDto {
                IgnoredFromDb = Ignore<string?>()
            });
        }
    }

}
