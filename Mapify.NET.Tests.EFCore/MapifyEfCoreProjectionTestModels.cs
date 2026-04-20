using Microsoft.EntityFrameworkCore;

namespace Mapify.NET.Tests.EFCore;

public partial class MapifyEfCoreProjectionTests {
    private sealed class EfCoreMapifyContext(DbContextOptions<EfCoreMapifyContext> options) : DbContext(options) {
        public DbSet<EfCorePerson> People => Set<EfCorePerson>();
        public DbSet<EfCoreAddress> Addresses => Set<EfCoreAddress>();
        public DbSet<EfCoreStreet> Streets => Set<EfCoreStreet>();
        public DbSet<EfCorePhone> Phones => Set<EfCorePhone>();
        public DbSet<EfCoreBill> Bills => Set<EfCoreBill>();
        public DbSet<EfCoreCostItem> CostItems => Set<EfCoreCostItem>();
        public DbSet<EfCoreCostItemType1> CostItemsType1 => Set<EfCoreCostItemType1>();
        public DbSet<EfCoreCostItemType2> CostItemsType2 => Set<EfCoreCostItemType2>();
        public DbSet<EfCoreRecursiveNode> RecursiveNodes => Set<EfCoreRecursiveNode>();
        public DbSet<EfCoreProjectionIgnoreEntity> ProjectionIgnoreEntities => Set<EfCoreProjectionIgnoreEntity>();
    }

    private sealed class EfCoreRecursiveNode {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? ParentId { get; set; }
        public EfCoreRecursiveNode? Parent { get; set; }
        public ICollection<EfCoreRecursiveNode> Children { get; set; } = [];
    }

    private sealed class EfCoreRecursiveNodeDto {
        public string Name { get; set; } = string.Empty;
        public List<EfCoreRecursiveNodeDto> Children { get; set; } = [];
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
        public int? StreetId { get; set; }
        public EfCoreStreet? Street { get; set; }
    }

    private sealed class EfCoreStreet {
        public int Id { get; set; }
        public int Number { get; set; }
    }

    private sealed class EfCorePhone {
        public int Id { get; set; }
        public string Number { get; set; } = string.Empty;
        public int PersonId { get; set; }
        public EfCorePerson Person { get; set; } = null!;
    }

    private sealed class EfCoreBill {
        public int Id { get; set; }
        public ICollection<EfCoreCostItem>? CostItems { get; set; }
    }

    private abstract class EfCoreCostItem {
        public int Id { get; set; }
        public int BillId { get; set; }
        public EfCoreBill Bill { get; set; } = null!;
    }

    private sealed class EfCoreCostItemType1 : EfCoreCostItem {
        public decimal Price { get; set; }
    }

    private sealed class EfCoreCostItemType2 : EfCoreCostItem {
        public decimal TotalPrice { get; set; }
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

    private sealed class EfCoreCostItemDto {
        public decimal Price { get; set; }
    }

    private sealed class EfCoreBillDto {
        public IEnumerable<EfCoreCostItemDto> CostItems { get; set; } = [];
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

    private sealed class EfCorePersonRuntimeParameterDto {
        public int AdjustedId { get; set; }
    }

    private sealed class EfCorePersonStreetNumberDto {
        public int StreetNumber { get; set; }
    }

    private sealed class EfCorePersonStreetNullableNumberDto {
        public int? StreetNumber { get; set; }
    }
}
