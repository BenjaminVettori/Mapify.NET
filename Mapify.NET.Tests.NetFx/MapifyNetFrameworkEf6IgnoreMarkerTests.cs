using System.Data.Common;
using System.Data.Entity;

namespace Mapify.NET.Tests.NetFx;

public class MapifyNetFrameworkEf6IgnoreMarkerTests {
    [Fact]
    public void IgnoreMarker_ShouldExcludeIgnoredPropertyFromEf6SqlProjection() {
        using var connection = Effort.DbConnectionFactory.CreateTransient();
        using var db = new Ef6IgnoreMapifyContext(connection);
        db.Database.CreateIfNotExists();

        db.Set<Ef6ProjectionIgnoreEntity>().Add(new Ef6ProjectionIgnoreEntity {
            Included = "included",
            IgnoredFromDb = "ignored-db"
        });
        db.SaveChanges();

        var mapify = new Mapify([
            new Ef6ProjectionIgnoreProfile()
        ]);

        var mapExpr = mapify.GetRequiredMap<Ef6ProjectionIgnoreEntity, Ef6ProjectionIgnoreDto>();
        var query = db.Set<Ef6ProjectionIgnoreEntity>().Select(mapExpr);
        var queryText = query.ToString();

        Assert.DoesNotContain("IgnoredFromDb", queryText, StringComparison.OrdinalIgnoreCase);

        var projected = query.Single();
        Assert.Equal("included", projected.Included);
        Assert.Null(projected.IgnoredFromDb);
    }

    [Fact]
    public void IgnoreMarker_ShouldExcludeIgnoredPropertyFromEf6SqlProjection_WhenUsingProjectTo() {
        using var connection = Effort.DbConnectionFactory.CreateTransient();
        using var db = new Ef6IgnoreMapifyContext(connection);
        db.Database.CreateIfNotExists();

        db.Set<Ef6ProjectionIgnoreEntity>().Add(new Ef6ProjectionIgnoreEntity {
            Included = "included",
            IgnoredFromDb = "ignored-db"
        });
        db.SaveChanges();

        var mapify = new Mapify([
            new Ef6ProjectionIgnoreProfile()
        ]);

        var query = db.Set<Ef6ProjectionIgnoreEntity>().ProjectTo<Ef6ProjectionIgnoreDto>(mapify);
        var queryText = query.ToString();

        Assert.DoesNotContain("IgnoredFromDb", queryText, StringComparison.OrdinalIgnoreCase);

        var projected = query.Single();
        Assert.Equal("included", projected.Included);
        Assert.Null(projected.IgnoredFromDb);
    }

    [Fact]
    public void IgnoreMarker_ShouldExcludeIgnoredPropertyFromEf6SqlProjection_WhenUsingSelect() {
        using var connection = Effort.DbConnectionFactory.CreateTransient();
        using var db = new Ef6IgnoreMapifyContext(connection);
        db.Database.CreateIfNotExists();

        db.Set<Ef6ProjectionIgnoreEntity>().Add(new Ef6ProjectionIgnoreEntity {
            Included = "included",
            IgnoredFromDb = "ignored-db"
        });
        db.SaveChanges();

        var mapify = new Mapify([
            new Ef6ProjectionIgnoreProfile()
        ]);

        var mapExpr = mapify.GetRequiredMap<Ef6ProjectionIgnoreEntity, Ef6ProjectionIgnoreDto>();
        var query = db.Set<Ef6ProjectionIgnoreEntity>().Select(mapExpr);
        var queryText = query.ToString();

        Assert.DoesNotContain("IgnoredFromDb", queryText, StringComparison.OrdinalIgnoreCase);

        var projected = query.Single();
        Assert.Equal("included", projected.Included);
        Assert.Null(projected.IgnoredFromDb);
    }

    private sealed class Ef6IgnoreMapifyContext : DbContext {
        public Ef6IgnoreMapifyContext(DbConnection connection)
            : base(connection, true) {
            Database.SetInitializer<Ef6IgnoreMapifyContext>(null);
        }

        public DbSet<Ef6ProjectionIgnoreEntity> ProjectionIgnoreEntities { get; set; } = null!;
    }

    private sealed class Ef6ProjectionIgnoreEntity {
        public int Id { get; set; }
        public string Included { get; set; } = string.Empty;
        public string IgnoredFromDb { get; set; } = string.Empty;
    }

    private sealed class Ef6ProjectionIgnoreDto {
        public string Included { get; set; } = string.Empty;
        public string? IgnoredFromDb { get; set; }
    }

    private sealed class Ef6ProjectionIgnoreProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<Ef6ProjectionIgnoreEntity, Ef6ProjectionIgnoreDto>(x => new Ef6ProjectionIgnoreDto {
                IgnoredFromDb = Ignore<string>()
            });
        }
    }
}
