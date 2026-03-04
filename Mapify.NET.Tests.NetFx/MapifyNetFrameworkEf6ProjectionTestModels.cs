using System.Data.Common;
using System.Data.Entity;

namespace Mapify.NET.Tests.NetFx;

public partial class MapifyNetFrameworkEf6ProjectionTests {
    public class Ef6MapifyContext : DbContext {
        public Ef6MapifyContext(DbConnection connection)
            : base(connection, true) {
            Database.SetInitializer<Ef6MapifyContext>(null);
        }

        public DbSet<Ef6Person> People { get; set; } = null!;
        public DbSet<Ef6Address> Addresses { get; set; } = null!;
        public DbSet<Ef6Phone> Phones { get; set; } = null!;
        public DbSet<Ef6ProjectionIgnoreEntity> ProjectionIgnoreEntities { get; set; } = null!;
    }

    public class Ef6ProjectionIgnoreEntity {
        public int Id { get; set; }
        public string Included { get; set; } = string.Empty;
        public string IgnoredFromDb { get; set; } = string.Empty;
    }

    public class Ef6Person {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public int? HomeAddressId { get; set; }
        public Ef6Address HomeAddress { get; set; } = null!;
        public ICollection<Ef6Phone> Phones { get; set; } = [];
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
        public List<Ef6PhoneDto> Phones { get; set; } = [];
    }

    public class Ef6AddressDto {
        public string City { get; set; } = string.Empty;
    }

    public class Ef6PhoneDto {
        public string Number { get; set; } = string.Empty;
    }

    public class Ef6PersonCollectionsDto {
        public string FullName { get; set; } = string.Empty;
        public List<Ef6PhoneDto> PhonesList { get; set; } = [];
        public IEnumerable<Ef6PhoneDto> PhonesEnumerable { get; set; } = [];
    }

    public class Ef6PersonArrayCollectionsDto {
        public string FullName { get; set; } = string.Empty;
        public Ef6PhoneDto[] PhonesArray { get; set; } = [];
        public List<Ef6PhoneDto> PhonesList { get; set; } = [];
    }

    public class Ef6PrimitiveCollectionsSource {
        public ICollection<int> Numbers { get; set; } = [];
        public ICollection<string> Texts { get; set; } = [];
    }

    public class Ef6PrimitiveArrayCollectionsSource {
        public int[] Numbers { get; set; } = [];
        public ICollection<string> Texts { get; set; } = [];
    }

    public class Ef6PrimitiveCollectionsDto {
        public List<int> Numbers { get; set; } = [];
        public IEnumerable<string> Texts { get; set; } = [];
    }

    public class Ef6PrimitiveArrayCollectionsDto {
        public List<int> Numbers { get; set; } = [];
        public string[] Texts { get; set; } = [];
    }

    public class Ef6PersonImplicitNestedAndCollectionsDto {
        public string FullName { get; set; } = string.Empty;
        public Ef6AddressDto HomeAddress { get; set; } = null!;
        public List<Ef6PhoneDto> Phones { get; set; } = [];
    }

    public class Ef6PersonImplicitNestedAndArrayDto {
        public string FullName { get; set; } = string.Empty;
        public Ef6AddressDto HomeAddress { get; set; } = null!;
        public Ef6PhoneDto[] Phones { get; set; } = [];
    }

    public class Ef6PersonFilteredPhonesDto {
        public IEnumerable<Ef6PhoneDto> Students { get; set; } = [];
    }

    public class Ef6NamedPhonesDto {
        public IEnumerable<Ef6PhoneDto> PhonesRaw { get; set; } = [];
        public IEnumerable<Ef6PhoneDto> PhonesMasked { get; set; } = [];
    }

    public class Ef6PersonChainedPhonesDto {
        public IEnumerable<Ef6PhoneDto> PhonesOrdered { get; set; } = [];
    }

    public class Ef6NamedPersonChainedPhonesDto {
        public IEnumerable<Ef6PhoneDto> PhonesOrdered { get; set; } = [];
    }

    public class Ef6PersonCalculationDto {
        public int Id { get; set; }
        public int AgeInDays { get; set; }
    }

    public class Ef6ProjectToNamedPhonesDto {
        public IEnumerable<Ef6PhoneDto> Phones { get; set; } = [];
    }

    public class Ef6ProjectionIgnoreDto {
        public string Included { get; set; } = string.Empty;
        public string? IgnoredFromDb { get; set; }
    }

    private class Ef6DiSource {
        public int Value { get; set; }
    }

    private class Ef6DiTarget {
        public int Value { get; set; }
    }
}
