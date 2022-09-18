
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

    //if(typeof(this.chart) == "undefined" || this.chartId != this.$props.chartdata.Id) 
    {

        data = {
                nodes: this.$props.chartdata.Nodes,
                links: this.$props.chartdata.Links
            };

        const margin = {top: 10, right: 10, bottom: 10, left: 10};

        let svg = d3.select("#sankey");
        var width = svg.node().clientWidth - margin.left - margin.right;
        var height = svg.node().clientHeight - margin.top - margin.bottom;

        
        const sankey = d3
            .sankey()
            .size([width, height])
            .nodeId(d => d.id)
            .nodeWidth(40)
            .nodePadding(10)
            .nodeAlign(d3.sankeyCenter);
        
        let graph = sankey(data);

        svg.selectAll("*").remove();
        
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
            .attr("stroke", "silver")
            .attr("stroke-width", d => d.width)
            .attr("stoke-opacity", 0.5)
            .append("title")
            .text(function(d) { return d.source.id + "->" + d.target.id +"\n" + d.value + "kW"; });
        
       
        let nodes = svg
            .append("g")
            .classed("nodes", true)
            .selectAll("rect")
            .data(graph.nodes)
            .enter();
        
        nodes
            .append("rect")
            .classed("node", true)
            .attr("x", d => d.x0)
            .attr("y", d => d.y0)
            .attr("width", d => d.x1 - d.x0)
            .attr("height", d => d.y1 - d.y0)
            .style("fill",  "lightskyblue")
            .style("stroke", "black")
            .attr("opacity", 0.8);
        
        nodes
            .append("text")
            .attr("x",  function(d) { return d.x0 - 6; })
            .attr("y", function(d) { return (d.y1 - d.y0) / 2; })
            .attr("dy", ".35em")
            .attr("text-anchor", "end")
            .attr("transform", null)
            .attr("fill", "black")
            .text(function(d) { return d.id; })
            .filter(function(d) { return d.x0 <= 0; })
            .attr("x", 6 + sankey.nodeWidth())
            .attr("y", function(d) { return d.y0 + (d.y1 - d.y0) / 2; })
            .attr("text-anchor", "start");

        this.chart = sankey;
        this.chartId = this.$props.chartdata.Id;
    }


}
