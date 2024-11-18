using System.Collections.Generic;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnusedMember.Global

namespace IctBaden.Stonehenge.Test.Serializer;

public class NestedClass
{
    public string Name { get; set; } = string.Empty;
    public IList<NestedClass2> Nested { get; set; } = new List<NestedClass2>();
}