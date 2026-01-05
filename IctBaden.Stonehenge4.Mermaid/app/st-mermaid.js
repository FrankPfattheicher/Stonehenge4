
mounted: async function() {

    console.log("Mermaid: mounted");

    const display = this.$el.style.display;
    this.$el.style.display = 'none';

    const mermaidModule = await import("./src/mermaid.esm.mjs");
    const elkLayoutsModule = await import("./src/mermaid-layout-elk.esm.mjs");
    const mermaid = mermaidModule.default;
    const elkLayouts = elkLayoutsModule.default;

    mermaid.registerLayoutLoaders(elkLayouts);
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
    this.$el.style.display = display;
},
updated: async function () {

    console.log("Mermaid: updated");

    const display = this.$el.style.display;
    this.$el.style.display = 'none';

    const mermaidModule = await import("./src/mermaid.esm.mjs");
    const mermaid = mermaidModule.default;

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
    this.$el.style.display = display;

}
