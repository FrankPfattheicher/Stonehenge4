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
