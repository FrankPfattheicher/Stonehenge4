
* 8.2.1 - 08.02.2025 Internationalization support added tor route titles
* 8.2.0 - 03.02.2025 Internationalization support added using RESX files

* 8.1.4 - 18.01.2025 LogCritical "Failed to create ViewModel"
* 8.1.3 - 18.12.2024 Fixed System.IdentityModel.Tokens.Jwt packet reference
* 8.1.2 - 06.12.2024 Updated mermaid.js to v11.4.1, do not display parse errors
* 8.1.1 - 18.11.2024 Remove stonehenge-id from query if session not found
* 8.1.0 - 18.11.2024 Do NOT use stonehenge id in URL, remove login redirect parameters

* 8.0.11 - 06.11.2024 Added SortSeriesTooltips to Chart
* 8.0.10 - 28.08.2024 ViewModelProvider use property name instead of type name
* 8.0.9 - 28.08.2024 Improve ViewModelProvider error logs including name
* 8.0.8 - 02.08.2024 CollectEvents with CancellationToken, handle X-Forwarded-Host header
* 8.0.7 - 22.07.2024 Fixed _eventRelease handle CancellationTokenSource exception
* 8.0.6 - 13.07.2024 Fixed nuget reference
* 8.0.5 - 12.07.2024 Improved CollectEvents, added log, avoid reentrancy
* 8.0.4 - 10.07.2024 Removed -c option on midori host window
* 8.0.3 - 26.06.2024 app-dialog enableOk defaults to true
* 8.0.2 - 18.06.2024 Fixed SetUserNameFromContext
* 8.0.1 - 17.06.2024 Add Cors StonehengePolicy as in Stonehenge3
* 8.0.0 - 14.06.2024 Drop net6.0 support and AspNetCore 2.x

* 4.9.3 - 18.05.2024 Added enableOk to app-dialog 
* 4.9.2 - 08.05.2024 Make AppSessions non static, rename extension DropEdit to more generic name Forms, 
                     added st-error-message, IStonehengeHost.GetAllSessions()
* 4.9.1 - 23.04.2024 CreateType using default values for value types 
* 4.9.0 - 20.04.2024 EnableRoute w/o exec client script, added IsRouteEnabled, C3 charts method UpdateId

* 4.8.2 - 15.04.2024 static class AppSessions to handle sessions thread safe
* 4.8.1 - 14.04.2024 Initial session timeout 10sec, provide empty map resources
* 4.8.0 - 29.03.2024 Using IDisposableAnalyzers, removed AppSession SetTerminator

* 4.7.1 - 11.03.2024 Packets updated, static logging templates, AppSessions moved to AppSession  
* 4.7.0 - 08.03.2024 Fixed memory leak and improved UserContentLinks

* 4.6.8 - 08.03.2024 Fixed "No VM for type  defined."
* 4.6.7 - 04.03.2024 CSS display st-nav '' or 'none'
* 4.6.6 - 27.02.2024 Added UserContentLinks.AddStyleSheet, removing sample custom index.html
* 4.6.5 - 24.02.2024 On error hide nav bar and reload page
* 4.6.4 - 06.01.2024 C#12, options with SessionTimeout
* 4.6.3 - 13.12.2023 Pie chart uses element size
* 4.6.2 - 13.12.2023 Fixed session timeout, added pie chart sector colors
* 4.6.1 - 05.12.2023 Allow empty query parameters
* 4.6.0 - 27.11.2023 Nullable enabled
 
* 4.5.5 - 25.11.2023 Decode user agent, session.OnNavigate, dispose AppSession
* 4.5.4 - Support POST request with content type application/x-www-form-urlencoded
* 4.5.3 - Use setting PollRetries (default 1) to avoid StonehengeIsDisconnected on single failure
* 4.5.2 - Handle PATCH requests same as PUT, ChartSeries.Data as object?[]
* 4.5.1 - Fixed async POST, PUT and DELETE requests, tests added
* 4.5.0 - IStonehengeResourceProvider added PUT and DELETE

* 4.4.7 - TreeNode set methods to trigger state provider
* 4.4.6 - TreeNode enable setting id in constructor
* 4.4.5 - TreeNode enable setting id on init
* 4.4.4 - TreeView IExpandedProvider extended to IStateProvider
* 4.4.3 - ChartsC3 KnownColor replaced by generic Color
* 4.4.2 - Fixed read POST content using buffering 
* 4.4.1 - Added UploadFile function, added TreeVie checkbox and HTML node support
* 4.4.0 - Added Mermaid extension, Serialize view model first, then Stonehenge internal properties
* 4.3.16 - Resource type EOT, fix query keys contains empty key exception
* 4.3.15 - TreeView refactored, nullable enabled
* 4.3.14 - TreeView extension ready to use
* 4.3.13 - C# 11, Tests nullable, Session.CreateType log error "No public constructors"
* 4.3.12 - DropEdit border-spacing fixed
* 4.3.11 - DropEdit control size fixed
* 4.3.10 - Added DropEdit control st-drop-edit
* 4.3.9 - Fixed app context check on linux
* 4.3.8 - Added chart clickData event
* 4.3.7 - Added bar chart group functionality
* 4.3.6 - Not ok status code < 200 and >= 400
* 4.3.5 - Retry get events on non ok status code
* 4.3.4 - AppDialog/AppMessageBox options cancelText,okText
* 4.3.3 - AppDialog close button using fontawesome icon
* 4.3.2 - Initial culture support
* 4.3.1 - C3 pie chart added, Bootstrap v5.2.3 incl. JS
* 4.2.12 - NavigateToRoute enable http URLs, Keycloak redirects from browser
* 4.2.11 - CORS inline policy
* 4.2.10 - CORS WithOrigins *
* 4.2.9 - Clear NavigateToRoute only if navigation happens
* 4.2.8 - Remove AddStonehengeInternalProperties from events (caused lost navigation)
* 4.2.7 - StopUpdateTimer, prevent navigate to current route
* 4.2.6 - Serialize StonehengeNavigate after StonehengeEval 
* 4.2.5 - Keycloak logout fixed
* 4.2.4 - Fixed CustomMiddleware by type name
* 4.2.3 - Added ActiveViewModel.ReloadPage
* 4.2.2 - Added DoubleConverter to avoid invalid double value serialization exceptions
* 4.2.1 - AuthorizeRedirectUrl with timestamp
* 4.2.0 - UseKeycloakAuthentication
* 4.1.3 - Make ChartGridLine non-abstract
* 4.1.2 - Added property position to ChartGridLine
* 4.1.1 - Fix Sankey node text positioning
* 4.1.0 - Using async tasks, ActiveViewModel OnUpdateTimer, OnDispose, Sankey chart
* 4.0.26 - Updated nuget references, added extensions
* 4.0.25 - Deserialization test with current and international culture
* 4.0.24 - Deserialization of floating point values works with current and international format
* 4.0.23 - Use InvariantCulture in converting action method parameters
* 4.0.22 - Fixed Nuget package references
* 4.0.21 - Namespace fixed
* 4.0.20 - Fixed kestrel initialization
* 4.0.19 - Fixed element type in deserialization
* 4.0.18 - ViewModelProvider deserialize arrays of structs
* 4.0.17 - Added StonehengeUiWindow and SDK check in Kestrel host
* 4.0.16 - Fixed nuget packet references
* 4.0.15 - Handle empty struct and DateTimeOffset deserialization
* 4.0.14 - Fix array property deserialization
* 4.0.13 - Generalize DeserializeStructValue using JsonObject
* 4.0.12 - ViewModelProvider added DeserializeStructValue
* 4.0.11 - Cookies replaced by X-Stonehenge-Id header, option removed
* 4.0.10 - Added --no-first-run to chrome command line
* 4.0.9 - Added missing bootstrap-vue.min.js
* 4.0.8 - NavigateBack
* 4.0.7 - IStonehengeExtension
* 4.0.6 - ChartsC3 DLL
* 4.0.5 - Removed version "4" from namespace
* 4.0.4 - FontAwesome 6.0.0, Bootstrap 5.1.3
* 4.0.3 - Manually merged stonehenge3 v3.24.01 changes
* 4.0.2 - FontAwesome 6.0.0-beta2, Bootstrap 5.1.3 
* 4.0.1 - Removed Newtonsoft.Json, removed SetModel support
* 4.0.0 - Start porting to NET 6
