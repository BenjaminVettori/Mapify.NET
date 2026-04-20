using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

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
    public void InstanceMapify_ProjectTo_ShouldFallbackToBaseMap_ForDerivedRuntimeElementTypes() {
        var mapify = new Mapify([
            new EfCoreProxyLikeBaseProfile()
        ]);

        IQueryable source = new List<EfCoreProxyLikeDerivedSource> {
            new EfCoreProxyLikeDerivedSource { Value = 1 },
            new EfCoreProxyLikeDerivedSource { Value = 2 }
        }.AsQueryable();

        var projected = source
            .ProjectTo<EfCoreProxyLikeDto>(mapify)
            .ToList();

        Assert.Equal(new[] { 2, 3 }, projected.Select(x => x.Value).ToArray());
    }

    [Fact]
    public void InstanceMapify_ProjectToNamed_ShouldFallbackToNamedBaseMap_ForDerivedRuntimeElementTypes() {
        var mapify = new Mapify([
            new EfCoreProxyLikeNamedBaseProfile()
        ]);

        IQueryable source = new List<EfCoreProxyLikeDerivedSource> {
            new EfCoreProxyLikeDerivedSource { Value = 1 },
            new EfCoreProxyLikeDerivedSource { Value = 2 }
        }.AsQueryable();

        var projected = source
            .ProjectTo<EfCoreProxyLikeDto>(mapify, "Offset")
            .ToList();

        Assert.Equal(new[] { 11, 12 }, projected.Select(x => x.Value).ToArray());
    }

    [Fact]
    public void InstanceMapify_ProjectTo_ShouldMapPolymorphicItems_WhenConditionalCollectionBranchIsUsed() {
        var options = new DbContextOptionsBuilder<EfCoreMapifyContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        using var db = new EfCoreMapifyContext(options);

        db.Bills.Add(new EfCoreBill {
            CostItems = [
                new EfCoreCostItemType1 { Price = 10m },
                new EfCoreCostItemType2 { TotalPrice = 25m }
            ]
        });

        db.SaveChanges();

        var mapify = new Mapify([
            new EfCorePolymorphicCostItemProfile(),
            new EfCorePolymorphicBillProfile()
        ]);

        var projected = db.Bills
            .ProjectTo<EfCoreBillDto>(mapify)
            .Single();

        Assert.All(projected.CostItems, item => Assert.NotNull(item));
        Assert.Equal(new[] { 10m, 25m }, projected.CostItems.Select(x => x.Price).ToArray());
    }

    [Fact]
    public void InstanceMapify_ProjectTo_ShouldMapPolymorphicItems_WhenConditionalCollectionBranchIsUsed_InSqliteProjection() {
        var options = new DbContextOptionsBuilder<EfCoreMapifyContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        using var db = new EfCoreMapifyContext(options);
        db.Database.OpenConnection();
        db.Database.EnsureCreated();

        db.Bills.Add(new EfCoreBill {
            CostItems = [
                new EfCoreCostItemType1 { Price = 10m },
                new EfCoreCostItemType2 { TotalPrice = 25m }
            ]
        });

        db.SaveChanges();

        var mapify = new Mapify([
            new EfCorePolymorphicCostItemProfile(),
            new EfCorePolymorphicBillRelationalProfile()
        ]);

        var projected = db.Bills
            .ProjectTo<EfCoreBillDto>(mapify)
            .Single();

        Assert.All(projected.CostItems, item => Assert.NotNull(item));
        Assert.Equal(new[] { 10m, 25m }, projected.CostItems.Select(x => x.Price).ToArray());
    }

    [Fact]
    public void InstanceMapify_ProjectTo_ShouldMapNestedPolymorphicItems_WithoutNullEntries() {
        var options = new DbContextOptionsBuilder<EfCoreMapifyContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        using var db = new EfCoreMapifyContext(options);

        db.BillsWithBlocks.Add(new EfCoreBillWithBlocks {
            Blocks = [
                new EfCoreBlock {
                    CostItems = [
                        new EfCoreBlockCostItemType1 { Price = 1m },
                        new EfCoreBlockCostItemType2 { TotalPrice = 2m },
                        new EfCoreBlockCostItemType1 { Price = 3m },
                        new EfCoreBlockCostItemType2 { TotalPrice = 4m },
                        new EfCoreBlockCostItemType1 { Price = 5m }
                    ]
                }
            ]
        });

        db.SaveChanges();

        var mapify = new Mapify([
            new EfCoreBlockCostItemProfile(),
            new EfCoreBlockProfile(),
            new EfCoreBillWithBlocksProfile()
        ]);

        var projected = db.BillsWithBlocks
            .ProjectTo<EfCoreBillWithBlocksDto>(mapify)
            .Single();

        var block = projected.Blocks.Single();
        var costItems = block.CostItems.ToArray();

        Assert.Equal(5, costItems.Length);
        Assert.All(costItems, item => Assert.NotNull(item));
        Assert.Equal(new[] { 1m, 2m, 3m, 4m, 5m }, costItems.Select(x => x.Price).OrderBy(x => x).ToArray());
    }

    [Fact]
    public void InstanceMapify_ProjectTo_ShouldNotProduceNullCostItems_WhenSiblingBlockHasNullCollection() {
        var options = new DbContextOptionsBuilder<EfCoreMapifyContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        using var db = new EfCoreMapifyContext(options);

        db.BillsWithBlocks.Add(new EfCoreBillWithBlocks {
            Blocks = [
                new EfCoreBlock {
                    CostItems = null
                },
                new EfCoreBlock {
                    CostItems = [
                        new EfCoreBlockCostItemType1 { Price = 1m },
                        new EfCoreBlockCostItemType2 { TotalPrice = 2m },
                        new EfCoreBlockCostItemType1 { Price = 3m },
                        new EfCoreBlockCostItemType2 { TotalPrice = 4m },
                        new EfCoreBlockCostItemType1 { Price = 5m }
                    ]
                }
            ]
        });

        db.SaveChanges();

        var mapify = new Mapify([
            new EfCoreBlockCostItemProfile(),
            new EfCoreBlockProfile(),
            new EfCoreBillWithBlocksProfile()
        ]);

        var projected = db.BillsWithBlocks
            .ProjectTo<EfCoreBillWithBlocksDto>(mapify)
            .Single();

        var blocks = projected.Blocks.ToArray();
        Assert.Equal(2, blocks.Length);

        var nonEmptyBlock = blocks.Single(b => b.CostItems.Any());
        var costItems = nonEmptyBlock.CostItems.ToArray();

        Assert.Equal(5, costItems.Length);
        Assert.All(costItems, item => Assert.NotNull(item));
        Assert.Equal(new[] { 1m, 2m, 3m, 4m, 5m }, costItems.Select(x => x.Price).OrderBy(x => x).ToArray());
    }

    [Fact]
    public void InstanceMapify_ProjectTo_ShouldNotProduceNullCostItems_ForNestedPolymorphicSqliteConditionalProjection() {
        var options = new DbContextOptionsBuilder<EfCoreMapifyContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        using var db = new EfCoreMapifyContext(options);
        db.Database.OpenConnection();
        db.Database.EnsureCreated();

        db.BillsWithBlocks.Add(new EfCoreBillWithBlocks {
            Blocks = [
                new EfCoreBlock {
                    CostItems = [
                        new EfCoreBlockCostItemType1 { Price = 1m },
                        new EfCoreBlockCostItemType2 { TotalPrice = 2m },
                        new EfCoreBlockCostItemType1 { Price = 3m },
                        new EfCoreBlockCostItemType2 { TotalPrice = 4m },
                        new EfCoreBlockCostItemType1 { Price = 5m }
                    ]
                }
            ]
        });

        db.SaveChanges();

        var mapify = new Mapify([
            new EfCoreBlockCostItemProfile(),
            new EfCoreBlockConditionalRelationalProfile(),
            new EfCoreBillWithBlocksConditionalRelationalProfile()
        ]);

        var projected = db.BillsWithBlocks
            .ProjectTo<EfCoreBillWithBlocksDto>(mapify)
            .Single();

        var costItems = projected.Blocks.Single().CostItems.ToArray();

        Assert.Equal(5, costItems.Length);
        Assert.All(costItems, item => Assert.NotNull(item));
        Assert.Equal(new[] { 1m, 2m, 3m, 4m, 5m }, costItems.Select(x => x.Price).OrderBy(x => x).ToArray());
    }

    [Fact]
    public void InstanceMapify_ProjectTo_ShouldMapVirtualListSource_ToIEnumerableTarget_WithoutNullItems() {
        var options = new DbContextOptionsBuilder<EfCoreMapifyContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        using var db = new EfCoreMapifyContext(options);

        db.BillsWithVirtualListBlocks.Add(new EfCoreBillWithVirtualListBlocks {
            Blocks = [
                new EfCoreVirtualListBlock {
                    CostItems = [
                        new EfCoreVirtualListCostItemType1 { Price = 10m },
                        new EfCoreVirtualListCostItemType2 { TotalPrice = 25m },
                        new EfCoreVirtualListCostItemType1 { Price = 30m },
                        new EfCoreVirtualListCostItemType2 { TotalPrice = 45m },
                        new EfCoreVirtualListCostItemType1 { Price = 50m }
                    ]
                }
            ]
        });

        db.SaveChanges();

        var mapify = new Mapify([
            new EfCoreVirtualListCostItemProfile(),
            new EfCoreVirtualListBlockProfile(),
            new EfCoreVirtualListBillProfile()
        ]);

        var projected = db.BillsWithVirtualListBlocks
            .ProjectTo<EfCoreBillWithVirtualListBlocksDto>(mapify)
            .Single();

        var items = projected.Blocks.Single().CostItems.ToArray();

        Assert.Equal(5, items.Length);
        Assert.All(items, item => Assert.NotNull(item));
        Assert.Equal(new[] { 10m, 25m, 30m, 45m, 50m }, items.Select(x => x.Price).OrderBy(x => x).ToArray());
    }

    [Fact]
    public void InstanceMapify_ProjectTo_ShouldNotProduceNullItems_ForExactVirtualListToIEnumerableUserShape() {
        var options = new DbContextOptionsBuilder<EfCoreMapifyContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        using var db = new EfCoreMapifyContext(options);

        var bill = new EfCoreBillWithVirtualListBlocks {
            Blocks = [
                new EfCoreVirtualListBlock {
                    CostItems = [
                        new EfCoreVirtualListCostItemType1 { Price = 10m },
                        new EfCoreVirtualListCostItemType2 { TotalPrice = 25m },
                        new EfCoreVirtualListCostItemType1 { Price = 30m },
                        new EfCoreVirtualListCostItemType2 { TotalPrice = 45m },
                        new EfCoreVirtualListCostItemType1 { Price = 50m }
                    ]
                }
            ]
        };

        db.BillsWithVirtualListBlocks.Add(bill);
        db.SaveChanges();

        var mapify = new Mapify([
            new EfCoreVirtualListCostItemProfile(),
            new EfCoreVirtualListBlockExactUserShapeProfile(),
            new EfCoreVirtualListBillExactUserShapeProfile()
        ]);

        var projected = db.BillsWithVirtualListBlocks
            .Where(x => x.Id == bill.Id)
            .ProjectTo<EfCoreBillWithVirtualListBlocksDto>(mapify)
            .Single();

        var items = projected.Blocks.Single().CostItems.ToArray();

        Assert.Equal(5, items.Length);
        Assert.All(items, item => Assert.NotNull(item));
        Assert.Equal(new[] { 10m, 25m, 30m, 45m, 50m }, items.Select(x => x.Price).OrderBy(x => x).ToArray());
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
    public void InstanceMapify_ProjectTo_ShouldApplyNestedNullFallback_ForNullableAndNonNullableTargets() {
        var options = new DbContextOptionsBuilder<EfCoreMapifyContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        using var db = new EfCoreMapifyContext(options);
        db.Database.OpenConnection();
        db.Database.EnsureCreated();

        db.People.AddRange(
            new EfCorePerson {
                FirstName = "Has",
                LastName = "Street",
                HomeAddress = new EfCoreAddress {
                    City = "Paris",
                    Street = new EfCoreStreet { Number = 77 }
                }
            },
            new EfCorePerson {
                FirstName = "No",
                LastName = "Street",
                HomeAddress = new EfCoreAddress {
                    City = "Rome",
                    Street = null
                }
            },
            new EfCorePerson {
                FirstName = "No",
                LastName = "Address",
                HomeAddress = null!
            }
        );

        db.SaveChanges();

        var mapify = new Mapify([
            new EfCorePersonStreetNumberProfile(),
            new EfCorePersonStreetNullableNumberProfile()
        ]);

        var nonNullableProjection = db.People
            .OrderBy(x => x.Id)
            .ProjectTo<EfCorePersonStreetNumberDto>(mapify)
            .Select(x => x.StreetNumber)
            .OrderBy(x => x)
            .ToArray();

        var nullableProjection = db.People
            .OrderBy(x => x.Id)
            .ProjectTo<EfCorePersonStreetNullableNumberDto>(mapify)
            .Select(x => x.StreetNumber)
            .OrderBy(x => x)
            .ToArray();

        Assert.Equal(new[] { 0, 0, 77 }, nonNullableProjection);
        Assert.Equal(new int?[] { null, null, 77 }, nullableProjection);
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

    [Fact]
    public void InstanceMapify_ProjectToRecursive_ShouldUseDefaultDepthSix() {
        var options = new DbContextOptionsBuilder<EfCoreMapifyContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        using var db = new EfCoreMapifyContext(options);

        var root = BuildRecursiveTree(8);
        db.RecursiveNodes.Add(root);
        db.SaveChanges();

        var mapify = new Mapify([new EfCoreRecursiveNodeDefaultDepthProfile()]);

        var projected = db.RecursiveNodes
            .Where(x => x.ParentId == null)
            .ProjectTo<EfCoreRecursiveNodeDto>(mapify)
            .Single();

        Assert.Equal(6, CountProjectedDepth(projected));
    }

    [Fact]
    public void InstanceMapify_ProjectToRecursive_ShouldUseExplicitMarkerDepth() {
        var options = new DbContextOptionsBuilder<EfCoreMapifyContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        using var db = new EfCoreMapifyContext(options);

        var root = BuildRecursiveTree(8);
        db.RecursiveNodes.Add(root);
        db.SaveChanges();

        var mapify = new Mapify([new EfCoreRecursiveNodeDepthThreeProfile()]);

        var projected = db.RecursiveNodes
            .Where(x => x.ParentId == null)
            .ProjectTo<EfCoreRecursiveNodeDto>(mapify, "DepthThree")
            .Single();

        Assert.Equal(3, CountProjectedDepth(projected));
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenRecursiveProjectToDepthExceedsHardCap() {
        var mapify = new Mapify((IEnumerable<MapifyProfile>?)null);
        RegisterDepthElevenRecursiveMap(mapify);

        var buildMethod = typeof(Mapify).GetMethod("BuildRegisteredMaps", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        var tie = Assert.Throws<System.Reflection.TargetInvocationException>(() => buildMethod.Invoke(mapify, null));
        var ex = Assert.IsType<InvalidOperationException>(tie.InnerException);
        Assert.Contains("exceeds the configured hard cap 10", ex.Message, StringComparison.Ordinal);
    }

    private static EfCoreRecursiveNode BuildRecursiveTree(int depth) {
        var root = new EfCoreRecursiveNode { Name = "N1" };
        var current = root;
        for (var i = 2; i <= depth; i++) {
            var child = new EfCoreRecursiveNode {
                Name = $"N{i}",
                Parent = current
            };

            current.Children.Add(child);
            current = child;
        }

        return root;
    }

    private static int CountProjectedDepth(EfCoreRecursiveNodeDto node) {
        var depth = 0;
        var current = node;
        while (current != null && depth < 100) {
            depth++;
            current = current.Children.FirstOrDefault();
        }

        return depth;
    }

    private static void RegisterDepthElevenRecursiveMap(Mapify mapify) {
        var sourceParameter = Expression.Parameter(typeof(EfCoreRecursiveNode), "x");
        var sourceChildren = Expression.Property(sourceParameter, nameof(EfCoreRecursiveNode.Children));

        var useMapMarker = typeof(MapifyProfile)
            .GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
            .Single(m => m.Name == "UseMap"
                && m.IsGenericMethodDefinition
                && m.GetParameters().Length == 2
                && m.GetParameters()[1].ParameterType == typeof(int))
            .MakeGenericMethod(typeof(ICollection<EfCoreRecursiveNode>), typeof(List<EfCoreRecursiveNodeDto>));

        var useMapCall = Expression.Call(useMapMarker, sourceChildren, Expression.Constant(11));

        var body = Expression.MemberInit(
            Expression.New(typeof(EfCoreRecursiveNodeDto)),
            Expression.Bind(
                typeof(EfCoreRecursiveNodeDto).GetProperty(nameof(EfCoreRecursiveNodeDto.Name))!,
                Expression.Property(sourceParameter, nameof(EfCoreRecursiveNode.Name))
            ),
            Expression.Bind(
                typeof(EfCoreRecursiveNodeDto).GetProperty(nameof(EfCoreRecursiveNodeDto.Children))!,
                useMapCall
            )
        );

        var partial = Expression.Lambda<Func<EfCoreRecursiveNode, EfCoreRecursiveNodeDto>>(body, sourceParameter);

        var addPendingMap = typeof(Mapify)
            .GetMethod("AddPendingMap", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .MakeGenericMethod(typeof(EfCoreRecursiveNode), typeof(EfCoreRecursiveNodeDto));
        addPendingMap.Invoke(mapify, [null, partial]);
    }
}
