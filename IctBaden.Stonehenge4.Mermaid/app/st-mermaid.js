
mounted: async function() {
    
    mermaid.initialize({
        startOnLoad: false,
        suppressErrors: true
    });

    const ts = new Date().getMilliseconds();
    const id = 'id' + ts + Math.floor(Math.random() * 100);
    const graphData = this.$props.graphData;
    try {
        if (await mermaid.parse(graphData)) {
            mermaid.render(id, graphData)
                .then(({svg, bindFunctions}) => {
                    this.$el.innerHTML = svg;
                });
        }
    } catch(ex) {
        if(graphData !== '') {
            console.log("Mermaid: " + ex)
            //debugger;
        }
        this.$el.innerHTML = '';
    }
},
updated: async function () {

    const ts = new Date().getMilliseconds();
    const id = 'id' + ts + Math.floor(Math.random() * 100);
    const graphData = this.$props.graphData;
    try {
        if (await mermaid.parse(graphData)) {
            mermaid.render(id, graphData)
                .then(({svg, bindFunctions}) => {
                    this.$el.innerHTML = svg;
                });
        }
    } catch(ex) {
        if(graphData !== '') {
            console.log("Mermaid: " + ex)
            //debugger;
        }
        this.$el.innerHTML = '';
    }

}
