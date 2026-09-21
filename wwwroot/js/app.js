// Pequenos utilitários que o Blazor não consegue fazer sozinho: mexer no cursor de um
// campo de texto, rolar uma lista e pedir confirmação nativa.
window.lingua = {
    insertAtCursor(id, text) {
        const el = document.getElementById(id);
        if (!el) return;

        const start = el.selectionStart ?? el.value.length;
        const end = el.selectionEnd ?? start;

        el.value = el.value.slice(0, start) + text + el.value.slice(end);
        el.selectionStart = el.selectionEnd = start + text.length;

        // Os dois eventos porque o Blazor escuta "input" em uns campos e "change" em outros.
        el.dispatchEvent(new Event('input', { bubbles: true }));
        el.dispatchEvent(new Event('change', { bubbles: true }));
        el.focus();
    },

    scrollToBottom(id) {
        const el = document.getElementById(id);
        if (el) el.scrollTop = el.scrollHeight;
    },

    confirm(message) {
        return window.confirm(message);
    }
};
