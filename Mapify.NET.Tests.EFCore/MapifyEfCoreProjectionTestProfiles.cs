namespace Mapify.NET.Tests.EFCore;

using System.Linq;

public partial class MapifyEfCoreProjectionTests {
    private sealed class EfCorePhoneProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<EfCorePhone, EfCorePhoneDto>();
        }
    }

    private sealed class EfCoreAddressProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<EfCoreAddress, EfCoreAddressDto>();
        }
    }

    private sealed class EfCorePolymorphicCostItemProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<EfCoreCostItem, EfCoreCostItemDto>(ci => new EfCoreCostItemDto {
                Price = ci is EfCoreCostItemType1
                    ? ((EfCoreCostItemType1)ci).Price
                    : ((EfCoreCostItemType2)ci).TotalPrice
            });
        }
    }

    private sealed class EfCorePolymorphicBillProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<EfCoreBill, EfCoreBillDto>(b => new EfCoreBillDto {
                CostItems = b.CostItems != null
                    ? b.CostItems.ProjectTo<EfCoreCostItemDto>()
                    : new List<EfCoreCostItemDto>()
            });
        }
    }

    private sealed class EfCorePolymorphicBillRelationalProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<EfCoreBill, EfCoreBillDto>(b => new EfCoreBillDto {
                CostItems = b.CostItems!.ProjectTo<EfCoreCostItemDto>()
            });
        }
    }

    private sealed class EfCoreBlockCostItemProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<EfCoreBlockCostItem, EfCoreCostItemDto>(ci => new EfCoreCostItemDto {
                Price = ci is EfCoreBlockCostItemType1
                    ? ((EfCoreBlockCostItemType1)ci).Price
                    : ((EfCoreBlockCostItemType2)ci).TotalPrice
            });
        }
    }

    private sealed class EfCoreBlockProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<EfCoreBlock, EfCoreBlockDto>(b => new EfCoreBlockDto {
                CostItems = b.CostItems != null
                    ? b.CostItems.ProjectTo<EfCoreCostItemDto>()
                    : new List<EfCoreCostItemDto>()
            });
        }
    }

    private sealed class EfCoreBillWithBlocksProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<EfCoreBillWithBlocks, EfCoreBillWithBlocksDto>(b => new EfCoreBillWithBlocksDto {
                Blocks = b.Blocks != null
                    ? b.Blocks.ProjectTo<EfCoreBlockDto>()
                    : new List<EfCoreBlockDto>()
            });
        }
    }

    private sealed class EfCoreBlockConditionalRelationalProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<EfCoreBlock, EfCoreBlockDto>(b => new EfCoreBlockDto {
                CostItems = b.CostItems != null
                    ? b.CostItems.ProjectTo<EfCoreCostItemDto>()
                    : Enumerable.Empty<EfCoreCostItemDto>()
            });
        }
    }

    private sealed class EfCoreBillWithBlocksConditionalRelationalProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<EfCoreBillWithBlocks, EfCoreBillWithBlocksDto>(b => new EfCoreBillWithBlocksDto {
                Blocks = b.Blocks != null
                    ? b.Blocks.ProjectTo<EfCoreBlockDto>()
                    : Enumerable.Empty<EfCoreBlockDto>()
            });
        }
    }

    private sealed class EfCoreVirtualListCostItemProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<EfCoreVirtualListCostItem, EfCoreCostItemDto>(ci => new EfCoreCostItemDto {
                Price = ci is EfCoreVirtualListCostItemType1
                    ? ((EfCoreVirtualListCostItemType1)ci).Price
                    : ((EfCoreVirtualListCostItemType2)ci).TotalPrice
            });
        }
    }

    private sealed class EfCoreVirtualListBlockProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<EfCoreVirtualListBlock, EfCoreVirtualListBlockDto>(b => new EfCoreVirtualListBlockDto {
                CostItems = b.CostItems != null
                    ? b.CostItems.ProjectTo<EfCoreCostItemDto>()
                    : Enumerable.Empty<EfCoreCostItemDto>()
            });
        }
    }

    private sealed class EfCoreVirtualListBillProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<EfCoreBillWithVirtualListBlocks, EfCoreBillWithVirtualListBlocksDto>(b => new EfCoreBillWithVirtualListBlocksDto {
                Blocks = b.Blocks != null
                    ? b.Blocks.ProjectTo<EfCoreVirtualListBlockDto>()
                    : Enumerable.Empty<EfCoreVirtualListBlockDto>()
            });
        }
    }

    private sealed class EfCoreVirtualListBlockExactUserShapeProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<EfCoreVirtualListBlock, EfCoreVirtualListBlockDto>(b => new EfCoreVirtualListBlockDto {
                CostItems = b.CostItems != null
                    ? b.CostItems.ProjectTo<EfCoreCostItemDto>().ToList()
                    : new List<EfCoreCostItemDto>()
            });
        }
    }

    private sealed class EfCoreVirtualListBillExactUserShapeProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<EfCoreBillWithVirtualListBlocks, EfCoreBillWithVirtualListBlocksDto>(a => new EfCoreBillWithVirtualListBlocksDto {
                Blocks = a.Blocks != null
                    ? a.Blocks.ProjectTo<EfCoreVirtualListBlockDto>().ToList()
                    : new List<EfCoreVirtualListBlockDto>()
            });
        }
    }

    private sealed class EfCorePersonCollectionsProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<EfCorePerson, EfCorePersonCollectionsDto>(x => new EfCorePersonCollectionsDto {
                FullName = x.FirstName + " " + x.LastName,
                PhonesArray = UseMap<ICollection<EfCorePhone>, EfCorePhoneDto[]>(x.Phones),
                PhonesList = UseMap<ICollection<EfCorePhone>, List<EfCorePhoneDto>>(x.Phones)
            });
        }
    }

    private sealed class EfCorePersonImplicitNestedAndArrayProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<EfCorePerson, EfCorePersonImplicitNestedAndArrayDto>(x => new EfCorePersonImplicitNestedAndArrayDto {
                FullName = x.FirstName + " " + x.LastName
            });
        }
    }

    private sealed class EfCorePersonFilteredPhonesProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<EfCorePerson, EfCorePersonFilteredPhonesDto>(x => new EfCorePersonFilteredPhonesDto {
                Students = UseMap<IEnumerable<EfCorePhone>, IEnumerable<EfCorePhoneDto>>(x.Phones.Where(s => s.Number.StartsWith("+44")))
            });
        }
    }

    private sealed class EfCoreNamedPhoneProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<EfCorePhone, EfCorePhoneDto>("Raw", x => new EfCorePhoneDto {
                Number = x.Number
            });

            CreateMap<EfCorePhone, EfCorePhoneDto>("Masked", x => new EfCorePhoneDto {
                Number = x.Number + " [MASKED]"
            });
        }
    }

    private sealed class EfCoreNamedPersonProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<EfCorePerson, EfCoreNamedPhonesDto>(x => new EfCoreNamedPhonesDto {
                PhonesRaw = UseMap<IEnumerable<EfCorePhone>, IEnumerable<EfCorePhoneDto>>("Raw", x.Phones),
                PhonesMasked = UseMap<IEnumerable<EfCorePhone>, IEnumerable<EfCorePhoneDto>>("Masked", x.Phones)
            });
        }
    }

    private sealed class EfCorePersonChainedPhonesProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<EfCorePerson, EfCorePersonChainedPhonesDto>(x => new EfCorePersonChainedPhonesDto {
                PhonesOrdered = UseMap<IEnumerable<EfCorePhone>, IEnumerable<EfCorePhoneDto>>(x.Phones)
                    .OrderBy(dto => dto.Number)
                    .ToList()
            });
        }
    }

    private sealed class EfCoreNamedPersonChainedProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<EfCorePerson, EfCorePersonChainedPhonesDto>(x => new EfCorePersonChainedPhonesDto {
                PhonesOrdered = UseMap<IEnumerable<EfCorePhone>, IEnumerable<EfCorePhoneDto>>("Masked", x.Phones)
                    .OrderBy(dto => dto.Number)
                    .ToList()
            });
        }
    }

    private sealed class EfCoreIntIdentityProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<int, int>(x => x);
        }
    }

    private sealed class EfCorePersonCalculationProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<EfCorePerson, EfCorePersonCalculationDto>(x => new EfCorePersonCalculationDto {
                AgeInDays = 365 * UseMap<int, int>(x.Id)
            });
        }
    }

    private sealed class EfCoreNamedProjectToPersonProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<EfCorePerson, EfCoreProjectToNamedPhonesDto>("Raw", x => new EfCoreProjectToNamedPhonesDto {
                Phones = x.Phones.ProjectTo<EfCorePhoneDto>("Raw").ToList()
            });

            CreateMap<EfCorePerson, EfCoreProjectToNamedPhonesDto>("Masked", x => new EfCoreProjectToNamedPhonesDto {
                Phones = x.Phones.ProjectTo<EfCorePhoneDto>("Masked").ToList()
            });
        }
    }

    private sealed class EfCoreNamedNestedProjectToPersonProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<EfCorePerson, EfCoreProjectToNamedPhonesDto>("Masked", x => new EfCoreProjectToNamedPhonesDto {
                Phones = x.Phones.ProjectTo<EfCorePhoneDto>("Masked")
                    .OrderBy(dto => dto.Number)
                    .ToList()
            });
        }
    }

    private sealed class EfCoreProjectionIgnoreProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<EfCoreProjectionIgnoreEntity, EfCoreProjectionIgnoreDto>(x => new EfCoreProjectionIgnoreDto {
                IgnoredFromDb = Ignore<string?>()
            });
        }
    }

    private sealed class EfCorePersonRuntimeParameterProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<EfCorePerson, EfCorePersonRuntimeParameterDto>(x => new EfCorePersonRuntimeParameterDto {
                AdjustedId = x.Id + Parameter<int>("offset")
            });
        }
    }

    private sealed class EfCorePersonStreetNumberProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<EfCorePerson, EfCorePersonStreetNumberDto>(x => new EfCorePersonStreetNumberDto {
                StreetNumber = x.HomeAddress.Street!.Number
            });
        }
    }

    private sealed class EfCorePersonStreetNullableNumberProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<EfCorePerson, EfCorePersonStreetNullableNumberDto>(x => new EfCorePersonStreetNullableNumberDto {
                StreetNumber = x.HomeAddress.Street!.Number
            });
        }
    }

    private sealed class EfCoreProxyLikeBaseProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<EfCoreProxyLikeBaseSource, EfCoreProxyLikeDto>(x => new EfCoreProxyLikeDto {
                Value = x.Value + 1
            });
        }
    }

    private sealed class EfCoreProxyLikeNamedBaseProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<EfCoreProxyLikeBaseSource, EfCoreProxyLikeDto>("Offset", x => new EfCoreProxyLikeDto {
                Value = x.Value + 10
            });
        }
    }

    private sealed class EfCoreVirtualListCostItemBaseOnlyProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<EfCoreVirtualListCostItem, EfCoreCostItemDto>(x => new EfCoreCostItemDto {
                Price = x.Id
            });
        }
    }

    private sealed class EfCoreRecursiveNodeDefaultDepthProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<EfCoreRecursiveNode, EfCoreRecursiveNodeDto>(x => new EfCoreRecursiveNodeDto {
                Name = x.Name,
                Children = UseMap<ICollection<EfCoreRecursiveNode>, List<EfCoreRecursiveNodeDto>>(x.Children)
            });
        }
    }

    private sealed class EfCoreRecursiveNodeDepthThreeProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<EfCoreRecursiveNode, EfCoreRecursiveNodeDto>("DepthThree", x => new EfCoreRecursiveNodeDto {
                Name = x.Name,
                Children = UseMap<ICollection<EfCoreRecursiveNode>, List<EfCoreRecursiveNodeDto>>(x.Children, 3)
            });
        }
    }

}
