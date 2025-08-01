# Stonehenge 4
An open source .NET Framework to use Web UI technologies for desktop and/or web applications based on the MVVM pattern.

* See a (very) short [getting started introduction here](docs/GettingStarted.md).
* Core functionality [active view models](docs/active-vm.md)
* Creating [reusable components](docs/ReusableComponents.md)


## Version 4
This version is based on .NET 8. 

**Attention:** Microsoft.NET.Sdk.Web is required!

With this version the SimpleHost is removed. Only Kestrel self and IIS hosting is supported.
Also Newtonsoft.JSON is removed, using NET's own JSON serializer.


Used technology

* [Kestrel](https://docs.microsoft.com/de-de/aspnet/core/fundamentals/servers/kestrel) - the Microsoft netcore web stack for self hosting
* [Vue.js 2](https://vuejs.org/) client framework (bootstrap-vue currently not support Vue 3)
* [Bootstrap 5](https://getbootstrap.com/) front-end open source toolkit
* [Fontawesome 6](https://fontawesome.com/) icon library

Read the release history: [ReleaseNotes](ReleaseNotes3.md)

## Other Projects supporting the same technology stack

* [DotVVM - Interactive web apps with just C# and HTML](https://www.dotvvm.com/) 

## Still there
* v3.x - Net Core 3.1 based
* v3.6 - Aurelia client framework (deprecated, included up to v3.6 only)
* V2.x - (deprecated) .NET Full Framework V4.6, Katana, Aurelia
* V1.x - .NET Full Framework V4.6, ServiceStack, Knockout


**SampleFull with target framework V4.7.1**    
The application is able to use netstandard 2.0 libraries adding the following lines to the csproj file.

	<AutoGenerateBindingRedirects>true</AutoGenerateBindingRedirects>
  	<GenerateBindingRedirectsOutputType>true</GenerateBindingRedirectsOutputType>

