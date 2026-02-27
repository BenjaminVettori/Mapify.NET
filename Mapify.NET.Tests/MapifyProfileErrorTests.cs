namespace Mapify.NET.Tests;

public class MapifyProfileErrorTests {
    private class Source {
    }

    private class Target {
    }

    private sealed class ProfileFacade : MapifyProfile {
        protected override void Configure() {
        }

        public void InvokeCreateMapWithoutConfigurator() {
            CreateMap<Source, Target>();
        }

        public void InvokeNamedCreateMapWithoutConfigurator() {
            CreateMap<Source, Target>("Named");
        }

        public static int InvokeUseMapMarker(int value) {
            return UseMap<int, int>(value);
        }

        public static int InvokeUseMapMarkerNamed(string name, int value) {
            return UseMap<int, int>(name, value);
        }

        public static int InvokeIgnoreMarker() {
            return Ignore<int>();
        }
    }

    [Fact]
    public void CreateMap_ShouldThrow_WhenCalledOutsideConfigure() {
        var profile = new ProfileFacade();

        Assert.Throws<InvalidOperationException>(() => profile.InvokeCreateMapWithoutConfigurator());
    }

    [Fact]
    public void CreateMapNamed_ShouldThrow_WhenCalledOutsideConfigure() {
        var profile = new ProfileFacade();

        Assert.Throws<InvalidOperationException>(() => profile.InvokeNamedCreateMapWithoutConfigurator());
    }

    [Fact]
    public void UseMap_ShouldThrow_WhenUsedOutsideMappingExpression() {
        Assert.Throws<InvalidOperationException>(() => ProfileFacade.InvokeUseMapMarker(3));
    }

    [Fact]
    public void UseMapNamed_ShouldThrow_WhenUsedOutsideMappingExpression() {
        Assert.Throws<InvalidOperationException>(() => ProfileFacade.InvokeUseMapMarkerNamed("named", 3));
    }

    [Fact]
    public void Ignore_ShouldThrow_WhenUsedOutsideMappingExpression() {
        Assert.Throws<InvalidOperationException>(() => ProfileFacade.InvokeIgnoreMarker());
    }
}
