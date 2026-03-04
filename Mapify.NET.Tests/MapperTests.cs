namespace Mapify.NET.Tests;

[Collection("Mapper Tests")]
public partial class MapperTests {
    public MapperTests() {
        Mapper.UseDefaultMapIfTypeMapIsMissing(false);
        Mapper.ClearMappings();
    }
}
