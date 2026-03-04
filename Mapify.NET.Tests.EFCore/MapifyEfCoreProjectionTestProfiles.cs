namespace Mapify.NET.Tests.EFCore;

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
}
