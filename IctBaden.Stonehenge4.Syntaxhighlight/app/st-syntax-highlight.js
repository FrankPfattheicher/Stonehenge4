
mounted: async function() {

    console.log('syntax-highlight mounted');
    try {
        //debugger;
        hljs.highlightAll();
        
        const source = this.$props.source ?? "";
        const language = this.$props.language ?? "";
        if(language !== "") {
            const highlightedCode = hljs
                .highlight(
                    source,
                    { language: language }
                ).value;
            this.$el.innerHTML = highlightedCode;
        }
    } catch(ex) {
        console.log("Highliter: " + ex)
    }
},
updated: async function () {

    console.log('syntax-highlight updated');
    try {
        debugger;
        const source = this.$props.source ?? "";
        const language = this.$props.language ?? "xml";
        const highlightedCode = hljs
            .highlight(
                source,
                { language: language }
            ).value;
        this.$el.innerHTML = highlightedCode;
    } catch(ex) {
        console.log("Highliter: " + ex)
    }

}
