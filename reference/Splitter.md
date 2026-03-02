
## Splitter

To use a splitter you need to add a div with two div pane's and a `st-splitter` component between to your page.


The splitter will be horizontal (column) if the parent div has a flex-direction of row and vertical (row) if the parent div has a flex-direction of column.

``` html

    <div style="display: flex; flex-direction: row; height: 50vh;">
    
        <div style="border: 1px solid gray; padding: 0.5em; width: 50%;">
            First pane
        </div>
        
        <st-splitter :model="Splitter"></st-splitter>
        
        <div style="border: 1px solid gray; padding: 0.5em; width: 50%;">
            Second pane
        </div>
    
    </div>

```

If you want to handle the splitter-moved event, you can use the `SplitterMoved` event of the component.

The event handler will be called with the first and second pane's width (column) or height (row).

``` csharp

    public Splitter Splitter { get; } = new();
    
    ctor()
    {
        Splitter.SplitterMoved += SplitterMoved;
    }
    
    private void SplitterMoved(int first, int second)
    {
        // handle splitter moved event
    }

```

