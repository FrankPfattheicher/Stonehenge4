using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable NotAccessedField.Global

namespace IctBaden.Stonehenge.Test.Serializer;

[SuppressMessage("Design", "MA0016:Prefer using collection abstraction instead of implementation")]
public class SimpleClass
{
    public int Integer { get; set; }
    public bool Boolean { get; set; }
    public double FloatingPoint { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public DateTimeOffset Timeoffset { get; set; } = DateTimeOffset.Now;

    public TestEnum Wieviel { get; set; } = TestEnum.Fumpf;

    public string PrivateText = string.Empty;
    
    public List<string> PhoneNumbers { get; set; } = [];
}