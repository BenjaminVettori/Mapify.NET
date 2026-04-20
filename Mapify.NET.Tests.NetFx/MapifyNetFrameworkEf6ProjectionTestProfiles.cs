namespace Mapify.NET.Tests.NetFx;

public partial class MapifyNetFrameworkEf6ProjectionTests {
    private class Ef6PhoneProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<Ef6Phone, Ef6PhoneDto>();
        }
    }

    private class Ef6AddressProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<Ef6Address, Ef6AddressDto>();
        }
    }

    private class Ef6PolymorphicCostItemProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<Ef6CostItem, Ef6CostItemDto>(ci => new Ef6CostItemDto {
                Price = ci is Ef6CostItemType1
                    ? ((Ef6CostItemType1)ci).Price
                    : ((Ef6CostItemType2)ci).TotalPrice
            });
        }
    }

    private class Ef6PolymorphicBillProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<Ef6Bill, Ef6BillDto>(b => new Ef6BillDto {
                CostItems = b.CostItems != null
                    ? b.CostItems.ProjectTo<Ef6CostItemDto>()
                    : new List<Ef6CostItemDto>()
            });
        }
    }

    private class Ef6PersonCollectionsProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<Ef6Person, Ef6PersonCollectionsDto>(x => new Ef6PersonCollectionsDto {
                FullName = x.FirstName + " " + x.LastName,
                PhonesList = UseMap<ICollection<Ef6Phone>, List<Ef6PhoneDto>>(x.Phones),
                PhonesEnumerable = UseMap<ICollection<Ef6Phone>, IEnumerable<Ef6PhoneDto>>(x.Phones)
            });
        }
    }

    private class Ef6PersonArrayCollectionsProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<Ef6Person, Ef6PersonArrayCollectionsDto>(x => new Ef6PersonArrayCollectionsDto {
                FullName = x.FirstName + " " + x.LastName,
                PhonesArray = UseMap<ICollection<Ef6Phone>, Ef6PhoneDto[]>(x.Phones),
                PhonesList = UseMap<ICollection<Ef6Phone>, List<Ef6PhoneDto>>(x.Phones)
            });
        }
    }

    private class Ef6PersonImplicitNestedAndCollectionsProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<Ef6Person, Ef6PersonImplicitNestedAndCollectionsDto>(x => new Ef6PersonImplicitNestedAndCollectionsDto {
                FullName = x.FirstName + " " + x.LastName
            });
        }
    }

    private class Ef6PersonImplicitNestedAndArrayProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<Ef6Person, Ef6PersonImplicitNestedAndArrayDto>(x => new Ef6PersonImplicitNestedAndArrayDto {
                FullName = x.FirstName + " " + x.LastName
            });
        }
    }

    private class Ef6PersonFilteredPhonesProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<Ef6Person, Ef6PersonFilteredPhonesDto>(x => new Ef6PersonFilteredPhonesDto {
                Students = UseMap<IEnumerable<Ef6Phone>, IEnumerable<Ef6PhoneDto>>(x.Phones.Where(s => s.Number.StartsWith("+44")))
            });
        }
    }

    private class Ef6NamedPhoneProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<Ef6Phone, Ef6PhoneDto>("Raw", x => new Ef6PhoneDto {
                Number = x.Number
            });

            CreateMap<Ef6Phone, Ef6PhoneDto>("Masked", x => new Ef6PhoneDto {
                Number = x.Number + " [MASKED]"
            });
        }
    }

    private class Ef6NamedPersonProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<Ef6Person, Ef6NamedPhonesDto>(x => new Ef6NamedPhonesDto {
                PhonesRaw = UseMap<IEnumerable<Ef6Phone>, IEnumerable<Ef6PhoneDto>>("Raw", x.Phones),
                PhonesMasked = UseMap<IEnumerable<Ef6Phone>, IEnumerable<Ef6PhoneDto>>("Masked", x.Phones)
            });
        }
    }

    private class Ef6PersonChainedPhonesProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<Ef6Person, Ef6PersonChainedPhonesDto>(x => new Ef6PersonChainedPhonesDto {
                PhonesOrdered = UseMap<IEnumerable<Ef6Phone>, IEnumerable<Ef6PhoneDto>>(x.Phones)
                    .OrderBy(dto => dto.Number)
                    .ToList()
            });
        }
    }

    private class Ef6NamedPersonChainedProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<Ef6Person, Ef6NamedPersonChainedPhonesDto>(x => new Ef6NamedPersonChainedPhonesDto {
                PhonesOrdered = UseMap<IEnumerable<Ef6Phone>, IEnumerable<Ef6PhoneDto>>("Masked", x.Phones)
                    .OrderBy(dto => dto.Number)
                    .ToList()
            });
        }
    }

    private class Ef6IntIdentityProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<int, int>(x => x);
        }
    }

    private class Ef6PersonCalculationProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<Ef6Person, Ef6PersonCalculationDto>(x => new Ef6PersonCalculationDto {
                AgeInDays = 365 * UseMap<int, int>(x.Id)
            });
        }
    }

    private class Ef6NamedProjectToPersonProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<Ef6Person, Ef6ProjectToNamedPhonesDto>("Raw", x => new Ef6ProjectToNamedPhonesDto {
                Phones = x.Phones.ProjectTo<Ef6PhoneDto>("Raw").ToList()
            });

            CreateMap<Ef6Person, Ef6ProjectToNamedPhonesDto>("Masked", x => new Ef6ProjectToNamedPhonesDto {
                Phones = x.Phones.ProjectTo<Ef6PhoneDto>("Masked").ToList()
            });
        }
    }

    private class Ef6NamedNestedProjectToPersonProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<Ef6Person, Ef6ProjectToNamedPhonesDto>("MaskedNested", x => new Ef6ProjectToNamedPhonesDto {
                Phones = x.Phones.ProjectTo<Ef6PhoneDto>("Masked")
                    .OrderBy(dto => dto.Number)
                    .ToList()
            });
        }
    }

    private class Ef6ProjectionIgnoreProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<Ef6ProjectionIgnoreEntity, Ef6ProjectionIgnoreDto>(x => new Ef6ProjectionIgnoreDto {
                IgnoredFromDb = Ignore<string>()
            });
        }
    }

    private class Ef6DiProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<Ef6DiSource, Ef6DiTarget>();
        }
    }

    private class Ef6RecursiveNodeDefaultDepthProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<Ef6RecursiveNode, Ef6RecursiveNodeDto>(x => new Ef6RecursiveNodeDto {
                Name = x.Name,
                Children = UseMap<ICollection<Ef6RecursiveNode>, List<Ef6RecursiveNodeDto>>(x.Children)
            });
        }
    }

    private class Ef6RecursiveNodeDepthThreeProfile : MapifyProfile {
        protected override void Configure() {
            CreateMap<Ef6RecursiveNode, Ef6RecursiveNodeDto>("DepthThree", x => new Ef6RecursiveNodeDto {
                Name = x.Name,
                Children = UseMap<ICollection<Ef6RecursiveNode>, List<Ef6RecursiveNodeDto>>(x.Children, 3)
            });
        }
    }

}
