using System;

namespace IctBaden.Stonehenge.Types;

public static class Element
{
    public static string NewId() => Guid.NewGuid().ToString("N");
}