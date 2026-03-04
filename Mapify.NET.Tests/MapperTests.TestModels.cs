namespace Mapify.NET.Tests;

public partial class MapperTests {
    public class Source { public int Id { get; set; } public string Name { get; set; } = string.Empty; public int? Age { get; set; } public DateTime Date { get; set; } }
    public class Target { public int Id { get; set; } public string Name { get; set; } = string.Empty; public int? Age { get; set; } public DateTime Date { get; set; } }
    public class TargetSubset { public string Name { get; set; } = string.Empty; }
    public class TargetWithDifferentProp { public int Id { get; set; } public string FullName { get; set; } = string.Empty; }

    public class SourceNullable { public int? Value { get; set; } }
    public class TargetNonNullable { public int Value { get; set; } }
    public class TargetNullable { public int? Value { get; set; } }

    public class C1 { public int Id { get; set; } public string Name { get; set; } = string.Empty; }
    public class D1 { public int Id { get; set; } public string Name { get; set; } = string.Empty; }

    public class A1 { public int Prop { get; set; } }
    public class B1 { public int Prop { get; set; } }

    public class A2 { public int Prop { get; set; } }
    public class B2 { public int Prop { get; set; } }

    public class A3 { public int Prop { get; set; } }
    public class B3 { public int Prop { get; set; } }

    public class A4 { public int Prop { get; set; } }
    public class B4 { public int Prop { get; set; } }

    public class NestedSource { public int Value { get; set; } }
    public class NestedTarget { public int Value { get; set; } }

    public class ParentWithNestedSource { public NestedSource Nested { get; set; } = new NestedSource(); }
    public class ParentWithNestedTarget { public NestedTarget Nested { get; set; } = new NestedTarget(); }

    public struct NumberSource { public int Value { get; set; } }
    public struct NumberTarget { public int Value { get; set; } }

    public class ContainerSrcToTarget { public NumberSource Number { get; set; } }
    public class ContainerTarget { public NumberTarget Number { get; set; } }

    public class ContainerSrcToNullableTarget { public NumberSource Number { get; set; } }
    public class ContainerNullableTarget { public NumberTarget? Number { get; set; } }

    public class ContainerNullableSrcToTarget { public NumberSource? Number { get; set; } }
    public class ContainerNullableSrcToNullableTarget { public NumberSource? Number { get; set; } }

    public struct ComplexValue { public int Value { get; set; } }
    public class ComplexValueContainerSource { public ComplexValue? Item { get; set; } }
    public class ComplexValueContainerTarget { public ComplexValue Item { get; set; } }

    public struct PrecedenceNumberSource { public int Value { get; set; } }
    public struct PrecedenceNumberTarget { public int Value { get; set; } }
    public class PrecedenceNullableContainerSource { public PrecedenceNumberSource? Item { get; set; } }
    public class PrecedenceNullableContainerTarget { public PrecedenceNumberTarget? Item { get; set; } }

    public class PrecedenceCollectionElementSource { public int Value { get; set; } }
    public class PrecedenceCollectionElementTarget { public int Value { get; set; } }
    public class PrecedenceCollectionContainerSource { public List<PrecedenceCollectionElementSource> Items { get; set; } = []; }
    public class PrecedenceCollectionContainerTarget { public List<PrecedenceCollectionElementTarget> Items { get; set; } = []; }
    public class NullableCollectionContainerSource { public List<PrecedenceCollectionElementSource>? Items { get; set; } }
    public class NullableCollectionContainerTarget { public List<PrecedenceCollectionElementTarget>? Items { get; set; } }

    public class AmbiguousEnumerable : IEnumerable<int>, IEnumerable<string> {
        IEnumerator<int> IEnumerable<int>.GetEnumerator() => Array.Empty<int>().AsEnumerable().GetEnumerator();

        IEnumerator<string> IEnumerable<string>.GetEnumerator() => Array.Empty<string>().AsEnumerable().GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            => ((IEnumerable<int>)this).GetEnumerator();
    }

    public class AmbiguousEnumerableContainerSource { public AmbiguousEnumerable Items { get; set; } = new AmbiguousEnumerable(); }
    public class AmbiguousEnumerableContainerTarget { public List<int> Items { get; set; } = []; }

    public class ListBindingSource { public int Value { get; set; } }
    public class ListBindingTarget { public List<int> Items { get; } = []; }

    public class EnumerableCtorSource { public int[] Numbers { get; set; } = []; }
    public class EnumerableCtorContainerTarget { public EnumerableCtorCollection Numbers { get; set; } = new EnumerableCtorCollection([]); }
    public class EnumerableCtorCollection(IEnumerable<int> values) : IEnumerable<int> {
        private readonly int[] _values = [.. values];

        public int[] ToArray() => _values;

        public IEnumerator<int> GetEnumerator() => ((IEnumerable<int>)_values).GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _values.GetEnumerator();
    }

    public class ResolverInnerSource { public int Value { get; set; } }
    public class ResolverInnerTarget { public int Value { get; set; } }
    public class ResolverSource { public ResolverInnerSource Child { get; set; } = new ResolverInnerSource(); }
    public class ResolverTarget { public ResolverInnerTarget? Child { get; set; } }

    public class IgnoreSource { public int Included { get; set; } public int Ignored { get; set; } }
    public class IgnoreTarget { public int Included { get; set; } public int Ignored { get; set; } }
    public class IgnoreRequiredTarget { public int Included { get; set; } public required int Ignored { get; set; } }

    public class PersonNameSource { public string Name { get; set; } = string.Empty; }

    public enum SourceStatus { Inactive = 0, Active = 1 }
    public enum TargetStatus { Disabled = 0, Enabled = 1 }
}
