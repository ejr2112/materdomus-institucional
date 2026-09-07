// wwwroot/js/detailModal.js

const FOCUSABLE_SELECTOR = 'a[href], button:not([disabled]), input, [tabindex]:not([tabindex="-1"])';
const FOCUS_DELAY_MS = 0; // foco imediato; bem dentro do limite de 500 ms (Req 6.1)

let dialogEl = null;
let dotNetRef = null;
let keydownHandler = null;

function getFocusable() {
    if (!dialogEl) return [];
    // Filtra elementos ocultos (offsetParent nulo indica não renderizado/oculto)
    return Array.from(dialogEl.querySelectorAll(FOCUSABLE_SELECTOR))
        .filter(el => el instanceof HTMLElement && (el.offsetParent !== null || el === document.activeElement));
}

// Abre o diálogo: foca o primeiro focável (Req 6.1), instala focus trap (Req 6.2)
// e listener de Escape que aciona o callback .NET (Req 6.3).
export function open(dialog, ref) {
    // Remove qualquer estado remanescente antes de instalar um novo
    close();

    dialogEl = dialog;
    dotNetRef = ref;

    if (!dialogEl) return;

    // Move o foco para o primeiro elemento focável dentro do diálogo (Req 6.1)
    setTimeout(() => {
        const focusable = getFocusable();
        if (focusable.length > 0) {
            focusable[0].focus();
        } else if (dialogEl) {
            // Sem focáveis: garante que o foco entre no diálogo
            dialogEl.focus();
        }
    }, FOCUS_DELAY_MS);

    keydownHandler = (event) => {
        if (event.key === 'Escape') {
            event.preventDefault();
            if (dotNetRef) {
                dotNetRef.invokeMethodAsync('OnEscapePressed');
            }
            return;
        }

        if (event.key === 'Tab') {
            // Focus trap: mantém o foco contido no diálogo (Req 6.2)
            const focusable = getFocusable();
            if (focusable.length === 0) {
                event.preventDefault();
                return;
            }

            const first = focusable[0];
            const last = focusable[focusable.length - 1];
            const active = document.activeElement;

            if (event.shiftKey) {
                // Shift+Tab a partir do primeiro (ou de fora): volta para o último
                if (active === first || !dialogEl.contains(active)) {
                    event.preventDefault();
                    last.focus();
                }
            } else {
                // Tab a partir do último (ou de fora): volta para o primeiro
                if (active === last || !dialogEl.contains(active)) {
                    event.preventDefault();
                    first.focus();
                }
            }
        }
    };

    document.addEventListener('keydown', keydownHandler, true);
}

// Fecha o diálogo: remove os listeners instalados por open (Req 6.3 ciclo de vida).
export function close() {
    if (keydownHandler) {
        document.removeEventListener('keydown', keydownHandler, true);
        keydownHandler = null;
    }
    dialogEl = null;
    dotNetRef = null;
}

// Restaura o foco: tenta o gatilho original; se ausente, usa o fallback estável da grade (Req 6.4, 6.6).
export function restoreFocus(triggerSelector, fallbackSelector) {
    const trigger = triggerSelector ? document.querySelector(triggerSelector) : null;
    if (trigger instanceof HTMLElement && trigger.offsetParent !== null) {
        trigger.focus();
        return;
    }

    const fallback = fallbackSelector ? document.querySelector(fallbackSelector) : null;
    if (fallback instanceof HTMLElement) {
        fallback.focus();
    }
}
