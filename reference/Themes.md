
# Themes

## From Cookie
Theme can be realized by using different css files and store  selected theme in a cookie named 'theme'.


To select a theme use this code:
``` html

    <select class="form-select" v-model="Theme" v-focus style="width: unset;"
            onchange="document.cookie='theme=' + this.value; location.replace('index.html?ts=' + new Date().getTime());" >
        <option value="">Light</option>
        <option value="dark">Dark</option>
        <option value="blue">Blue</option>
    </select>

```

In the view model use this code to get the selected theme:
``` csharp

    public string Theme { get; set; } = string.Empty;

    public override void OnLoad()
    {
        Theme = Session.Cookies.TryGetValue("theme", out var theme)
            ? theme
            : string.Empty;
    }
   

```

## From Subdomain

A theme can also be taken from the subdomain.
```
http://blue.localhost:32000
```

will use the theme 'blue'.