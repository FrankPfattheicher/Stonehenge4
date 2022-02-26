

updated: function () {

    if(typeof(this.chart) == "undefined") {

        this.chart = c3.generate({
            bindto: this.$el,
            title: this.$props.chartdata.Title,
            data: this.$props.chartdata.Data,
            axis: this.$props.chartdata.Axis,
            point: this.$props.chartdata.Point
        });
    }

    this.chart.load({
        columns: this.$props.chartdata.Data.columns
    });

}
