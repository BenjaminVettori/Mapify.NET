namespace Mapify.NET.Tests.NetFx;

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
}
