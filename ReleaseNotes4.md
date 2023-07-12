
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
