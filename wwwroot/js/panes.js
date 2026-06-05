// Pane resize handles + floating window drag.
// Direct DOM manipulation; no SignalR round-trip during drag.

export function installResize(handleEl, workspaceEl, side) {
    if (!handleEl || handleEl.dataset.boundResize === '1') return;
    handleEl.dataset.boundResize = '1';

    handleEl.addEventListener('pointerdown', e => {
        if (e.button !== 0) return;
        e.preventDefault();
        e.stopPropagation();

        const prop = side === 'left' ? '--left-w' : '--right-w';
        const cs = getComputedStyle(workspaceEl);
        const startW = parseFloat(cs.getPropertyValue(prop)) || (side === 'left' ? 280 : 340);
        const startX = e.clientX;

        handleEl.setPointerCapture(e.pointerId);
        document.body.style.cursor = 'ew-resize';
        document.body.style.userSelect = 'none';

        const onMove = ev => {
            const dx = ev.clientX - startX;
            // Left resizer grows when dragged right; right resizer grows when dragged left.
            const newW = side === 'left' ? startW + dx : startW - dx;
            const clamped = Math.max(180, Math.min(700, newW));
            workspaceEl.style.setProperty(prop, clamped + 'px');
        };
        const onUp = ev => {
            try { handleEl.releasePointerCapture(ev.pointerId); } catch {}
            handleEl.removeEventListener('pointermove', onMove);
            handleEl.removeEventListener('pointerup', onUp);
            handleEl.removeEventListener('pointercancel', onUp);
            document.body.style.cursor = '';
            document.body.style.userSelect = '';
        };
        handleEl.addEventListener('pointermove', onMove);
        handleEl.addEventListener('pointerup', onUp);
        handleEl.addEventListener('pointercancel', onUp);
    });
}

export function installFloatingDrag(headerEl, paneEl) {
    if (!headerEl || !paneEl || headerEl.dataset.boundFloat === '1') return;
    headerEl.dataset.boundFloat = '1';

    headerEl.addEventListener('pointerdown', e => {
        if (e.button !== 0) return;
        if (e.target.closest('button, input, select')) return;
        e.preventDefault();

        const startX = e.clientX, startY = e.clientY;
        const rect = paneEl.getBoundingClientRect();
        const startLeft = rect.left, startTop = rect.top;

        headerEl.setPointerCapture(e.pointerId);
        document.body.style.userSelect = 'none';

        const onMove = ev => {
            const x = startLeft + (ev.clientX - startX);
            const y = startTop + (ev.clientY - startY);
            // Keep at least 60px on screen so user can grab the header back.
            const maxX = window.innerWidth - 60;
            const maxY = window.innerHeight - 30;
            paneEl.style.left = Math.max(-paneEl.offsetWidth + 60, Math.min(maxX, x)) + 'px';
            paneEl.style.top  = Math.max(0, Math.min(maxY, y)) + 'px';
        };
        const onUp = ev => {
            try { headerEl.releasePointerCapture(ev.pointerId); } catch {}
            headerEl.removeEventListener('pointermove', onMove);
            headerEl.removeEventListener('pointerup', onUp);
            headerEl.removeEventListener('pointercancel', onUp);
            document.body.style.userSelect = '';
        };
        headerEl.addEventListener('pointermove', onMove);
        headerEl.addEventListener('pointerup', onUp);
        headerEl.addEventListener('pointercancel', onUp);
    });
}

export function installFloatingResize(handleEl, paneEl) {
    if (!handleEl || !paneEl || handleEl.dataset.boundFloatResize === '1') return;
    handleEl.dataset.boundFloatResize = '1';

    handleEl.addEventListener('pointerdown', e => {
        if (e.button !== 0) return;
        e.preventDefault();
        e.stopPropagation();

        const startX = e.clientX, startY = e.clientY;
        const startW = paneEl.offsetWidth, startH = paneEl.offsetHeight;

        handleEl.setPointerCapture(e.pointerId);
        document.body.style.cursor = 'nwse-resize';
        document.body.style.userSelect = 'none';

        const onMove = ev => {
            const w = Math.max(220, startW + (ev.clientX - startX));
            const h = Math.max(160, startH + (ev.clientY - startY));
            paneEl.style.width  = w + 'px';
            paneEl.style.height = h + 'px';
        };
        const onUp = ev => {
            try { handleEl.releasePointerCapture(ev.pointerId); } catch {}
            handleEl.removeEventListener('pointermove', onMove);
            handleEl.removeEventListener('pointerup', onUp);
            handleEl.removeEventListener('pointercancel', onUp);
            document.body.style.cursor = '';
            document.body.style.userSelect = '';
        };
        handleEl.addEventListener('pointermove', onMove);
        handleEl.addEventListener('pointerup', onUp);
        handleEl.addEventListener('pointercancel', onUp);
    });
}
