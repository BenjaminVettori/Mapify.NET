using Microsoft.EntityFrameworkCore;

namespace Mapify.NET.Tests.EFCore;

public partial class MapifyEfCoreProjectionTests {
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
    public void InstanceMapify_GetRequiredMapWithParameters_ShouldWorkInEfCoreProjection_WhenUsingSelect() {
        var options = new DbContextOptionsBuilder<EfCoreMapifyContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        using var db = new EfCoreMapifyContext(options);
        db.Database.OpenConnection();
        db.Database.EnsureCreated();

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
            new EfCorePersonRuntimeParameterProfile()
        ]);

        var mapExpr = mapify.GetRequiredMap<EfCorePerson, EfCorePersonRuntimeParameterDto>(
            new Dictionary<string, object?> { ["offset"] = 100 }
        );

        var result = db.People
            .OrderBy(x => x.Id)
            .Select(mapExpr)
            .ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(new[] { 101, 102 }, result.Select(x => x.AdjustedId).ToArray());
    }

    [Fact]
    public void InstanceMapify_Map_ShouldFallbackToBaseMap_ForProxyLikeVirtualListsLoadedInMemory() {
        var options = new DbContextOptionsBuilder<EfCoreMapifyContext>()
            .UseLazyLoadingProxies()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        using (var seed = new EfCoreMapifyContext(options)) {
            seed.BillsWithVirtualListBlocks.Add(new EfCoreBillWithVirtualListBlocks {
                Blocks = [
                    new EfCoreVirtualListBlock {
                        CostItems = [
                            new EfCoreVirtualListCostItemType1 { Price = 10m },
                            new EfCoreVirtualListCostItemType2 { TotalPrice = 25m }
                        ]
                    }
                ]
            });

            seed.SaveChanges();
        }

        using var db = new EfCoreMapifyContext(options);

        var loaded = db.BillsWithVirtualListBlocks.Single();

        var blocks = loaded.Blocks!;
        var costItems = blocks.Single().CostItems!;

        Assert.NotEqual(typeof(EfCoreBillWithVirtualListBlocks), loaded.GetType());
        Assert.NotEqual(typeof(EfCoreVirtualListBlock), blocks.Single().GetType());
        Assert.All(costItems, item => {
            Assert.NotEqual(typeof(EfCoreVirtualListCostItemType1), item.GetType());
            Assert.NotEqual(typeof(EfCoreVirtualListCostItemType2), item.GetType());
        });

        var mapify = new Mapify([
            new EfCoreVirtualListCostItemBaseOnlyProfile(),
            new EfCoreVirtualListBlockProfile(),
            new EfCoreVirtualListBillProfile()
        ]);

        var mapped = mapify.Map<EfCoreBillWithVirtualListBlocks, EfCoreBillWithVirtualListBlocksDto>(loaded);

        var items = mapped.Blocks.Single().CostItems.ToArray();
        Assert.Equal(2, items.Length);
        Assert.All(items, item => Assert.NotNull(item));
        Assert.All(items, item => Assert.True(item.Price > 0));
    }
}
