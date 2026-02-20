namespace Mapify.TestProfiles.ModuleB;

public class ModuleBSource {
    public string Text { get; set; } = string.Empty;
}

public class ModuleBTarget {
    public string Text { get; set; } = string.Empty;
}

public class ModuleBProfile : Mapify.NET.MapifyProfile {
    protected override void Configure() {
        CreateMap<ModuleBSource, ModuleBTarget>();
    }
}
