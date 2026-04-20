namespace Mapify.NET.Tests.NetFx;

using System.Linq.Expressions;

public partial class MapifyNetFrameworkEf6ProjectionTests {
    [Fact]
    public void InstanceMapify_ProjectTo_ShouldWorkInEf6Projection() {
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
            new Ef6PhoneProfile(),
            new Ef6PersonCollectionsProfile()
        ]);

        var result = db.People
            .OrderBy(x => x.Id)
            .ProjectTo<Ef6PersonCollectionsDto>(mapify)
            .ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal("Ada Lovelace", result[0].FullName);
        Assert.Equal(["+44-100", "+44-101"], result[0].PhonesList.Select(x => x.Number).ToArray());
        Assert.Equal(["+44-100", "+44-101"], result[0].PhonesEnumerable.Select(x => x.Number).ToArray());

        Assert.Equal("Alan Turing", result[1].FullName);
        Assert.Equal(["+44-200"], result[1].PhonesList.Select(x => x.Number).ToArray());
        Assert.Equal(["+44-200"], result[1].PhonesEnumerable.Select(x => x.Number).ToArray());
    }

    [Fact]
    public void InstanceMapify_ProjectToNamed_ShouldWorkInEf6Projection() {
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

        db.SaveChanges();

        var mapify = new Mapify([
            new Ef6NamedPhoneProfile(),
            new Ef6NamedProjectToPersonProfile()
        ]);

        var result = db.People
            .ProjectTo<Ef6ProjectToNamedPhonesDto>(mapify, "Masked")
            .Single();

        Assert.Equal(["+44-100 [MASKED]", "+44-101 [MASKED]"], result.Phones.Select(x => x.Number).ToArray());
    }

    [Fact]
    public void InstanceMapify_ProjectTo_ShouldMapPolymorphicItems_WhenConditionalCollectionBranchIsUsed() {
        using var connection = Effort.DbConnectionFactory.CreateTransient();
        using var db = new Ef6MapifyContext(connection);
        db.Database.CreateIfNotExists();

        db.Bills.Add(new Ef6Bill {
            CostItems = [
                new Ef6CostItemType1 { Price = 10m },
                new Ef6CostItemType2 { TotalPrice = 25m }
            ]
        });

        db.SaveChanges();

        var mapify = new Mapify([
            new Ef6PolymorphicCostItemProfile(),
            new Ef6PolymorphicBillProfile()
        ]);

        var projected = db.Bills
            .AsEnumerable()
            .ProjectTo<Ef6BillDto>(mapify)
            .Single();

        Assert.All(projected.CostItems, item => Assert.NotNull(item));
        Assert.Equal([10m, 25m], projected.CostItems.Select(x => x.Price).ToArray());
    }

    [Fact]
    public void InstanceMapify_NestedNamedProjectToMarker_ShouldWorkInEf6Projection() {
        using var connection = Effort.DbConnectionFactory.CreateTransient();
        using var db = new Ef6MapifyContext(connection);
        db.Database.CreateIfNotExists();

        db.People.Add(new Ef6Person {
            FirstName = "Ada",
            LastName = "Lovelace",
            HomeAddress = new Ef6Address { City = "London" },
            Phones = [
                new Ef6Phone { Number = "+44-300" },
                new Ef6Phone { Number = "+44-100" }
            ]
        });

        db.SaveChanges();

        var mapify = new Mapify([
            new Ef6NamedPhoneProfile(),
            new Ef6NamedNestedProjectToPersonProfile()
        ]);

        var mapExpr = mapify.GetMap<Ef6Person, Ef6ProjectToNamedPhonesDto>("MaskedNested");

        var result = db.People
            .Select(mapExpr)
            .Single();

        Assert.Equal(["+44-100 [MASKED]", "+44-300 [MASKED]"], result.Phones.Select(x => x.Number).ToArray());
    }

    [Fact]
    public void InstanceMapify_ProjectToRecursive_ShouldUseDefaultDepthSix() {
        using var connection = Effort.DbConnectionFactory.CreateTransient();
        using var db = new Ef6MapifyContext(connection);
        db.Database.CreateIfNotExists();

        var root = BuildRecursiveTree(8);
        db.RecursiveNodes.Add(root);
        db.SaveChanges();

        var mapify = new Mapify([new Ef6RecursiveNodeDefaultDepthProfile()]);

        var projected = db.RecursiveNodes
            .Where(x => x.ParentId == null)
            .AsEnumerable()
            .ProjectTo<Ef6RecursiveNodeDto>(mapify)
            .Single();

        Assert.Equal(6, CountProjectedDepth(projected));
    }

    [Fact]
    public void InstanceMapify_ProjectToRecursive_ShouldUseExplicitMarkerDepth() {
        using var connection = Effort.DbConnectionFactory.CreateTransient();
        using var db = new Ef6MapifyContext(connection);
        db.Database.CreateIfNotExists();

        var root = BuildRecursiveTree(8);
        db.RecursiveNodes.Add(root);
        db.SaveChanges();

        var mapify = new Mapify([new Ef6RecursiveNodeDepthThreeProfile()]);

        var projected = db.RecursiveNodes
            .Where(x => x.ParentId == null)
            .AsEnumerable()
            .ProjectTo<Ef6RecursiveNodeDto>(mapify, "DepthThree")
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

    private static Ef6RecursiveNode BuildRecursiveTree(int depth) {
        var root = new Ef6RecursiveNode { Name = "N1" };
        var current = root;
        for (var i = 2; i <= depth; i++) {
            var child = new Ef6RecursiveNode {
                Name = $"N{i}",
                Parent = current
            };

            current.Children.Add(child);
            current = child;
        }

        return root;
    }

    private static int CountProjectedDepth(Ef6RecursiveNodeDto node) {
        var depth = 0;
        var current = node;
        while (current != null && depth < 100) {
            depth++;
            current = current.Children.FirstOrDefault();
        }

        return depth;
    }

    private static void RegisterDepthElevenRecursiveMap(Mapify mapify) {
        var sourceParameter = Expression.Parameter(typeof(Ef6RecursiveNode), "x");
        var sourceChildren = Expression.Property(sourceParameter, nameof(Ef6RecursiveNode.Children));

        var useMapMarker = typeof(MapifyProfile)
            .GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
            .Single(m => m.Name == "UseMap"
                && m.IsGenericMethodDefinition
                && m.GetParameters().Length == 2
                && m.GetParameters()[1].ParameterType == typeof(int))
            .MakeGenericMethod(typeof(ICollection<Ef6RecursiveNode>), typeof(List<Ef6RecursiveNodeDto>));

        var useMapCall = Expression.Call(useMapMarker, sourceChildren, Expression.Constant(11));

        var body = Expression.MemberInit(
            Expression.New(typeof(Ef6RecursiveNodeDto)),
            Expression.Bind(
                typeof(Ef6RecursiveNodeDto).GetProperty(nameof(Ef6RecursiveNodeDto.Name))!,
                Expression.Property(sourceParameter, nameof(Ef6RecursiveNode.Name))
            ),
            Expression.Bind(
                typeof(Ef6RecursiveNodeDto).GetProperty(nameof(Ef6RecursiveNodeDto.Children))!,
                useMapCall
            )
        );

        var partial = Expression.Lambda<Func<Ef6RecursiveNode, Ef6RecursiveNodeDto>>(body, sourceParameter);

        var addPendingMap = typeof(Mapify)
            .GetMethod("AddPendingMap", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .MakeGenericMethod(typeof(Ef6RecursiveNode), typeof(Ef6RecursiveNodeDto));
        addPendingMap.Invoke(mapify, [null, partial]);
    }
}
