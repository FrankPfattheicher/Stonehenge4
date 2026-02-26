# Reusable Components
Components consist of the html view and the corresponding C# view model.

## Components ViewModel
The ViewModel has to be derived from StonehengeComponent.

``` csharp
public class ComputerInfo : StonehengeComponent
{
    public string Name => Environment.MachineName;
}
```

## Components View
The view is defined in a html file.

    computer-info.html

The first line has to be 
```html
<!--CustomElement:model-->
```
The second line names the view model to be used.
```html
<!--ViewModel:ComputerInfo-->
```

The complete component view should be a &lt;div&gt; element.    
Properties of the view model can be accessed using the **model** element. 

```html
<!--CustomElement:model-->
<!--ViewModel:ComputerInfo-->
<div>
    <p>Hello Client from computer {{model.Name}}!</p>
</div>
```

## Components Use
The component is published as html-tag with the name of the component's view file name.

The **model** property has to be bound to the component's view model instance.

```html
<div>
    <computer-info :model="Info" ></computer-info>
</div>
```

The corresponding view model property is of the type of the component. 

``` csharp
public ComputerInfo Info { get; private set; } = new();
```


