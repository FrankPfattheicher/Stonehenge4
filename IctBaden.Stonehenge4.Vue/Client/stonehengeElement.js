Vue.component('stonehengeCustomElementName',
    {
        props: [stonehengeCustomElementProps],
        template: 'stonehengeElementTemplate',
        data: function() {
            return { I18n: this.$parent.I18n }
        }
        //stonehengeElementMethods
    });

