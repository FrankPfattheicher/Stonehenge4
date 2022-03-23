

updated: function () {

    if(typeof(this.chart) == "undefined") {

        this.chart = c3.generate({
            bindto: this.$el,
            title: this.$props.chartdata.Title,
            data: this.$props.chartdata.Data,
            axis: this.$props.chartdata.Axis,
            grid: this.$props.chartdata.Grid,
            point: this.$props.chartdata.Point,
            size: this.$props.chartdata.Size
        });
    }

    this.chart.load({
        columns: this.$props.chartdata.Data.columns
    });

}
