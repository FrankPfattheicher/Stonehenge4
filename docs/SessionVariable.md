# SessionVariable Attribute

SessionVariable is an attribute that can be applied to a property or field in a class to indicate that the property should be stored in the session state. 
This allows the property to persist across multiple view model changes within the same session, providing a way to maintain stateful information between user interactions.

## Usage

```csharp
public class MyViewModel
{
    [SessionVariable]
    public string MyProperty { get; set; }
    [SessionVariable] 
    public int MyField = 5;
}
```
This stores the values of MyProperty and MyField in the session state using the property/field name as the key.

The value ist stored on disposing the view model and is restored on next creation of the view model.

## Properties with private setters
```csharp
public class MyViewModel
{
    [SessionVariable]
    public string MyProperty { get; private set; }
}
```
Properties with private setters are not supported due to it is impossible to set teh value using reflection.

The workaround is using a backing field and apply the SessionVariable attribute to that.
```csharp
public class MyViewModel
{
    [SessionVariable] 
    private string _myProperty;
    public string MyProperty 
    {
        get return _myProperty; 
        private set => _myProperty = value;
    }
}
```

## SessionVariable in Template ViewModels
Using a templated class as view model is also supported.

To use typed SessionVariables, different for each template instance,
you can specify the type name as "<T>" a parameter to the attribute.

"<T>" will be replaced by the concrete type name of the template instance.

```csharp
public class MyViewModel
{
    [SessionVariable("<T>.MyProperty")]
    public string MyProperty { get; set; }
}
```
