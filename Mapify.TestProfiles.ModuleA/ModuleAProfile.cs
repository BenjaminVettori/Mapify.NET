namespace Mapify.TestProfiles.ModuleA;

public class ModuleASource {
    public int Value { get; set; }
}

public class ModuleATarget {
    public int Value { get; set; }
}

public class ModuleAProfile : Mapify.NET.MapifyProfile {
    protected override void Configure() {
        CreateMap<ModuleASource, ModuleATarget>();
    }
}
