window.ReportEditor = {
    clickElement(id) {
        const el = document.getElementById(id);
        if (!el) return;
        if (el.tagName === 'INPUT' && el.type === 'file') el.value = '';
        el.click();
    },

    /** Fresh file picker each time. Reads as text and calls back to .NET.
        Avoids the Blazor InputFile caching/round-trip slowness on large RDLs. */
    openRdl(dotnetRef, callback) {
        const input = document.createElement('input');
        input.type = 'file';
        input.accept = '.rdl,application/xml';
        input.style.display = 'none';
        document.body.appendChild(input);
        input.onchange = async () => {
            try {
                const file = input.files?.[0];
                if (file) {
                    const text = await file.text();
                    await dotnetRef.invokeMethodAsync(callback, file.name, text);
                }
            } finally {
                input.remove();
            }
        };
        input.click();
    },
    downloadText(filename, text, mime) {
        const blob = new Blob([text], { type: mime || 'text/plain' });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url; a.download = filename;
        document.body.appendChild(a); a.click(); a.remove();
        URL.revokeObjectURL(url);
    },
    focusEditable(el) {
        if (!el) return;
        el.focus();
        const range = document.createRange();
        range.selectNodeContents(el);
        const sel = window.getSelection();
        sel.removeAllRanges();
        sel.addRange(range);
    },
    focusElement(el) { if (el) el.focus(); },
    focusEditableById(id) {
        const el = document.getElementById(id);
        if (!el) return;
        el.focus();
        const range = document.createRange();
        range.selectNodeContents(el);
        const sel = window.getSelection();
        sel.removeAllRanges();
        sel.addRange(range);
    },
    getElementTextById(id) {
        const el = document.getElementById(id);
        return el ? (el.innerText ?? '') : '';
    },
    saveDraft(key, value) {
        try { localStorage.setItem(key, value); } catch {}
    },
    loadDraft(key) {
        try { return localStorage.getItem(key) || ''; } catch { return ''; }
    },
    removeDraft(key) {
        try { localStorage.removeItem(key); } catch {}
    },
    listDrafts(prefix) {
        try {
            const out = [];
            for (let i = 0; i < localStorage.length; i++) {
                const k = localStorage.key(i);
                if (k && k.startsWith(prefix)) out.push(k);
            }
            return out;
        } catch { return []; }
    },
    printPage() { window.print(); },
    getElementText(el) {
        return el ? (el.innerText ?? '') : '';
    },

    installShortcuts(dotnetRef) {
        if (window.__reportEditorShortcuts) {
            document.removeEventListener('keydown', window.__reportEditorShortcuts);
        }
        const handler = (e) => {
            const t = e.target;
            const tag = (t?.tagName || '').toUpperCase();
            const editable = tag === 'INPUT' || tag === 'TEXTAREA' || tag === 'SELECT' ||
                             (t?.isContentEditable === true);

            const key = e.key;
            const ctrl = e.ctrlKey || e.metaKey;
            const shift = e.shiftKey;

            if (ctrl && !shift && key.toLowerCase() === 's') { e.preventDefault(); dotnetRef.invokeMethodAsync('OnShortcut', 'save'); return; }
            if (ctrl && !shift && key.toLowerCase() === 'z') { e.preventDefault(); dotnetRef.invokeMethodAsync('OnShortcut', 'undo'); return; }
            if (ctrl && (key.toLowerCase() === 'y' || (shift && key.toLowerCase() === 'z'))) {
                e.preventDefault(); dotnetRef.invokeMethodAsync('OnShortcut', 'redo'); return;
            }
            if (ctrl && !shift && key.toLowerCase() === 'd') { e.preventDefault(); dotnetRef.invokeMethodAsync('OnShortcut', 'duplicate'); return; }
            if (ctrl && !shift && key.toLowerCase() === 'f') { e.preventDefault(); dotnetRef.invokeMethodAsync('OnShortcut', 'find'); return; }
            if (ctrl && shift && key.toLowerCase() === 'f') { e.preventDefault(); dotnetRef.invokeMethodAsync('OnShortcut', 'replace'); return; }
            if (ctrl && !shift && key.toLowerCase() === 'p') { e.preventDefault(); dotnetRef.invokeMethodAsync('OnShortcut', 'print'); return; }

            if (editable) return;

            // Outside editable: cut/copy/paste apply to selected report items.
            if (ctrl && key.toLowerCase() === 'x') { e.preventDefault(); dotnetRef.invokeMethodAsync('OnShortcut', 'cut'); return; }
            if (ctrl && key.toLowerCase() === 'c') { e.preventDefault(); dotnetRef.invokeMethodAsync('OnShortcut', 'copy'); return; }
            if (ctrl && key.toLowerCase() === 'v') { e.preventDefault(); dotnetRef.invokeMethodAsync('OnShortcut', 'paste'); return; }

            if (key === 'Delete' || key === 'Backspace') {
                e.preventDefault();
                dotnetRef.invokeMethodAsync('OnShortcut', 'delete');
            } else if (key === 'Escape') {
                dotnetRef.invokeMethodAsync('OnShortcut', 'escape');
            } else if (key.startsWith('Arrow')) {
                e.preventDefault();
                const dir = key.substring(5).toLowerCase();
                dotnetRef.invokeMethodAsync('OnNudge', dir, shift);
            }
        };
        document.addEventListener('keydown', handler);
        window.__reportEditorShortcuts = handler;

        // Close context menus on any background click.
        if (window.__reportEditorCloseCtx) {
            document.removeEventListener('mousedown', window.__reportEditorCloseCtx);
        }
        const closer = (e) => {
            if (!e.target.closest('.ctx-menu')) {
                dotnetRef.invokeMethodAsync('OnShortcut', 'close-ctx');
            }
        };
        document.addEventListener('mousedown', closer);
        window.__reportEditorCloseCtx = closer;
    },

    installDragDrop(dotnetRef) {
        if (window.__reportEditorDnd) return;
        window.__reportEditorDnd = true;

        const overlay = document.createElement('div');
        overlay.id = 'rdl-drop-overlay';
        overlay.style.cssText = 'display:none;position:fixed;inset:0;background:rgba(0,122,204,.2);border:3px dashed #007acc;z-index:2000000;pointer-events:none;color:#fff;font-size:1.5rem;align-items:center;justify-content:center;';
        overlay.innerHTML = '<div style="background:#007acc;padding:20px 40px;border-radius:8px;">Drop .rdl to open</div>';
        document.body.appendChild(overlay);

        let dragDepth = 0;
        window.addEventListener('dragenter', e => {
            if (!Array.from(e.dataTransfer?.items || []).some(i => i.kind === 'file')) return;
            dragDepth++;
            overlay.style.display = 'flex';
        });
        window.addEventListener('dragover', e => {
            if (e.dataTransfer?.types?.includes('Files')) {
                e.preventDefault();
            }
        });
        window.addEventListener('dragleave', () => {
            dragDepth = Math.max(0, dragDepth - 1);
            if (dragDepth === 0) overlay.style.display = 'none';
        });
        window.addEventListener('drop', async e => {
            dragDepth = 0;
            overlay.style.display = 'none';
            const file = e.dataTransfer?.files?.[0];
            if (!file) return;
            if (!file.name.toLowerCase().endsWith('.rdl')) return;
            e.preventDefault();
            const text = await file.text();
            await dotnetRef.invokeMethodAsync('LoadRdlText', file.name, text);
        });
    }
};
