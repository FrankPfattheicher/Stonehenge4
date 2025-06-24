using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using IctBaden.Framework.AppUtils;
using IctBaden.Stonehenge.Core;
using IctBaden.Stonehenge.ViewModel;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace IctBaden.Stonehenge.Vue.SampleCore.ViewModels;

[SuppressMessage("Usage", "MA0011:IFormatProvider is missing")]
public class InfoVm : ActiveViewModel
{
    public string TestValue { get; set; } = string.Empty;

    public string AppReleaseDate { get; private set; } = string.Empty;

    public string RuntimeDirectory { get; private set; } = string.Empty;
    public bool IsSelfHosted { get; private set; }
    public string ClrVersion { get; private set; } = string.Empty;
    
    public InfoVm(AppSession session) : base(session)
    {
        SupportsEvents = false;
    }
    
    public override void OnLoad()
    {
        var directory = Environment.ProcessPath ?? Directory.GetCurrentDirectory();
        AppReleaseDate = File.GetCreationTime(directory).Date.ToString("d");

        RuntimeDirectory = RuntimeEnvironment.GetRuntimeDirectory();
        IsSelfHosted = FrameworkInfo.IsSelfHosted;
        ClrVersion = RuntimeEnvironment.GetSystemVersion();
    }
}