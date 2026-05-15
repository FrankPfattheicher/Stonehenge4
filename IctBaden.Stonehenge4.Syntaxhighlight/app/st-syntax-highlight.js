
mounted: function () {
    var self = this;
    self._highlight = self.$el.querySelector('.st-sh-highlight');
    self._input = self.$el.querySelector('.st-sh-input');
    self._internalChange = false;

    self._getEditorModel = function () {
        if (self.model && typeof self.model === 'object') {
            return self.model;
        }
        if (self.$parent && self.$parent.ScEdit && typeof self.$parent.ScEdit === 'object') {
            return self.$parent.ScEdit;
        }
        return null;
    };

    self._readSource = function () {
        var editorModel = self._getEditorModel();
        if (editorModel && editorModel.Source !== undefined && editorModel.Source !== null) {
            return editorModel.Source;
        }
        return '';
    };

    self._writeSource = function (value) {
        var editorModel = self._getEditorModel();
        if (!editorModel) {
            return;
        }
        editorModel.Source = value;
    };

    self._refreshHighlight = function () {
        if (!self._highlight || !self._input) {
            return;
        }
        var text = self._input.value ?? '';
        if (typeof StCsharpHighlighter === 'undefined') {
            self._highlight.textContent = text;
            return;
        }
        StCsharpHighlighter.renderElement(self._highlight, text, 'csharp');
    };

    self._syncFromModel = function () {
        if (self._internalChange || !self._input) {
            return;
        }
        var text = self._readSource();
        if (self._input.value !== text) {
            self._input.value = text;
        }
        self._refreshHighlight();
        self._syncScroll();
    };

    self._syncScroll = function () {
        if (!self._highlight || !self._input) {
            return;
        }
        self._highlight.scrollTop = self._input.scrollTop;
        self._highlight.scrollLeft = self._input.scrollLeft;
    };

    self._onInput = function () {
        self._internalChange = true;
        self._writeSource(self._input.value);
        self._refreshHighlight();
        self._syncScroll();
        self.$emit('input', self._input.value);
        self.$nextTick(function () {
            self._internalChange = false;
        });
    };

    self._onScroll = function () {
        self._syncScroll();
    };

    self._input.addEventListener('input', self._onInput);
    self._input.addEventListener('scroll', self._onScroll);
    self._input.addEventListener('keydown', function (e) {
        if (typeof StCsharpHighlighter !== 'undefined' &&
            StCsharpHighlighter.handleEditorKeydown(self._input, e)) {
            self._onInput();
        }
    });

    self.$watch('model', function () {
        self._syncFromModel();
    }, { deep: true });

    self.$watch(
        function () {
            return self.$parent && self.$parent.ScEdit;
        },
        function () {
            self._syncFromModel();
        },
        { deep: true }
    );

    self.$nextTick(function () {
        self._syncFromModel();
    });
},

updated: function () {
    if (typeof this._syncFromModel === 'function') {
        this._syncFromModel();
    }
}
