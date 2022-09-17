
mounted: function() {
    // if(typeof(this.$props.chartdata.Id) != "undefined") {
    //     this.chart = c3.generate({
    //         bindto: this.$el,
    //         id: this.$props.chartdata.Id,
    //         title: this.$props.chartdata.Title,
    //         grid: this.$props.chartdata.Grid,
    //         data: this.$props.chartdata.Data,
    //         axis: this.$props.chartdata.Axis,
    //         point: this.$props.chartdata.Point,
    //         zoom: this.$props.chartdata.Zoom,
    //         size: this.$props.chartdata.Size
    //     });
    // }
    // this.chartId = this.$props.chartdata.Id;
},
updated: function () {

    if(typeof(this.chart) == "undefined" || this.chartId != this.$props.chartdata.Id) {

        var margin = {top: 10, right: 10, bottom: 10, left: 10},
            width = 800 - margin.left - margin.right,
            height = 500 - margin.top - margin.bottom;

        var formatNumber = d3.format(",.0f"),    // zero decimal places
            format = function(d) { return formatNumber(d) + " " + units; };

        // append the svg canvas to the page
        var svg = d3.select("#sankey").append("svg")
            .attr("width", width + margin.left + margin.right)
            .attr("height", height + margin.top + margin.bottom)
            .append("g")
            .attr("transform",
                "translate(" + margin.left + "," + margin.top + ")");

        // Set the sankey diagram properties
        var sankey = d3.sankey()
            .size([width, height])
            .nodeId(d => d.id)
            .nodeWidth(36)
            .nodePadding(10)
            .nodeAlign(d3.sankeyCenter);

        var data = {
        nodes : [
            {"id": "Alice"},
            {"id": "Bert"},
            {"id": "Bob"},
            {"id": "Carol"}
        ],
        links : [
            {"source": "Alice", "target": "Bob", value: 10 },
            {"source": "Bert", "target": "Bob", value: 5 },
            {"source": "Bob", "target": "Carol", value: 20 }
        ]
        };

        data = {
            nodes: this.$props.chartdata.Nodes,
            links: this.$props.chartdata.Links
        };

        debugger;
        
        let graph = sankey(data);

        let links = svg
            .append("g")
            .classed("links", true)
            .selectAll("path")
            .data(graph.links)
            .enter()
            .append("path")
            .classed("link", true)
            .attr("d", d3.sankeyLinkHorizontal())
            .attr("fill", "none")
            .attr("stroke", "#606060")
            .attr("stroke-width", d => d.width)
            .attr("stoke-opacity", 0.5);

        let nodes = svg
            .append("g")
            .classed("nodes", true)
            .selectAll("rect")
            .data(graph.nodes)
            .enter()
            .append("rect")
            .classed("node", true)
            .attr("x", d => d.x0)
            .attr("y", d => d.y0)
            .attr("width", d => d.x1 - d.x0)
            .attr("height", d => d.y1 - d.y0)
            .attr("fill", "blue")
            .attr("opacity", 0.8);

        nodes
            .append("text")
           .attr("x", -6)
           .attr("y", function(d) { return d.dy / 2; })
           .attr("dy", ".35em")
            .attr("fill", "black")
            .attr("stroke", "black")
            .attr("text-anchor", "end")
            .attr("transform", null)
            .text(function(d) { return d.id; })
            .filter(function(d) { return d.x < width / 2; })
            .attr("x", 6 + sankey.nodeWidth())
            .attr("text-anchor", "start");

        this.chart = sankey;
        this.chartId = this.$props.chartdata.Id;
    }


}
