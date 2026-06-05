// Pointer-event drag/resize with multi-select + smart alignment guides.
// Visual preview lives entirely in JS; .NET only sees the final delta on
// pointerup (so SignalR isn't hammered during motion).

const state = new WeakMap(); // overlayEl -> { zoom }

export function attach(overlayEl, dotnetRef, zoom) {
    state.set(overlayEl, { zoom });
    overlayEl.querySelectorAll('.handle').forEach(h => {
        if (h.dataset.bound === '1') return;
        h.dataset.bound = '1';
        h.addEventListener('pointerdown', e => beginDrag(e, h, overlayEl, dotnetRef));
    });
}

export function setZoom(overlayEl, zoom) {
    const s = state.get(overlayEl);
    if (s) s.zoom = zoom;
}

const GUIDE_THRESHOLD = 4; // px in canvas-space to trigger a guide snap

function beginDrag(e, handle, overlayEl, dotnetRef) {
    if (e.button !== 0) return;
    e.preventDefault();
    e.stopPropagation();

    const kind = handle.dataset.kind;
    const s = state.get(overlayEl) || { zoom: 1 };
    const zoom = s.zoom || 1;

    const canvas = overlayEl.parentElement;
    const items = canvas ? Array.from(canvas.querySelectorAll('.item.selected')) : [];
    const others = canvas ? Array.from(canvas.querySelectorAll('.item:not(.selected)')) : [];

    const guidesEnabled = canvas?.parentElement?.parentElement
        ?.querySelector('input[type="checkbox"][name="guides"]')?.checked ?? true;

    const starts = items.map(it => ({
        el: it,
        l: parseFloat(it.style.left)   || 0,
        t: parseFloat(it.style.top)    || 0,
        w: parseFloat(it.style.width)  || 0,
        h: parseFloat(it.style.height) || 0,
    }));
    const overlayStart = {
        l: parseFloat(overlayEl.style.left)   || 0,
        t: parseFloat(overlayEl.style.top)    || 0,
        w: parseFloat(overlayEl.style.width)  || 0,
        h: parseFloat(overlayEl.style.height) || 0,
    };
    const outlines = canvas ? Array.from(canvas.querySelectorAll('.sel-outline')) : [];

    // Pre-compute snap targets from other items.
    const targets = others.map(o => {
        const l = parseFloat(o.style.left) || 0;
        const t = parseFloat(o.style.top)  || 0;
        const w = parseFloat(o.style.width)  || 0;
        const h = parseFloat(o.style.height) || 0;
        return { l, t, r: l + w, b: t + h, cx: l + w/2, cy: t + h/2 };
    });

    const startX = e.clientX, startY = e.clientY;
    handle.setPointerCapture(e.pointerId);

    // Lazy guide layer.
    let guideLayer = canvas?.querySelector('.guide-layer');
    if (!guideLayer && canvas) {
        guideLayer = document.createElement('div');
        guideLayer.className = 'guide-layer';
        guideLayer.style.cssText = 'position:absolute;inset:0;pointer-events:none;z-index:100002;';
        canvas.appendChild(guideLayer);
    }
    const clearGuides = () => { if (guideLayer) guideLayer.innerHTML = ''; };

    const drawGuide = (orient, pos) => {
        if (!guideLayer) return;
        const el = document.createElement('div');
        if (orient === 'v') {
            el.style.cssText = `position:absolute;left:${pos}px;top:0;width:1px;height:100%;background:#ff00aa;`;
        } else {
            el.style.cssText = `position:absolute;top:${pos}px;left:0;height:1px;width:100%;background:#ff00aa;`;
        }
        guideLayer.appendChild(el);
    };

    const snapAxis = (val, candidates) => {
        let best = val, bestDiff = GUIDE_THRESHOLD;
        for (const c of candidates) {
            const d = Math.abs(val - c);
            if (d <= bestDiff) { best = c; bestDiff = d; }
        }
        return { val: best, snapped: best !== val };
    };

    const onMove = ev => {
        clearGuides();
        const dxRaw = (ev.clientX - startX) / zoom;
        const dyRaw = (ev.clientY - startY) / zoom;

        if (kind === 'move' && starts.length === 1 && guidesEnabled) {
            // Apply guides only to the primary in single-select drag.
            const s0 = starts[0];
            let nl = s0.l + dxRaw, nt = s0.t + dyRaw;

            // Candidate snap positions: left/right/center of any other.
            const xCandidates = [];
            const yCandidates = [];
            for (const t of targets) {
                xCandidates.push(t.l, t.r, t.cx);
                yCandidates.push(t.t, t.b, t.cy);
            }

            // Snap left edge / right edge / center.
            const tryX = [
                { kind: 'l',  val: nl, set: v => nl = v },
                { kind: 'r',  val: nl + s0.w, set: v => nl = v - s0.w },
                { kind: 'cx', val: nl + s0.w/2, set: v => nl = v - s0.w/2 },
            ];
            for (const t of tryX) {
                const r = snapAxis(t.val, xCandidates);
                if (r.snapped) { t.set(r.val); drawGuide('v', r.val); break; }
            }
            const tryY = [
                { kind: 't',  val: nt, set: v => nt = v },
                { kind: 'b',  val: nt + s0.h, set: v => nt = v - s0.h },
                { kind: 'cy', val: nt + s0.h/2, set: v => nt = v - s0.h/2 },
            ];
            for (const t of tryY) {
                const r = snapAxis(t.val, yCandidates);
                if (r.snapped) { t.set(r.val); drawGuide('h', r.val); break; }
            }

            const dx = nl - s0.l;
            const dy = nt - s0.t;
            for (let i = 0; i < starts.length; i++) {
                const s = starts[i];
                s.el.style.left = (s.l + dx) + 'px';
                s.el.style.top  = (s.t + dy) + 'px';
            }
            for (let i = 0; i < outlines.length && i < starts.length; i++) {
                outlines[i].style.left = (starts[i].l + dx) + 'px';
                outlines[i].style.top  = (starts[i].t + dy) + 'px';
            }
            overlayEl.style.left = (overlayStart.l + dx) + 'px';
            overlayEl.style.top  = (overlayStart.t + dy) + 'px';

            // Stash the snapped delta for commit.
            handle._lastDx = dx;
            handle._lastDy = dy;
            return;
        }

        const dx = dxRaw, dy = dyRaw;
        if (kind === 'move') {
            for (let i = 0; i < starts.length; i++) {
                const s = starts[i];
                s.el.style.left = (s.l + dx) + 'px';
                s.el.style.top  = (s.t + dy) + 'px';
            }
            for (let i = 0; i < outlines.length && i < starts.length; i++) {
                outlines[i].style.left = (starts[i].l + dx) + 'px';
                outlines[i].style.top  = (starts[i].t + dy) + 'px';
            }
            overlayEl.style.left = (overlayStart.l + dx) + 'px';
            overlayEl.style.top  = (overlayStart.t + dy) + 'px';
            handle._lastDx = dx; handle._lastDy = dy;
        } else {
            const primary = starts[starts.length - 1];
            const applied = applyResize(overlayStart.l, overlayStart.t,
                overlayStart.w, overlayStart.h, kind, dx, dy);
            overlayEl.style.left = applied.l + 'px';
            overlayEl.style.top  = applied.t + 'px';
            overlayEl.style.width  = applied.w + 'px';
            overlayEl.style.height = applied.h + 'px';
            if (primary) {
                primary.el.style.left = applied.l + 'px';
                primary.el.style.top  = applied.t + 'px';
                primary.el.style.width  = applied.w + 'px';
                primary.el.style.height = applied.h + 'px';
            }
            handle._lastDx = dx; handle._lastDy = dy;
        }
    };

    const onUp = ev => {
        try { handle.releasePointerCapture(ev.pointerId); } catch {}
        handle.removeEventListener('pointermove', onMove);
        handle.removeEventListener('pointerup', onUp);
        handle.removeEventListener('pointercancel', onUp);
        clearGuides();

        const dx = handle._lastDx ?? (ev.clientX - startX) / zoom;
        const dy = handle._lastDy ?? (ev.clientY - startY) / zoom;
        dotnetRef.invokeMethodAsync('CommitGeometry', kind, dx, dy);
    };

    handle.addEventListener('pointermove', onMove);
    handle.addEventListener('pointerup', onUp);
    handle.addEventListener('pointercancel', onUp);
}

function applyResize(l0, t0, w0, h0, kind, dx, dy) {
    let l = l0, t = t0, w = w0, h = h0;
    switch (kind) {
        case 'e':  w += dx; break;
        case 's':  h += dy; break;
        case 'se': w += dx; h += dy; break;
        case 'w':  l += dx; w -= dx; break;
        case 'n':  t += dy; h -= dy; break;
        case 'nw': l += dx; w -= dx; t += dy; h -= dy; break;
        case 'ne': t += dy; h -= dy; w += dx; break;
        case 'sw': l += dx; w -= dx; h += dy; break;
    }
    if (w < 4) w = 4;
    if (h < 4) h = 4;
    return { l, t, w, h };
}

// Rubber-band selection.
export function installRubberBand(canvasEl, dotnetRef) {
    if (!canvasEl || canvasEl.dataset.rubberBound === '1') return;
    canvasEl.dataset.rubberBound = '1';
    let start = null;
    let band = null;

    canvasEl.addEventListener('pointerdown', e => {
        if (e.button !== 0) return;
        if (e.target.closest('.item') || e.target.closest('.sel-overlay') ||
            e.target.closest('.sel-outline') || e.target.closest('.tablix-handle') ||
            e.target.closest('.ctx-menu')) return;
        start = { x: e.offsetX, y: e.offsetY };
        band = document.createElement('div');
        band.className = 'rubber-band';
        band.style.cssText = `position:absolute;left:${start.x}px;top:${start.y}px;width:0;height:0;pointer-events:none;z-index:100001;background:rgba(0,122,204,.15);border:1px solid #007acc;`;
        canvasEl.appendChild(band);
        canvasEl.setPointerCapture(e.pointerId);
    });
    canvasEl.addEventListener('pointermove', e => {
        if (!start || !band) return;
        const x = Math.min(start.x, e.offsetX);
        const y = Math.min(start.y, e.offsetY);
        const w = Math.abs(e.offsetX - start.x);
        const h = Math.abs(e.offsetY - start.y);
        band.style.left = x + 'px'; band.style.top = y + 'px';
        band.style.width = w + 'px'; band.style.height = h + 'px';
    });
    canvasEl.addEventListener('pointerup', e => {
        if (!start || !band) return;
        const x = Math.min(start.x, e.offsetX);
        const y = Math.min(start.y, e.offsetY);
        const w = Math.abs(e.offsetX - start.x);
        const h = Math.abs(e.offsetY - start.y);
        band.remove(); band = null; start = null;
        try { canvasEl.releasePointerCapture(e.pointerId); } catch {}
        if (w < 3 && h < 3) return;
        dotnetRef.invokeMethodAsync('SelectInRect', x, y, w, h);
    });
}

// Tablix gridline resize. One listener on the canvas; finds .tablix-handle children.
export function installTablixResize(canvasEl, dotnetRef) {
    if (!canvasEl || canvasEl.dataset.tablixResizeBound === '1') return;
    canvasEl.dataset.tablixResizeBound = '1';
    canvasEl.__dotnet = dotnetRef;
}

export function rebindTablixHandles(canvasEl) {
    if (!canvasEl) return;
    const dotnetRef = canvasEl.__dotnet;
    if (!dotnetRef) return;
    canvasEl.querySelectorAll('.tablix-handle').forEach(h => {
        if (h.dataset.bound === '1') return;
        h.dataset.bound = '1';
        h.addEventListener('pointerdown', e => beginTablixResize(e, h, canvasEl, dotnetRef));
    });
}

function beginTablixResize(e, handle, canvasEl, dotnetRef) {
    if (e.button !== 0) return;
    e.preventDefault(); e.stopPropagation();

    const axis = handle.dataset.axis;
    const index = parseInt(handle.dataset.index, 10);
    const handlesEl = handle.parentElement;
    const tablixName = handlesEl.dataset.tablixName;
    const grid = canvasEl.querySelector(`.tablix-grid[data-tablix-name="${tablixName}"]`);
    const tablixItem = grid?.closest('.item');
    if (!grid || !tablixItem) return;

    // zoom from the canvas transform.
    const tr = canvasEl.style.transform || '';
    const m = tr.match(/scale\(([\d.]+)\)/);
    const zoom = m ? parseFloat(m[1]) : 1;

    const cols = (grid.style.gridTemplateColumns || '').split(/\s+/).filter(x => x).map(parseFloat);
    const rows = (grid.style.gridTemplateRows    || '').split(/\s+/).filter(x => x).map(parseFloat);
    const startCols = [...cols], startRows = [...rows];
    const startItemW = parseFloat(tablixItem.style.width)  || 0;
    const startItemH = parseFloat(tablixItem.style.height) || 0;
    const startHandlesW = parseFloat(handlesEl.style.width)  || 0;
    const startHandlesH = parseFloat(handlesEl.style.height) || 0;
    const startX = e.clientX, startY = e.clientY;

    handle.setPointerCapture(e.pointerId);

    const onMove = ev => {
        if (axis === 'col') {
            const dx = (ev.clientX - startX) / zoom;
            const newW = Math.max(8, startCols[index] + dx);
            const realDx = newW - startCols[index];
            const newCols = [...startCols]; newCols[index] = newW;
            grid.style.gridTemplateColumns = newCols.map(w => w + 'px').join(' ');
            tablixItem.style.width = (startItemW + realDx) + 'px';
            handlesEl.style.width  = (startHandlesW + realDx) + 'px';
            // Shift subsequent col handles.
            handlesEl.querySelectorAll('.col-handle').forEach((h, i) => {
                if (i > index) {
                    const orig = parseFloat(h.dataset.origLeft ?? (h.dataset.origLeft = h.style.left));
                    h.style.left = (orig + realDx) + 'px';
                }
            });
        } else {
            const dy = (ev.clientY - startY) / zoom;
            const newH = Math.max(8, startRows[index] + dy);
            const realDy = newH - startRows[index];
            const newRows = [...startRows]; newRows[index] = newH;
            grid.style.gridTemplateRows = newRows.map(h => h + 'px').join(' ');
            tablixItem.style.height = (startItemH + realDy) + 'px';
            handlesEl.style.height  = (startHandlesH + realDy) + 'px';
            handlesEl.querySelectorAll('.row-handle').forEach((h, i) => {
                if (i > index) {
                    const orig = parseFloat(h.dataset.origTop ?? (h.dataset.origTop = h.style.top));
                    h.style.top = (orig + realDy) + 'px';
                }
            });
        }
    };

    const onUp = ev => {
        try { handle.releasePointerCapture(ev.pointerId); } catch {}
        handle.removeEventListener('pointermove', onMove);
        handle.removeEventListener('pointerup', onUp);
        // Clear cached origs so a future drag re-snapshots.
        handlesEl.querySelectorAll('.tablix-handle').forEach(h => {
            delete h.dataset.origLeft; delete h.dataset.origTop;
        });
        const delta = axis === 'col'
            ? (ev.clientX - startX) / zoom
            : (ev.clientY - startY) / zoom;
        dotnetRef.invokeMethodAsync('CommitTablixResize', tablixName, axis, index, delta);
    };
    handle.addEventListener('pointermove', onMove);
    handle.addEventListener('pointerup', onUp);
}
