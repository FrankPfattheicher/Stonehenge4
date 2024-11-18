using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnusedMethodReturnValue.Global

namespace IctBaden.Stonehenge.Vue.Test;

[SuppressMessage("Design", "MA0046:Use EventHandler<T> to declare events")]
public class VueTestData
{
    public IDictionary<string, string> StartVmParameters { get; set; } = new Dictionary<string, string>(StringComparer.Ordinal);
    public int StartVmOnLoadCalled { get; set; }

    public string CurrentRoute { get; set; } = string.Empty;

    public event Func<string, string> DoAction = s => s;

    public string ExecAction(string action) => DoAction.Invoke(action);
}