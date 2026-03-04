namespace Mapify.NET.Tests.NetFx;

public partial class MapifyNetFrameworkEf6ProjectionTests {
    [Fact]
    public void InstanceMapify_UseMap_ShouldWorkInEf6Projection_ForEnumerableCollections() {
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

        var mapExpr = mapify.GetRequiredMap<Ef6Person, Ef6PersonCollectionsDto>();

        var result = db.People
            .OrderBy(x => x.Id)
            .Select(mapExpr)
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
    public void InstanceMapify_UseMap_ShouldThrowForArrayProjection_InEf6Projection() {
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
                new Ef6PersonArrayCollectionsProfile()
        ]);

        var mapExpr = mapify.GetMap<Ef6Person, Ef6PersonArrayCollectionsDto>();

        // EF6 cannot translate array materialization in projection pipelines.
        Assert.Throws<NotSupportedException>(() => db.People
            .OrderBy(x => x.Id)
            .Select(mapExpr)
            .ToList());
    }

    [Fact]
    public void InstanceMapify_UseMap_ShouldAcceptFilterExpressions_InEf6Projection() {
        using var connection = Effort.DbConnectionFactory.CreateTransient();
        using var db = new Ef6MapifyContext(connection);
        db.Database.CreateIfNotExists();

        db.People.Add(new Ef6Person {
            FirstName = "Ada",
            LastName = "Lovelace",
            HomeAddress = new Ef6Address { City = "London" },
            Phones = [
                new Ef6Phone { Number = "+44-100" },
                new Ef6Phone { Number = "+44-101" },
                new Ef6Phone { Number = "+1-999" }
            ]
        });

        db.People.Add(new Ef6Person {
            FirstName = "Alan",
            LastName = "Turing",
            HomeAddress = new Ef6Address { City = "Manchester" },
            Phones = [
                new Ef6Phone { Number = "+44-200" },
                new Ef6Phone { Number = "+1-123" }
            ]
        });

        db.SaveChanges();

        var mapify = new Mapify([
            new Ef6PhoneProfile(),
            new Ef6PersonFilteredPhonesProfile()
        ]);

        var mapExpr = mapify.GetRequiredMap<Ef6Person, Ef6PersonFilteredPhonesDto>();

        var result = db.People
            .OrderBy(x => x.Id)
            .Select(mapExpr)
            .ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(["+44-100", "+44-101"], result[0].Students.Select(x => x.Number).ToArray());
        Assert.Equal(["+44-200"], result[1].Students.Select(x => x.Number).ToArray());
    }

    [Fact]
    public void InstanceMapify_UseMap_ShouldSupportNamedMappings_InEf6Projection() {
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
            new Ef6NamedPersonProfile()
        ]);

        var mapExpr = mapify.GetRequiredMap<Ef6Person, Ef6NamedPhonesDto>();

        var result = db.People
            .OrderBy(x => x.Id)
            .Select(mapExpr)
            .Single();

        Assert.Equal(["+44-100", "+44-101"], result.PhonesRaw.Select(x => x.Number).ToArray());
        Assert.Equal(["+44-100 [MASKED]", "+44-101 [MASKED]"], result.PhonesMasked.Select(x => x.Number).ToArray());
    }

    [Fact]
    public void InstanceMapify_UseMap_ShouldAllowChaining_InEf6Projection() {
        using var connection = Effort.DbConnectionFactory.CreateTransient();
        using var db = new Ef6MapifyContext(connection);
        db.Database.CreateIfNotExists();

        db.People.Add(new Ef6Person {
            FirstName = "Ada",
            LastName = "Lovelace",
            HomeAddress = new Ef6Address { City = "London" },
            Phones = [
                new Ef6Phone { Number = "+44-300" },
                new Ef6Phone { Number = "+44-100" },
                new Ef6Phone { Number = "+44-200" }
            ]
        });

        db.SaveChanges();

        var mapify = new Mapify([
            new Ef6PhoneProfile(),
            new Ef6PersonChainedPhonesProfile()
        ]);

        var mapExpr = mapify.GetRequiredMap<Ef6Person, Ef6PersonChainedPhonesDto>();

        var result = db.People
            .Select(mapExpr)
            .Single();

        Assert.Equal(["+44-100", "+44-200", "+44-300"], result.PhonesOrdered.Select(x => x.Number).ToArray());
    }

    [Fact]
    public void InstanceMapify_UseMapNamed_ShouldAllowChaining_InEf6Projection() {
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
            new Ef6NamedPersonChainedProfile()
        ]);

        var mapExpr = mapify.GetRequiredMap<Ef6Person, Ef6NamedPersonChainedPhonesDto>();

        var result = db.People
            .Select(mapExpr)
            .Single();

        Assert.Equal(["+44-100 [MASKED]", "+44-300 [MASKED]"], result.PhonesOrdered.Select(x => x.Number).ToArray());
    }

    [Fact]
    public void InstanceMapify_UseMap_ShouldSupportCalculations_InEf6Projection() {
        using var connection = Effort.DbConnectionFactory.CreateTransient();
        using var db = new Ef6MapifyContext(connection);
        db.Database.CreateIfNotExists();

        db.People.Add(new Ef6Person {
            FirstName = "Ada",
            LastName = "Lovelace",
            HomeAddress = new Ef6Address { City = "London" }
        });

        db.People.Add(new Ef6Person {
            FirstName = "Alan",
            LastName = "Turing",
            HomeAddress = new Ef6Address { City = "Manchester" }
        });

        db.SaveChanges();

        var mapify = new Mapify([
            new Ef6IntIdentityProfile(),
            new Ef6PersonCalculationProfile()
        ]);

        var mapExpr = mapify.GetRequiredMap<Ef6Person, Ef6PersonCalculationDto>();

        var result = db.People
            .OrderBy(x => x.Id)
            .Select(mapExpr)
            .ToList();

        Assert.All(result, x => Assert.Equal(x.Id * 365, x.AgeInDays));
    }
}
