// Editor interop for textarea selection and keyboard shortcuts
window.editorInterop = {
    getSelection: function (textareaId) {
        const ta = document.getElementById(textareaId);
        if (!ta) return { start: 0, end: 0, text: '' };
        return {
            start: ta.selectionStart,
            end: ta.selectionEnd,
            text: ta.value.substring(ta.selectionStart, ta.selectionEnd)
        };
    },

    setSelection: function (textareaId, start, end) {
        const ta = document.getElementById(textareaId);
        if (!ta) return;
        ta.setSelectionRange(start, end);
        ta.focus();
    },

    insertAtCursor: function (textareaId, text) {
        const ta = document.getElementById(textareaId);
        if (!ta) return '';
        const start = ta.selectionStart;
        const end = ta.selectionEnd;
        const before = ta.value.substring(0, start);
        const after = ta.value.substring(end);
        ta.value = before + text + after;
        ta.selectionStart = ta.selectionEnd = start + text.length;
        ta.focus();
        // Trigger input event for Blazor binding
        ta.dispatchEvent(new Event('input', { bubbles: true }));
        return ta.value;
    },

    replaceSelection: function (textareaId, text) {
        return this.insertAtCursor(textareaId, text);
    },

    scrollSync: function (sourceId, targetId) {
        const source = document.getElementById(sourceId);
        const target = document.getElementById(targetId);
        if (!source || !target) return;
        const ratio = source.scrollTop / (source.scrollHeight - source.clientHeight || 1);
        target.scrollTop = ratio * (target.scrollHeight - target.clientHeight);
    },

    focusElement: function (elementId) {
        const el = document.getElementById(elementId);
        if (el) el.focus();
    },

    initKeyboardShortcuts: function (dotNetRef) {
        document.addEventListener('keydown', function (e) {
            // Ctrl+S / Cmd+S: Save
            if ((e.ctrlKey || e.metaKey) && e.key === 's') {
                e.preventDefault();
                dotNetRef.invokeMethodAsync('SaveFromJs');
            }
        });
    },

    playAudio: function (audioId) {
        const el = document.getElementById(audioId);
        if (el) el.play();
    },

    pauseAudio: function (audioId) {
        const el = document.getElementById(audioId);
        if (el) el.pause();
    }
};
