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
        public DbSet<EfCoreBillWithBlocks> BillsWithBlocks => Set<EfCoreBillWithBlocks>();
        public DbSet<EfCoreBlock> Blocks => Set<EfCoreBlock>();
        public DbSet<EfCoreBlockCostItem> BlockCostItems => Set<EfCoreBlockCostItem>();
        public DbSet<EfCoreBlockCostItemType1> BlockCostItemsType1 => Set<EfCoreBlockCostItemType1>();
        public DbSet<EfCoreBlockCostItemType2> BlockCostItemsType2 => Set<EfCoreBlockCostItemType2>();
        public DbSet<EfCoreBillWithVirtualListBlocks> BillsWithVirtualListBlocks => Set<EfCoreBillWithVirtualListBlocks>();
        public DbSet<EfCoreVirtualListBlock> VirtualListBlocks => Set<EfCoreVirtualListBlock>();
        public DbSet<EfCoreVirtualListCostItem> VirtualListCostItems => Set<EfCoreVirtualListCostItem>();
        public DbSet<EfCoreVirtualListCostItemType1> VirtualListCostItemsType1 => Set<EfCoreVirtualListCostItemType1>();
        public DbSet<EfCoreVirtualListCostItemType2> VirtualListCostItemsType2 => Set<EfCoreVirtualListCostItemType2>();
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

    private sealed class EfCoreBillWithBlocks {
        public int Id { get; set; }
        public ICollection<EfCoreBlock>? Blocks { get; set; }
    }

    private sealed class EfCoreBlock {
        public int Id { get; set; }
        public int BillId { get; set; }
        public EfCoreBillWithBlocks Bill { get; set; } = null!;
        public ICollection<EfCoreBlockCostItem>? CostItems { get; set; }
    }

    private abstract class EfCoreBlockCostItem {
        public int Id { get; set; }
        public int BlockId { get; set; }
        public EfCoreBlock Block { get; set; } = null!;
    }

    private sealed class EfCoreBlockCostItemType1 : EfCoreBlockCostItem {
        public decimal Price { get; set; }
    }

    private sealed class EfCoreBlockCostItemType2 : EfCoreBlockCostItem {
        public decimal TotalPrice { get; set; }
    }

    private sealed class EfCoreBlockDto {
        public IEnumerable<EfCoreCostItemDto> CostItems { get; set; } = [];
    }

    private sealed class EfCoreBillWithBlocksDto {
        public IEnumerable<EfCoreBlockDto> Blocks { get; set; } = [];
    }

    private class EfCoreBillWithVirtualListBlocks {
        public int Id { get; set; }
        public virtual List<EfCoreVirtualListBlock>? Blocks { get; set; }
    }

    private class EfCoreVirtualListBlock {
        public int Id { get; set; }
        public int BillId { get; set; }
        public virtual EfCoreBillWithVirtualListBlocks Bill { get; set; } = null!;
        public virtual List<EfCoreVirtualListCostItem>? CostItems { get; set; }
    }

    private abstract class EfCoreVirtualListCostItem {
        public int Id { get; set; }
        public int BlockId { get; set; }
        public virtual EfCoreVirtualListBlock Block { get; set; } = null!;
    }

    private sealed class EfCoreVirtualListCostItemType1 : EfCoreVirtualListCostItem {
        public decimal Price { get; set; }
    }

    private sealed class EfCoreVirtualListCostItemType2 : EfCoreVirtualListCostItem {
        public decimal TotalPrice { get; set; }
    }

    private sealed class EfCoreVirtualListBlockDto {
        public IEnumerable<EfCoreCostItemDto> CostItems { get; set; } = [];
    }

    private sealed class EfCoreBillWithVirtualListBlocksDto {
        public IEnumerable<EfCoreVirtualListBlockDto> Blocks { get; set; } = [];
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
