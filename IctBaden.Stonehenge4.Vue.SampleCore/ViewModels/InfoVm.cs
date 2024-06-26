using System;
using System.IO;
using System.Runtime.InteropServices;
using IctBaden.Framework.AppUtils;
using IctBaden.Stonehenge.Core;
using IctBaden.Stonehenge.ViewModel;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace IctBaden.Stonehenge.Vue.SampleCore.ViewModels;

public class InfoVm : ActiveViewModel
{
    public string AppReleaseDate { get; private set; } = string.Empty;

    public string RuntimeDirectory { get; private set; } = string.Empty;
    public bool IsSelfHosted { get; private set; }
    public string ClrVersion { get; private set; } = string.Empty;
    
    public InfoVm(AppSession session) : base(session)
    {
    }
    
    
    public override void OnLoad()
    {
        AppReleaseDate = File.GetCreationTime(Environment.ProcessPath!).Date.ToString("d");

        RuntimeDirectory = RuntimeEnvironment.GetRuntimeDirectory();
        IsSelfHosted = FrameworkInfo.IsSelfHosted;
        ClrVersion = RuntimeEnvironment.GetSystemVersion();
    }
}