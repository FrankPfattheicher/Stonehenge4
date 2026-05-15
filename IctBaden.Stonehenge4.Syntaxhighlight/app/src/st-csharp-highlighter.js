/* global StCsharpHighlighter */
var StCsharpHighlighter = (function () {
    'use strict';

    const KEYWORDS = new Set([
        'abstract', 'as', 'base', 'break', 'case', 'catch', 'checked', 'class', 'const', 'continue',
        'default', 'delegate', 'do', 'else', 'enum', 'event', 'explicit', 'extern', 'false', 'finally',
        'fixed', 'for', 'foreach', 'goto', 'if', 'implicit', 'in', 'interface', 'internal', 'is',
        'lock', 'namespace', 'new', 'null', 'operator', 'out', 'override', 'params', 'private',
        'protected', 'public', 'readonly', 'ref', 'return', 'sealed', 'sizeof', 'stackalloc', 'static',
        'struct', 'switch', 'this', 'throw', 'true', 'try', 'typeof', 'unchecked', 'unsafe', 'using',
        'virtual', 'void', 'volatile', 'while', 'add', 'alias', 'ascending', 'async', 'await',
        'by', 'descending', 'dynamic', 'equals', 'file', 'from', 'get', 'global', 'group', 'init',
        'into', 'join', 'let', 'managed', 'nameof', 'notnull', 'on', 'orderby', 'partial', 'record',
        'remove', 'required', 'scoped', 'select', 'set', 'unmanaged', 'value', 'var', 'when', 'where',
        'yield', 'and', 'or', 'not'
    ]);

    const BUILT_IN_TYPES = new Set([
        'bool', 'byte', 'char', 'decimal', 'double', 'float', 'int', 'long', 'nint', 'nuint', 'object',
        'sbyte', 'short', 'string', 'uint', 'ulong', 'ushort', 'void'
    ]);

    const LITERALS = new Set(['true', 'false', 'null']);

    const SCRIPT_DIRECTIVES = new Set(['r', 'load', 'loadpaths', 'help', 'i', 'line', 'nullable']);

    function escapeHtml(text) {
        return text
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;');
    }

    function span(className, text) {
        return '<span class="' + className + '">' + escapeHtml(text) + '</span>';
    }

    function isIdentStart(ch) {
        return (ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z') || ch === '_' || ch === '@';
    }

    function isIdentPart(ch) {
        return isIdentStart(ch) || (ch >= '0' && ch <= '9');
    }

    function readIdentifier(source, start) {
        let i = start;
        if (source[i] === '@') {
            i++;
        }
        while (i < source.length && isIdentPart(source[i])) {
            i++;
        }
        return source.slice(start, i);
    }

    function classifyIdentifier(text, nextNonWs) {
        const bare = text[0] === '@' ? text.slice(1) : text;
        const lower = bare.toLowerCase();
        if (LITERALS.has(lower)) {
            return 'st-sh-literal';
        }
        if (KEYWORDS.has(lower)) {
            return 'st-sh-keyword';
        }
        if (BUILT_IN_TYPES.has(lower)) {
            return 'st-sh-type';
        }
        if (nextNonWs === '(') {
            return 'st-sh-method';
        }
        if (bare.length > 0 && bare[0] >= 'A' && bare[0] <= 'Z') {
            return 'st-sh-type';
        }
        return 'st-sh-identifier';
    }

    function readNumber(source, start) {
        let i = start;
        const len = source.length;

        if (source[i] === '0' && i + 1 < len) {
            const marker = source[i + 1];
            if (marker === 'x' || marker === 'X') {
                i += 2;
                while (i < len && /[0-9a-fA-F_]/.test(source[i])) {
                    i++;
                }
            } else if (marker === 'b' || marker === 'B') {
                i += 2;
                while (i < len && /[01_]/.test(source[i])) {
                    i++;
                }
            }
        } else {
            while (i < len && /[0-9_]/.test(source[i])) {
                i++;
            }
        }

        if (i < len && source[i] === '.' && i + 1 < len && /[0-9]/.test(source[i + 1])) {
            i++;
            while (i < len && /[0-9_]/.test(source[i])) {
                i++;
            }
        }

        if (i < len && (source[i] === 'e' || source[i] === 'E')) {
            i++;
            if (i < len && (source[i] === '+' || source[i] === '-')) {
                i++;
            }
            while (i < len && /[0-9_]/.test(source[i])) {
                i++;
            }
        }

        while (i < len && /[dfmFlLuU]/.test(source[i])) {
            i++;
        }

        return i;
    }

    function readString(source, start) {
        let i = start;
        const len = source.length;
        let quote = '"';
        let interpolated = false;

        if (source[i] === '$') {
            interpolated = true;
            i++;
        }

        if (i < len && source[i] === '@') {
            i++;
            quote = '"';
            if (i >= len || source[i] !== quote) {
                return start + 1;
            }
            i++;
            while (i < len) {
                if (source[i] === quote) {
                    if (i + 1 < len && source[i + 1] === quote) {
                        i += 2;
                        continue;
                    }
                    return i + 1;
                }
                i++;
            }
            return len;
        }

        if (i >= len || source[i] !== quote) {
            return interpolated ? i : start + 1;
        }

        i++;
        while (i < len) {
            const ch = source[i];
            if (ch === '\\') {
                i += 2;
                continue;
            }
            if (interpolated && ch === '{') {
                let depth = 1;
                i++;
                while (i < len && depth > 0) {
                    if (source[i] === '{') {
                        depth++;
                    } else if (source[i] === '}') {
                        depth--;
                    }
                    i++;
                }
                continue;
            }
            if (ch === quote) {
                return i + 1;
            }
            i++;
        }
        return len;
    }

    function readRawString(source, start) {
        let i = start;
        let quoteCount = 0;
        while (i < source.length && source[i] === '"') {
            quoteCount++;
            i++;
        }
        if (quoteCount < 3) {
            return start + 1;
        }

        while (i < source.length) {
            if (source[i] === '"') {
                let end = i;
                let closing = 0;
                while (end < source.length && source[end] === '"') {
                    closing++;
                    end++;
                }
                if (closing >= quoteCount) {
                    return end;
                }
            }
            i++;
        }
        return source.length;
    }

    function readCharLiteral(source, start) {
        let i = start + 1;
        const len = source.length;
        while (i < len) {
            if (source[i] === '\\') {
                i += 2;
                continue;
            }
            if (source[i] === '\'') {
                return i + 1;
            }
            i++;
        }
        return len;
    }

    function readLineComment(source, start) {
        let i = start + 2;
        while (i < source.length && source[i] !== '\n' && source[i] !== '\r') {
            i++;
        }
        return i;
    }

    function readBlockComment(source, start) {
        let i = start + 2;
        while (i + 1 < source.length) {
            if (source[i] === '*' && source[i + 1] === '/') {
                return i + 2;
            }
            i++;
        }
        return source.length;
    }

    function readPreprocessor(source, start) {
        let i = start + 1;
        while (i < source.length && source[i] !== '\n' && source[i] !== '\r') {
            i++;
        }
        return i;
    }

    function peekNextNonWhitespace(source, index) {
        let i = index;
        while (i < source.length && source[i] <= ' ') {
            i++;
        }
        return i < source.length ? source[i] : '';
    }

    function highlight(source) {
        if (!source) {
            return '';
        }

        let i = 0;
        let result = '';
        let lineStart = true;

        while (i < source.length) {
            const ch = source[i];

            if (ch === '\n' || ch === '\r') {
                result += escapeHtml(ch);
                i++;
                lineStart = true;
                if (ch === '\r' && i < source.length && source[i] === '\n') {
                    result += escapeHtml('\n');
                    i++;
                }
                continue;
            }

            if (ch <= ' ') {
                let j = i + 1;
                while (j < source.length && source[j] <= ' ' && source[j] !== '\n' && source[j] !== '\r') {
                    j++;
                }
                result += escapeHtml(source.slice(i, j));
                i = j;
                continue;
            }

            if (lineStart && ch === '#') {
                const end = readPreprocessor(source, i);
                const directive = source.slice(i, end);
                const match = /^#\s*([a-zA-Z_][\w]*)/.exec(directive);
                const name = match ? match[1].toLowerCase() : '';
                const cls = SCRIPT_DIRECTIVES.has(name) ? 'st-sh-script-directive' : 'st-sh-preprocessor';
                result += span(cls, directive);
                i = end;
                lineStart = false;
                continue;
            }

            lineStart = false;

            if (ch === '/' && source[i + 1] === '/') {
                const end = readLineComment(source, i);
                result += span('st-sh-comment', source.slice(i, end));
                i = end;
                continue;
            }

            if (ch === '/' && source[i + 1] === '*') {
                const end = readBlockComment(source, i);
                result += span('st-sh-comment', source.slice(i, end));
                i = end;
                continue;
            }

            if (ch === '"' && i + 2 < source.length && source[i + 1] === '"' && source[i + 2] === '"') {
                const end = readRawString(source, i);
                result += span('st-sh-string', source.slice(i, end));
                i = end;
                continue;
            }

            if (ch === '"' || ch === '$' || (ch === '@' && source[i + 1] === '"')) {
                const end = readString(source, i);
                result += span('st-sh-string', source.slice(i, end));
                i = end;
                continue;
            }

            if (ch === '\'') {
                const end = readCharLiteral(source, i);
                result += span('st-sh-string', source.slice(i, end));
                i = end;
                continue;
            }

            if (ch === '[') {
                let j = i + 1;
                while (j < source.length && source[j] <= ' ') {
                    j++;
                }
                if (j < source.length && (isIdentStart(source[j]) || source[j] === ']')) {
                    const close = source.indexOf(']', j);
                    if (close !== -1) {
                        result += span('st-sh-attribute', source.slice(i, close + 1));
                        i = close + 1;
                        continue;
                    }
                }
            }

            if ((ch >= '0' && ch <= '9') || (ch === '.' && i + 1 < source.length && source[i + 1] >= '0' && source[i + 1] <= '9')) {
                const end = readNumber(source, i);
                result += span('st-sh-number', source.slice(i, end));
                i = end;
                continue;
            }

            if (isIdentStart(ch)) {
                const text = readIdentifier(source, i);
                const next = peekNextNonWhitespace(source, i + text.length);
                result += span(classifyIdentifier(text, next), text);
                i += text.length;
                continue;
            }

            if ('+-*/%=<>!&|^~?:.,;(){}[]'.includes(ch)) {
                result += span('st-sh-operator', ch);
                i++;
                continue;
            }

            result += escapeHtml(ch);
            i++;
        }

        return result;
    }

    function highlightPlain(source) {
        return escapeHtml(source ?? '');
    }

    function isCSharpLanguage(language) {
        const lang = (language ?? 'csharp').trim().toLowerCase();
        return lang === '' || lang === 'csharp' || lang === 'cs' || lang === 'c#';
    }

    const INDENT = '    ';
    const INDENT_SIZE = 4;

    function getLineBounds(text, pos) {
        const lineStart = text.lastIndexOf('\n', pos - 1) + 1;
        const nextBreak = text.indexOf('\n', pos);
        const lineEnd = nextBreak === -1 ? text.length : nextBreak;
        return { lineStart: lineStart, lineEnd: lineEnd };
    }

    function getLeadingWhitespace(line) {
        const match = /^[\t ]*/.exec(line);
        return match ? match[0] : '';
    }

    function indentLevel(whitespace) {
        let width = 0;
        for (let i = 0; i < whitespace.length; i++) {
            width += whitespace.charAt(i) === '\t' ? INDENT_SIZE : 1;
        }
        return Math.floor(width / INDENT_SIZE);
    }

    function makeIndent(level) {
        if (level <= 0) {
            return '';
        }
        let result = '';
        for (let i = 0; i < level; i++) {
            result += INDENT;
        }
        return result;
    }

    function findOpenBraceIndent(text, pos) {
        let depth = 0;
        for (let i = pos - 1; i >= 0; i--) {
            const ch = text.charAt(i);
            if (ch === '}') {
                depth++;
            } else if (ch === '{') {
                if (depth === 0) {
                    const lineStart = text.lastIndexOf('\n', i) + 1;
                    const lineEnd = text.indexOf('\n', i);
                    const line = text.substring(lineStart, lineEnd === -1 ? text.length : lineEnd);
                    return getLeadingWhitespace(line);
                }
                depth--;
            }
        }
        return '';
    }

    function insertText(textarea, start, end, insert, cursorPos) {
        const value = textarea.value;
        textarea.value = value.substring(0, start) + insert + value.substring(end);
        textarea.selectionStart = textarea.selectionEnd = cursorPos;
    }

    function indentLines(text, start, end, outdent) {
        const firstLine = text.lastIndexOf('\n', start - 1) + 1;
        const lastPos = Math.max(start, end - 1);
        const lastBreak = text.indexOf('\n', lastPos);
        const blockEnd = lastBreak === -1 ? text.length : lastBreak;
        const block = text.substring(firstLine, blockEnd);
        const lines = block.split('\n');
        let removedAtStart = 0;

        const changed = lines.map(function (line, index) {
            if (outdent) {
                if (line.startsWith(INDENT)) {
                    if (index === 0) {
                        removedAtStart = INDENT_SIZE;
                    }
                    return line.substring(INDENT_SIZE);
                }
                if (line.startsWith('\t')) {
                    if (index === 0) {
                        removedAtStart = 1;
                    }
                    return line.substring(1);
                }
                const partial = line.match(/^ {1,4}/);
                if (partial) {
                    if (index === 0) {
                        removedAtStart = partial[0].length;
                    }
                    return line.substring(partial[0].length);
                }
                return line;
            }
            if (index === 0) {
                removedAtStart = -INDENT_SIZE;
            }
            return INDENT + line;
        });

        const replacement = changed.join('\n');
        const newText = text.substring(0, firstLine) + replacement + text.substring(blockEnd);
        const delta = replacement.length - block.length;
        return {
            text: newText,
            selectionStart: outdent ? Math.max(firstLine, start - removedAtStart) : start + INDENT_SIZE,
            selectionEnd: end + delta
        };
    }

    function handleEditorKeydown(textarea, event) {
        const key = event.key;
        const value = textarea.value;
        const start = textarea.selectionStart;
        const end = textarea.selectionEnd;
        const hasSelection = start !== end;

        if (key === 'Tab') {
            event.preventDefault();
            if (hasSelection) {
                const result = indentLines(value, start, end, event.shiftKey);
                textarea.value = result.text;
                textarea.selectionStart = event.shiftKey ? Math.max(start - INDENT_SIZE, 0) : start + INDENT_SIZE;
                textarea.selectionEnd = end + (result.text.length - value.length);
            } else if (event.shiftKey) {
                const bounds = getLineBounds(value, start);
                const line = value.substring(bounds.lineStart, bounds.lineEnd);
                const ws = getLeadingWhitespace(line);
                const remove = line.startsWith(INDENT) ? INDENT_SIZE : (line.startsWith('\t') ? 1 : Math.min(ws.length, INDENT_SIZE));
                if (remove > 0) {
                    insertText(textarea, bounds.lineStart, bounds.lineStart + remove, '', start - remove);
                }
            } else {
                insertText(textarea, start, end, INDENT, start + INDENT_SIZE);
            }
            return true;
        }

        if (key === 'Enter') {
            event.preventDefault();
            const bounds = getLineBounds(value, start);
            const line = value.substring(bounds.lineStart, bounds.lineEnd);
            const beforeCursor = value.substring(bounds.lineStart, start);
            const afterCursor = value.substring(start, bounds.lineEnd);
            const baseWs = getLeadingWhitespace(line);
            const baseLevel = indentLevel(baseWs);
            const trimBefore = beforeCursor.trimEnd();

            let insert = '\n';
            let cursorPos = start + 1;

            if (trimBefore.endsWith('{')) {
                const inner = makeIndent(baseLevel + 1);
                const close = makeIndent(baseLevel);
                const restAfter = value.substring(bounds.lineEnd).trimStart();
                const hasCloseOnLine = afterCursor.trimStart().startsWith('}');
                if (!hasCloseOnLine && !restAfter.startsWith('}')) {
                    insert = '\n' + inner + '\n' + close + '}';
                    cursorPos = start + 1 + inner.length;
                } else {
                    insert = '\n' + inner;
                    cursorPos = start + insert.length;
                }
            } else if (/^\s*}\s*$/.test(afterCursor) && trimBefore === '') {
                const openIndent = findOpenBraceIndent(value, start);
                const closeLevel = indentLevel(openIndent);
                const closeWs = makeIndent(Math.max(0, closeLevel));
                insert = '\n' + closeWs;
                cursorPos = start + insert.length;
            } else {
                insert = '\n' + baseWs;
                cursorPos = start + insert.length;
            }

            insertText(textarea, start, end, insert, cursorPos);
            return true;
        }

        if (key === '}' && !event.shiftKey && !hasSelection) {
            const bounds = getLineBounds(value, start);
            const beforeOnLine = value.substring(bounds.lineStart, start);
            if (/^\s*$/.test(beforeOnLine)) {
                event.preventDefault();
                const openIndent = findOpenBraceIndent(value, start);
                const closeWs = openIndent || makeIndent(Math.max(0, indentLevel(getLeadingWhitespace(value.substring(bounds.lineStart, bounds.lineEnd))) - 1));
                const afterOnLine = value.substring(start, bounds.lineEnd);
                const skipWs = afterOnLine.match(/^\s*/);
                const skip = skipWs ? skipWs[0].length : 0;
                const insert = closeWs + '}';
                insertText(textarea, bounds.lineStart, start + skip, insert, bounds.lineStart + insert.length);
                return true;
            }
        }

        return false;
    }

    function renderElement(element, source, language) {
        if (!element) {
            return;
        }

        if (typeof element.classList !== 'undefined') {
            element.classList.add('st-sh-csharp');
        }

        if (isCSharpLanguage(language)) {
            element.innerHTML = highlight(source ?? '');
        } else {
            element.innerHTML = highlightPlain(source ?? '');
        }
    }

    return {
        highlight: highlight,
        highlightPlain: highlightPlain,
        isCSharpLanguage: isCSharpLanguage,
        renderElement: renderElement,
        handleEditorKeydown: handleEditorKeydown
    };
})();
