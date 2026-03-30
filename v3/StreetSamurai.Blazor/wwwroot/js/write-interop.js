window.writeInterop = {
    getCursorPosition: function (id) {
        const el = document.getElementById(id);
        return el ? el.selectionStart : 0;
    },

    getSelection: function (id) {
        const el = document.getElementById(id);
        if (!el) return { start: 0, end: 0, text: "" };
        return {
            start: el.selectionStart,
            end: el.selectionEnd,
            text: el.value.substring(el.selectionStart, el.selectionEnd)
        };
    },

    insertText: function (id, position, text) {
        const el = document.getElementById(id);
        if (!el) return "";
        const before = el.value.substring(0, position);
        const after = el.value.substring(position);
        el.value = before + text + after;
        const newPos = position + text.length;
        el.selectionStart = newPos;
        el.selectionEnd = newPos;
        el.dispatchEvent(new Event("input", { bubbles: true }));
        el.dispatchEvent(new Event("change", { bubbles: true }));
        return el.value;
    },

    replaceRange: function (id, start, end, text) {
        const el = document.getElementById(id);
        if (!el) return "";
        const before = el.value.substring(0, start);
        const after = el.value.substring(end);
        el.value = before + text + after;
        el.selectionStart = start;
        el.selectionEnd = start + text.length;
        el.dispatchEvent(new Event("input", { bubbles: true }));
        el.dispatchEvent(new Event("change", { bubbles: true }));
        return el.value;
    },

    setText: function (id, text) {
        const el = document.getElementById(id);
        if (!el) return;
        el.value = text;
        el.dispatchEvent(new Event("input", { bubbles: true }));
    },

    focusEnd: function (id) {
        const el = document.getElementById(id);
        if (!el) return;
        el.focus();
        el.selectionStart = el.value.length;
        el.selectionEnd = el.value.length;
    },

    focusAt: function (id, position) {
        const el = document.getElementById(id);
        if (!el) return;
        el.focus();
        el.selectionStart = position;
        el.selectionEnd = position;
    },

    // Right-click context menu — fires Blazor callback with position when text is selected
    _contextRef: null,
    _contextHandler: null,
    _dismissHandler: null,

    watchRightClick: function (id, dotNetRef) {
        const el = document.getElementById(id);
        if (!el) return;

        if (this._contextHandler) {
            el.removeEventListener("contextmenu", this._contextHandler);
        }
        if (this._dismissHandler) {
            document.removeEventListener("mousedown", this._dismissHandler);
        }

        this._contextRef = dotNetRef;

        this._contextHandler = function (e) {
            const hasSelection = el.selectionStart !== el.selectionEnd;
            if (!hasSelection) return; // no selection = use default browser menu

            e.preventDefault();
            dotNetRef.invokeMethodAsync("OnContextMenu", e.clientY, e.clientX);
        };

        this._dismissHandler = function (e) {
            // Dismiss context menu when clicking outside it
            if (!e.target.closest(".story-context-menu")) {
                dotNetRef.invokeMethodAsync("OnDismissContextMenu");
            }
        };

        el.addEventListener("contextmenu", this._contextHandler);
        document.addEventListener("mousedown", this._dismissHandler);
    },

    unwatchRightClick: function (id) {
        const el = document.getElementById(id);
        if (el && this._contextHandler) {
            el.removeEventListener("contextmenu", this._contextHandler);
        }
        if (this._dismissHandler) {
            document.removeEventListener("mousedown", this._dismissHandler);
        }
        this._contextHandler = null;
        this._dismissHandler = null;
        this._contextRef = null;
    }
};
