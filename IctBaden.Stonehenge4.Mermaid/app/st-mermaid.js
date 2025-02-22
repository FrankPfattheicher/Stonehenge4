
mounted: async function() {
    
    mermaid.initialize({
        startOnLoad: false,
        suppressErrors: true
    });
    
    const ts = new Date().getTime();
    const id = 'id' + ts;
    const graphData = this.$props.graphData;
    try {
        if (await mermaid.parse(graphData)) {
            mermaid.render(id, graphData)
                .then(({svg, bindFunctions}) => {
                    this.$el.innerHTML = svg;
                });
        }
    } catch {
        this.$el.innerHTML = '';
    }
},
updated: async function () {
    
    const ts = new Date().getTime();
    const id = 'id' + ts;
    const graphData = this.$props.graphData;
    try {
        if (await mermaid.parse(graphData)) {
            mermaid.render(id, graphData)
                .then(({svg, bindFunctions}) => {
                    this.$el.innerHTML = svg;
                });
        }
    } catch {
        this.$el.innerHTML = '';
    }

}
