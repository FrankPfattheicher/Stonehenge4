﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using IctBaden.Stonehenge.Core;
using IctBaden.Stonehenge.Resources;
using IctBaden.Stonehenge.ViewModel;

// ReSharper disable StringLiteralTypo
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable UnusedMember.Global

namespace IctBaden.Stonehenge.Vue.SampleCore.ViewModels;

// ReSharper disable once UnusedMember.Global
// ReSharper disable once UnusedType.Global
public class StartVm : ActiveViewModel
{
    // ReSharper disable once MemberCanBeMadeStatic.Global
    public string TimeStamp => DateTime.Now.ToLongTimeString();
    public double Numeric { get; set; }
    public string Version => Assembly.GetAssembly(typeof(Program))!.GetName().Version!.ToString(2);
    public bool IsLocal => Session.IsLocal;
    public string Browser => Session.Browser;
    public string Platform => Session.Platform;
    public string ClientAddress => Session.ClientAddress;
    public string UserIdentity => string.IsNullOrEmpty(Session.UserIdentity) ? "<none>" : Session.UserIdentity;
    public string UserIdentityId => Session.UserIdentityId;
    public string UserIdentityEMail => Session.UserIdentityEMail;
    public int SessionCount => Session.SessionCount;

    public bool ShowCookies => IsRouteEnabled("cookie");

    public string Culture { get; set; } = string.Empty;
    public string UploadFile { get; set; } = string.Empty;

    public bool AppBoxVisible { get; private set; }
    public string AppBoxCaption { get; private set; } = string.Empty;
    public string AppBoxText { get; private set; } = string.Empty;

    public bool AppDialogVisible1 { get; private set; }
    public bool AppDialogVisible2 { get; private set; }
    public string AppDialogCaption { get; private set; } = string.Empty;


    public string Parameters =>
        string.Join(", ", Session.Parameters.Select(p => $"{p.Key}={p.Value}"));

    public string SessionCulture =>
        string.Join(", ", Session.SessionCulture.ToString());

    public string? NotInitialized { get; set; }

    private string _text = "This ist the content of user file ;-) Press Alt+Left to return.";

    // ReSharper disable once UnusedMember.Global
    public StartVm(AppSession session) : base(session)
    {
        Numeric = 123.456;
        SetUpdateTimer(TimeSpan.FromSeconds(2));
    }

    public override void OnLoad()
    {
        Session.OnNavigate += route => Console.WriteLine(@"Session.OnNavigate " + route);
        Culture = Session.SessionCulture.ToString();
    }

    public override void OnUpdateTimer()
    {
        // ReSharper disable UnusedVariable
        var c = Thread.CurrentThread.CurrentCulture;
        var ui = Thread.CurrentThread.CurrentUICulture;
        // ReSharper restore UnusedVariable

        NotifyPropertyChanged(nameof(TimeStamp));
    }


    [ActionMethod]
    public void ShowMessageBox()
    {
        MessageBox("Stonehenge", $"Server side browser message box request.");
    }

    [ActionMethod]
    public void ShowAppBox()
    {
        CloseAppBox();
        AppBoxVisible = true;
        AppBoxCaption = "Stonehenge";
        AppBoxText = $"Server side application box request.";
    }

    [ActionMethod]
    public void ShowAppDialog1()
    {
        CloseAppBox();
        AppDialogVisible1 = true;
        AppDialogCaption = "Stonehenge";
    }

    [ActionMethod]
    public void ShowAppDialog2()
    {
        CloseAppBox();
        AppDialogVisible2 = true;
        AppDialogCaption = "Stonehenge disable OK";
    }

    [ActionMethod]
    public void CloseAppBox()
    {
        AppBoxVisible = false;
        AppDialogVisible1 = false;
        AppDialogVisible2 = false;
    }

    [ActionMethod]
    public void NavigateToTree()
    {
        NavigateTo("tree");
    }

    [ActionMethod]
    public void NavigateOnPage()
    {
        NavigateTo("#pagetop");
    }

    [ActionMethod]
    public void UserLogin()
    {
        Session.UserLogin();
    }

    [ActionMethod]
    public void UserLogout()
    {
        Session.UserLogout();
    }

    public override Resource GetDataResource(string resourceName)
    {
        if (!resourceName.EndsWith(".ics", StringComparison.OrdinalIgnoreCase))
        {
            return new Resource(resourceName, "Sample", ResourceType.Text, _text, Resource.Cache.None);
        }

        const string cal = @"BEGIN:VCALENDAR
PRODID:-//ICT Baden GmbH//Framework Library 2016//DE
VERSION:2.0
CALSCALE:GREGORIAN
METHOD:PUBLISH
BEGIN:VEVENT
UID:902af1f31c454e5983d707c6d7ee3d4a
DTSTART:20160501T181500Z
DTEND:20160501T194500Z
DTSTAMP:20160501T202905Z
CREATED:20160501T202905Z
LAST-MODIFIED:20160501T202905Z
TRANSP:OPAQUE
STATUS:CONFIRMED
ORGANIZER:ARD
SUMMARY:Tatort
END:VEVENT
END:VCALENDAR
";
        return new Resource(resourceName, "Sample", ResourceType.Calendar, cal, Resource.Cache.None);
    }

    public override Resource PostDataResource(string resourceName, IDictionary<string, string> parameters,
        IDictionary<string, string> formData)
    {
        var tempFileName = formData["uploadFile"];
        _text = File.ReadAllText(tempFileName);
        File.Delete(tempFileName);
        return Resource.NoContent;
    }

    [ActionMethod]
    public void ExecJavaScript()
    {
        ExecuteClientScript("var dateSpan = document.createElement('span');");
        ExecuteClientScript($"dateSpan.innerHTML = '<span style=\"color: green;\">{DateTime.Now:U}</span>';");
        ExecuteClientScript("var insert = document.getElementById('insertion-point');");
        ExecuteClientScript("insert.appendChild(dateSpan)");
        ExecuteClientScript("insert.appendChild(document.createElement('br'));");
    }

    [ActionMethod]
    public void ChangeCulture(string newCulture)
    {
        if (!string.IsNullOrEmpty(newCulture))
        {
            Culture = newCulture;
        }
        if (string.IsNullOrEmpty(Culture))
        {
            Session.SetSessionCulture(CultureInfo.CurrentUICulture);
            return;
        }

        var culture = new CultureInfo(Culture);
        Session.SetSessionCulture(culture);
    }

    [ActionMethod]
    public void ToggleShowCookies()
    {
        EnableRoute("cookie", !ShowCookies);
    }
}