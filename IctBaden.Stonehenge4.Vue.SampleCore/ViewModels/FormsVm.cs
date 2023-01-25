using System;
using IctBaden.Stonehenge.Core;
using IctBaden.Stonehenge.ViewModel;
// ReSharper disable MemberCanBePrivate.Global

namespace IctBaden.Stonehenge.Vue.SampleCore.ViewModels;

public class FormsVm : ActiveViewModel
{
    public string TimeText => DateTime.Now.ToString("G");

    public string RefreshText { get; private set; }
    
    public FormsVm(AppSession session)
    : base(session)
    {
        SetRefresh(0);
    }

    [ActionMethod]
    public void Refresh()
    {
    }

    [ActionMethod]
    public void SetRefresh(int seconds)
    {
        switch (seconds)
        {
            case 0: RefreshText = "Aus";
                break;
            case 1: RefreshText = "1s";
                break;
            case 10: RefreshText = "10s";
                break;
            case 30: RefreshText = "30s";
                break;
            case 60: RefreshText = "1min";
                break;
            case 300: RefreshText = "5min";
                break;
        }
        
        if(seconds == 0)
            StopUpdateTimer();
        else
            SetUpdateTimer(TimeSpan.FromSeconds(seconds));
    }

    public override void OnUpdateTimer()
    {
        NotifyPropertyChanged(nameof(TimeText));
    }
}