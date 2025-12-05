mounted: function () {
    if (typeof (this.$props.chartdata.Id) != "undefined") {

        if(typeof (this.chart) != "undefined") {
            this.chart.destroy();
        }

        const formats = this.$props.chartdata.Data.formats;
        app['ChartFormats' + this.$props.chartdata.Id] = function (value, ratio, id) {
            try {
                const format = formats[ratio] ?? '';
                return d3.format(format)(value);
            } catch {
                return d3.format('')(value);
            }
        }
        this.chart = c3.generate({
            bindto: this.$el,
            id: this.$props.chartdata.Id,
            title: this.$props.chartdata.Title,
            grid: this.$props.chartdata.Grid,
            data: {
                onclick: function (d, element) {
                    //debugger;
                    let event = new CustomEvent("clickData", {
                        bubbles: true,
                        detail: d
                    });
                    element.dispatchEvent(event);
                },
                labels: {
                    format:  app['ChartFormats' + this.$props.chartdata.Id]
                },
                ...this.$props.chartdata.Data
            },
            axis: this.$props.chartdata.Axis,
            point: this.$props.chartdata.Point,
            regions: this.$props.chartdata.Regions,
            zoom: this.$props.chartdata.Zoom,
            size: {
                width: this.$el.clientWidth,
                height: this.$el.clientHeight,
            },
            tooltip: {
                format: {
                    value: app['ChartFormats' + this.$props.chartdata.Id]
                },
                contents: function (d, defaultTitleFormat, defaultValueFormat, color) {
                    let $$ = this, config = $$.config,
                        titleFormat = config.tooltip_format_title || defaultTitleFormat,
                        nameFormat = config.tooltip_format_name || function (name) {
                            return name;
                        },
                        valueFormat = config.tooltip_format_value || defaultValueFormat,
                        text, i, title, value, name, bgcolor;

                    switch($$.sortSeriesTooltips) {
                        case 1:
                            d.sort(function(a, b) { return b.value - a.value; });  // descend value
                            break;
                        case 2:
                            d.sort(function(a, b) { return a.value - b.value; });  // ascend value
                            break;
                        case 3:
                            d.sort(function(a, b) { return a.name > b.name ? 1 : -1; });  // ascend title
                            break;
                        case 4:
                            d.sort(function(a, b) { return a.name < b.name ? 1 : -1; });  // descend title
                            break;
                    }

                    for (i = 0; i < d.length; i++) {
                        if (!(d[i] && (d[i].value || d[i].value === 0))) {
                            continue;
                        }
                        if (!text) {
                            title = titleFormat ? titleFormat(d[i].x) : d[i].x;
                            text = "<table class='" + $$.CLASS.tooltip + "'>" + (title || title === 0 ? "<tr><th colspan='2'>" + title + "</th></tr>" : "");
                        }
                        name = nameFormat(d[i].name);
                        value = valueFormat(d[i].value, d[i].ratio, d[i].id, d[i].index);
                        bgcolor = $$.levelColor ? $$.levelColor(d[i].value) : color(d[i].id);
                        text += "<tr class='" + $$.CLASS.tooltipName + "-" + d[i].id + "'>";
                        text += "<td class='name'><span style='background-color:" + bgcolor + "'></span>" + name + "</td>";
                        text += "<td class='value'>" + value + "</td>";
                        text += "</tr>";
                    }
                    return text + "</table>";
                }
            }
        });
    }
    if(this.chart && typeof (this.chart.internal.sortSeriesTooltips) != "undefined") {
        this.chart.internal.sortSeriesTooltips = this.$props.chartdata.SortSeriesTooltips;
    }
    this.chartId = this.$props.chartdata.Id;
}

,
updated: function () {

    if (typeof (this.chart) == "undefined" || this.chartId != this.$props.chartdata.Id) {

        if(typeof (this.chart) != "undefined") {
            this.chart.destroy();
        }

        const formats = this.$props.chartdata.Data.formats;
        app['ChartFormats' + this.$props.chartdata.Id] = function (value, ratio, id) {
            try {
                const format = formats[ratio] ?? '';
                return d3.format(format)(value);
            } catch {
                return d3.format('')(value);
            }
        }
        this.chart = c3.generate({
            bindto: this.$el,
            title: this.$props.chartdata.Title,
            grid: this.$props.chartdata.Grid,
            data: {
                onclick: function (d, element) {
                    //debugger;
                    let event = new CustomEvent("clickData", {
                        bubbles: true,
                        detail: d
                    });
                    element.dispatchEvent(event);
                },
                labels: {
                    format: app['ChartFormats' + this.$props.chartdata.Id]
                },
                ...this.$props.chartdata.Data
            },
            axis: this.$props.chartdata.Axis,
            point: this.$props.chartdata.Point,
            regions: this.$props.chartdata.Regions,
            zoom: this.$props.chartdata.Zoom,
            size: {
                width: this.$el.clientWidth,
                height: this.$el.clientHeight,
            },
            tooltip: {
                format: {
                    value: app['ChartFormats' + this.$props.chartdata.Id]
                },
                contents: function (d, defaultTitleFormat, defaultValueFormat, color) {
                    let $$ = this, config = $$.config,
                        titleFormat = config.tooltip_format_title || defaultTitleFormat,
                        nameFormat = config.tooltip_format_name || function (name) {
                            return name;
                        },
                        valueFormat = config.tooltip_format_value || defaultValueFormat,
                        text, i, title, value, name, bgcolor;

                    switch($$.sortSeriesTooltips) {
                        case 1:
                            d.sort(function(a, b) { return b.value - a.value; });  // descend value
                            break;
                        case 2:
                            d.sort(function(a, b) { return a.value - b.value; });  // ascend value
                            break;
                        case 3:
                            d.sort(function(a, b) { return a.name > b.name ? 1 : -1; });  // ascend title
                            break;
                        case 4:
                            d.sort(function(a, b) { return a.name < b.name ? 1 : -1; });  // descend title
                            break;
                    }

                    for (i = 0; i < d.length; i++) {
                        if (!(d[i] && (d[i].value || d[i].value === 0))) {
                            continue;
                        }
                        if (!text) {
                            title = titleFormat ? titleFormat(d[i].x) : d[i].x;
                            text = "<table class='" + $$.CLASS.tooltip + "'>" + (title || title === 0 ? "<tr><th colspan='2'>" + title + "</th></tr>" : "");
                        }
                        name = nameFormat(d[i].name);
                        value = valueFormat(d[i].value, d[i].ratio, d[i].id, d[i].index);
                        bgcolor = $$.levelColor ? $$.levelColor(d[i].value) : color(d[i].id);
                        text += "<tr class='" + $$.CLASS.tooltipName + "-" + d[i].id + "'>";
                        text += "<td class='name'><span style='background-color:" + bgcolor + "'></span>" + name + "</td>";
                        text += "<td class='value'>" + value + "</td>";
                        text += "</tr>";
                    }
                    return text + "</table>";
                }
            }
        });

        if(this.chart && typeof (this.chart.internal.sortSeriesTooltips) != "undefined") {
            this.chart.internal.sortSeriesTooltips = this.$props.chartdata.SortSeriesTooltips;
        }
        this.chartId = this.$props.chartdata.Id;
    }

    self.temp_domain = this.chart.zoom() ? this.chart.internal.x.orgDomain() : null;
    self.temp_chart = this.chart;
    this.chart.load({
        columns: this.$props.chartdata.Data.columns,
        onclick: function (d, element) {
            emit('clickData', d, element);
        },
        done: function() {
            // Nach dem Update den alten Zoom wiederherstellen
            // Not working if new data is added
            // if (self.temp_domain) {
            //     self.temp_chart.zoom(self.temp_domain);
            // }
        }
    });

}
