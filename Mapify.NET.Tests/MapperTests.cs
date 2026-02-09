using System.Linq.Expressions;

namespace Mapify.NET.Tests
{
    [Collection("Mapper Tests")]
    public class MapperTests
    {
        public class Source
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public int? Age { get; set; }
            public DateTime Date { get; set; }
        }

        public class Target
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public int? Age { get; set; }
            public DateTime Date { get; set; }
        }

        public class TargetSubset
        {
            public string Name { get; set; } = string.Empty;
        }

        public class TargetWithDifferentProp
        {
            public int Id { get; set; }
            public string FullName { get; set; } = string.Empty;
        }
        
        public class SourceNullable
        {
            public int? Value { get; set; }
        }

        public class TargetNonNullable
        {
            public int Value { get; set; }
        }

        public class TargetNullable
        {
            public int? Value { get; set; }
        }

        public MapperTests()
        {
            // Reset global flag to default before each test (best effort since it's static)
            Mapper.UseDefaultMapIfTypeMapIsMissing(false);
            Mapper.ClearMappings();
        }

        [Fact]
        public void Map_ShouldMapProperties_WhenAutoMapIsUsed()
        {
            // Arrange
            var source = new Source { Id = 1, Name = "Test", Age = 25, Date = DateTime.Now };
            
            // Act
            // Use implicit creation
            var target = Mapper.Map<Source, Target>(source, useDefaultMapIfTypeMapIsMissing: true);

            // Assert
            Assert.Equal(source.Id, target.Id);
            Assert.Equal(source.Name, target.Name);
            Assert.Equal(source.Age, target.Age);
            Assert.Equal(source.Date, target.Date);
        }

        [Fact]
        public void AddMap_ShouldThrowException_WhenMappingAlreadyExists()
        {
            // Arrange
            Mapper.AddMap<Source, TargetSubset>(s => new TargetSubset { Name = s.Name });

            // Act & Assert
            Assert.Throws<ArgumentException>(() => Mapper.AddMap<Source, TargetSubset>(s => new TargetSubset { Name = s.Name }));
        }

        [Fact]
        public void Map_ShouldUseExplicitMap_WhenAdded()
        {
            // Arrange
            // We need unique types to ensure no previous map exists or we are the ones adding it.
            // Using A1->B1
            Mapper.AddMap<A1, B1>(s => new B1 { Prop = s.Prop + 10 });
            var source = new A1 { Prop = 5 };

            // Act
            var target = Mapper.Map<A1, B1>(source);

            // Assert
            Assert.Equal(15, target.Prop);
        }

        [Fact]
        public void Map_ShouldThrowException_WhenMapMissingAndFlagFalse()
        {
            // Arrange
            // Ensure global flag is false
            Mapper.UseDefaultMapIfTypeMapIsMissing(false);
            var source = new A2 { Prop = 5 };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => Mapper.Map<A2, B2>(source));
        }

        [Fact]
        public void Map_ShouldUseDefaultMap_WhenMapMissingAndFlagTrue()
        {
            // Arrange
            var source = new A2 { Prop = 5 };

            // Act
            // Pass true to override global false
            var target = Mapper.Map<A2, B2>(source, useDefaultMapIfTypeMapIsMissing: true);

            // Assert
            Assert.Equal(5, target.Prop);
        }

        [Fact]
        public void Test_GlobalUseDefaultMapIfTypeMapIsMissing()
        {
            // Arrange
            Mapper.UseDefaultMapIfTypeMapIsMissing(true);
            var source = new A3 { Prop = 5 };

            // Act
            var target = Mapper.Map<A3, B3>(source);

            // Assert
            Assert.Equal(5, target.Prop);
        }

        [Fact]
        public void Map_ToExistingObject_ShouldUpdateProperties()
        {
            // Arrange
            var source = new Source { Id = 10, Name = "Updated" };
            var target = new Target { Id = 1, Name = "Original" };

            // Act
            Mapper.Map(source, target, useDefaultMapIfTypeMapIsMissing: true);

            // Assert
            Assert.Equal(10, target.Id);
            Assert.Equal("Updated", target.Name);
        }

        [Fact]
        public void Map_ToExistingObject_WithExplicitMap()
        {
             // Arrange
            var source = new A4 { Prop = 20 };
            var target = new B4 { Prop = 10 };
            
            Mapper.AddMap<A4, B4>(s => new B4 { Prop = s.Prop + 10 });
            
            // Act
            Mapper.Map(source, target);

            // Assert
            Assert.Equal(30, target.Prop);
        }

        [Fact]
        public void Map_ShouldHandleNullableToNonNullable_WithValues()
        {
            // Arrange
            var source = new SourceNullable { Value = 10 };
            
            // Act
            var target = Mapper.Map<SourceNullable, TargetNonNullable>(source, useDefaultMapIfTypeMapIsMissing: true);

            // Assert
            Assert.Equal(10, target.Value);
        }

        [Fact]
        public void Map_ShouldHandleNullableToNonNullable_WithNull()
        {
            // Arrange
            var source = new SourceNullable { Value = null };
            
            // Act
            var target = Mapper.Map<SourceNullable, TargetNonNullable>(source, useDefaultMapIfTypeMapIsMissing: true);

            // Assert
            Assert.Equal(0, target.Value); // Default for int
        }

        [Fact]
        public void Map_ShouldHandleNonNullableToNullable()
        {
            // Arrange
            var source = new TargetNonNullable { Value = 10 };

            // Act
            var target = Mapper.Map<TargetNonNullable, TargetNullable>(source, useDefaultMapIfTypeMapIsMissing: true);

            // Assert
            Assert.Equal(10, target.Value);
        }
        
        [Fact]
        public void CreateAndAddMap_ShouldCreateFullMap()
        {
             // Arrange
             // Unique types
             var source = new C1 { Id = 1, Name = "Test" };
             
             // Act
             // Partial map for Name, expected Id to be automapped
             Mapper.CreateAndAddMap<C1, D1>(s => new D1 { Name = s.Name + "_" });
             
             var target = Mapper.Map<C1, D1>(source);

             // Assert
             Assert.Equal(1, target.Id);
             Assert.Equal("Test_", target.Name);
        }

        [Fact]
        public void GetMap_ShouldReturnExpression()
        {
            // Arrange
            var source = new A2 { Prop = 10 };
            // Ensure we can get a map (defaults allowed)
            
            // Act
            var expr = Mapper.GetMap<A2, B2>(useDefaultMapIfTypeMapIsMissing: true);
            var func = expr.Compile();
            var target = func(source);
            
            // Assert
            Assert.NotNull(expr);
            Assert.Equal(10, target.Prop);
        }

        [Fact]
        public void CompileMapper_ShouldReturnAction()
        {
            // Arrange
            Expression<Func<A2, B2>> expr = x => new B2 { Prop = x.Prop };
            
            // Act
            var action = Mapper.CompileMapper(expr);
            var source = new A2 { Prop = 50 };
            var target = new B2();
            action(source, target);
            
            // Assert
            Assert.Equal(50, target.Prop);
        }

        public class C1 { public int Id { get; set; } public string Name { get; set; } }
        public class D1 { public int Id { get; set; } public string Name { get; set; } }

        public class A1 { public int Prop { get; set; } }
        public class B1 { public int Prop { get; set; } }

        public class A2 { public int Prop { get; set; } }
        public class B2 { public int Prop { get; set; } }

        public class A3 { public int Prop { get; set; } }
        public class B3 { public int Prop { get; set; } }

        public class A4 { public int Prop { get; set; } }
        public class B4 { public int Prop { get; set; } }
    }
}
