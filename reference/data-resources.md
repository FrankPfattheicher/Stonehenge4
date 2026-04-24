# Data Resources

Data resources are a feature in Stonehenge that allows you to provide and retrieve data associated with a specific session. 
They are useful for data that is specific to a user's session, such as user downloads as csv files.

Data resources are accessed using a URI that includes the data resource ID and the resource name. 
The URI format is `/Data_{dataResourceId}/{resourceName}` and can be retrieved by the ActiveViewModel's `GetDataResourceUri` method, 
which returns the URI for a given resource name. For example, `GetDataResourceUri("export.csv")`.

The URI can be used in a link to download the resource by binding it to the href attribute of an anchor tag.
``` html
    <a :href="DataResourceUri">Download Data</a>
```
Code to return the data in the view model:
``` csharp
    public override Resource GetDataResource(string resourceName)
    {
        const string cal = """
                           BEGIN:VCALENDAR
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
                           """;
        return new Resource(resourceName, "Sample", ResourceType.Calendar, cal, Resource.Cache.None);
    }
```

