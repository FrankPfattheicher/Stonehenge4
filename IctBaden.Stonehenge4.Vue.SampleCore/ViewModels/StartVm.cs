using System;
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

namespace IctBaden.Stonehenge.Vue.SampleCore.ViewModels
{
    // ReSharper disable once UnusedMember.Global
    // ReSharper disable once UnusedType.Global
    public class StartVm : ActiveViewModel
    {
        // ReSharper disable once MemberCanBeMadeStatic.Global
        public string TimeStamp => DateTime.Now.ToLongTimeString();
        public double Numeric { get; set; }
        public string Version => Assembly.GetAssembly(typeof(Program))!.GetName().Version!.ToString(2);
        public bool IsLocal => Session?.IsLocal ?? true;
        public string ClientAddress => Session.ClientAddress ?? "(unknown)";
        public string UserIdentity => Session.UserIdentity ?? "(unknown)";
        public string UserIdentityId => Session.UserIdentityId ?? "(unknown)";
        public string UserIdentityEMail => Session.UserIdentityEMail ?? "(unknown)";

        public bool ShowCookies { get; private set; }

        public string Culture { get; set; }
        
        public bool AppBoxVisible { get; private set; }
        public string AppBoxCaption { get; private set; }
        public string AppBoxText { get; private set; }
        
        public bool AppDialogVisible { get; private set; }
        public string AppDialogCaption { get; private set; }
        


        public string Parameters =>
            string.Join(", ", Session.Parameters.Select(p => $"{p.Key}={p.Value}"));

        public string NotInitialized { get; set; }

        private string _text = "This ist the content of user file ;-) Press Alt+Left to return.";

        // ReSharper disable once UnusedMember.Global
        public StartVm(AppSession session) : base(session)
        {
            Numeric = 123.456;

            SetUpdateTimer(TimeSpan.FromSeconds(2));
        }

        public override void OnLoad()
        {
            Culture = Session.SessionCulture?.ToString() ?? "";
        }

        public override void OnUpdateTimer()
        {
            var c = Thread.CurrentThread.CurrentCulture;
            var ui = Thread.CurrentThread.CurrentUICulture;
            
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
        public void ShowAppDialog()
        {
            CloseAppBox();
            AppDialogVisible = true;
            AppDialogCaption = "Stonehenge";
        }

        [ActionMethod]
        public void CloseAppBox()
        {
            AppBoxVisible = false;
            AppDialogVisible = false;
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
            if (!resourceName.EndsWith(".ics"))
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

        public override Resource PostDataResource(string resourceName, Dictionary<string, string> parameters,
            Dictionary<string, string> formData)
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
        public void ChangeCulture()
        {
            if (string.IsNullOrEmpty(Culture))
            {
                Session.SetSessionCulture(null);
                return;
            }

            var culture = new CultureInfo(Culture);
            Session.SetSessionCulture(culture);
        }

        [ActionMethod]
        public void ToggleShowCookies()
        {
            ShowCookies = !ShowCookies;
            EnableRoute("cookie", ShowCookies);
        }
    }
}