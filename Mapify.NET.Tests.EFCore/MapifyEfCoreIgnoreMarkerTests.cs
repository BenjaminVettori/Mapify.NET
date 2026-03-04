using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Mapify.NET.Tests.EFCore;

public class MapifyEfCoreIgnoreMarkerTests {
    [Fact]
    public void IgnoreMarker_ShouldExcludeIgnoredPropertyFromEfCoreSqlProjection() {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<EfCoreIgnoreMapifyContext>()
            .UseSqlite(connection)
            .Options;

        using var db = new EfCoreIgnoreMapifyContext(options);
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

        var options = new DbContextOptionsBuilder<EfCoreIgnoreMapifyContext>()
            .UseSqlite(connection)
            .Options;

        using var db = new EfCoreIgnoreMapifyContext(options);
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

        var options = new DbContextOptionsBuilder<EfCoreIgnoreMapifyContext>()
            .UseSqlite(connection)
            .Options;

        using var db = new EfCoreIgnoreMapifyContext(options);
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

    private sealed class EfCoreIgnoreMapifyContext(DbContextOptions<EfCoreIgnoreMapifyContext> options) : DbContext(options) {
        public DbSet<EfCoreProjectionIgnoreEntity> ProjectionIgnoreEntities => Set<EfCoreProjectionIgnoreEntity>();
    }

    private sealed class EfCoreProjectionIgnoreEntity {
        public int Id { get; set; }
        public string Included { get; set; } = string.Empty;
        public string IgnoredFromDb { get; set; } = string.Empty;
    }

    private sealed class EfCoreProjectionIgnoreDto {
        public string Included { get; set; } = string.Empty;
        public string? IgnoredFromDb { get; set; }
    }

    private sealed class EfCoreProjectionIgnoreProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<EfCoreProjectionIgnoreEntity, EfCoreProjectionIgnoreDto>(x => new EfCoreProjectionIgnoreDto {
                IgnoredFromDb = Ignore<string?>()
            });
        }
    }
}
