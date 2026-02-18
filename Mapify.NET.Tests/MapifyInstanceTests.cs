namespace Mapify.NET.Tests {
    public class MapifyInstanceTests {
        private class Source {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        private class Target {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        private class SourceA {
            public int ValueA { get; set; }
        }

        private class TargetA {
            public int ValueA { get; set; }
        }

        private class SourceB {
            public string ValueB { get; set; } = string.Empty;
        }

        private class TargetB {
            public string ValueB { get; set; } = string.Empty;
        }

        private class ProfileA : MapifyProfile {
            protected override void Configure() {
                CreateMap<SourceA, TargetA>();
            }
        }

        private class ProfileB : MapifyProfile {
            protected override void Configure() {
                CreateMap<SourceB, TargetB>();
            }
        }

        [Fact]
        public void Map_ToNewObject_ShouldWorkLikeStaticMapper() {
            var mapify = new Mapify();
            var source = new Source { Id = 10, Name = "Alice" };

            var target = mapify.Map<Source, Target>(source, useDefaultMapIfTypeMapIsMissing: true);

            Assert.Equal(source.Id, target.Id);
            Assert.Equal(source.Name, target.Name);
        }

        [Fact]
        public void Map_ToExistingObject_ShouldWorkLikeStaticMapper() {
            var mapify = new Mapify();
            var source = new Source { Id = 22, Name = "Bob" };
            var target = new Target { Id = 1, Name = "Initial" };

            mapify.Map(source, target, useDefaultMapIfTypeMapIsMissing: true);

            Assert.Equal(22, target.Id);
            Assert.Equal("Bob", target.Name);
        }

        [Fact]
        public void Constructor_ShouldConfigureAllProfiles() {
            var mapify = new Mapify(new IMapifyProfile[] { new ProfileA(), new ProfileB() });

            var aResult = mapify.Map<SourceA, TargetA>(new SourceA { ValueA = 7 });
            var bResult = mapify.Map<SourceB, TargetB>(new SourceB { ValueB = "ok" });

            Assert.Equal(7, aResult.ValueA);
            Assert.Equal("ok", bResult.ValueB);
        }

        [Fact]
        public void UseDefaultMapIfTypeMapIsMissing_ShouldBeInstanceScoped() {
            var mapify1 = new Mapify();
            var mapify2 = new Mapify();

            mapify1.UseDefaultMapIfTypeMapIsMissing(true);

            var result = mapify1.Map<SourceA, TargetA>(new SourceA { ValueA = 3 });
            Assert.Equal(3, result.ValueA);

            Assert.Throws<ArgumentException>(() => mapify2.Map<SourceA, TargetA>(new SourceA { ValueA = 3 }));
        }
    }
}
