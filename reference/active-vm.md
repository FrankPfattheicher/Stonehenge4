
## Active View Model

### OnLoad

    void OnLoad()

### GetDataResource

    Resource? GetDataResource(string resourceName)
    Resource? GetDataResource(string resourceName, IDictionary<string, string> parameters)

### PostDataResource

    Resource? PostDataResource(string resourceName, IDictionary<string, string> parameters, IDictionary<string, string> formData)

### SetUpdateTimer

    void SetUpdateTimer(int updateMs)
    void SetUpdateTimer(TimeSpan update)

### StopUpdateTimer

    void StopUpdateTimer()

### OnUpdateTimer

    void OnUpdateTimer()

### OnDispose

    void OnDispose()

### OnWindowResized

    void OnWindowResized(int width, int height)


