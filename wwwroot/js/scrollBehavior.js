// wwwroot/js/scrollBehavior.js

let lastScrollY = 0;
let headerEl = null;
let scrollHandler = null;
const SCROLL_THRESHOLD = 10;
const MOBILE_BREAKPOINT = 768;

export function initScrollBehavior(headerId) {
    headerEl = document.getElementById(headerId);
    if (!headerEl) return;

    lastScrollY = window.scrollY;

    scrollHandler = () => {
        // Disable on mobile
        if (window.innerWidth <= MOBILE_BREAKPOINT) {
            headerEl.classList.remove('header--hidden');
            return;
        }

        const currentScrollY = window.scrollY;

        // Always show at top of page
        if (currentScrollY <= SCROLL_THRESHOLD) {
            headerEl.classList.remove('header--hidden');
            lastScrollY = currentScrollY;
            return;
        }

        const delta = currentScrollY - lastScrollY;

        if (delta > SCROLL_THRESHOLD) {
            // Scrolling down: hide header
            headerEl.classList.add('header--hidden');
        } else if (delta < 0) {
            // Scrolling up: show header
            headerEl.classList.remove('header--hidden');
        }

        lastScrollY = currentScrollY;
    };

    window.addEventListener('scroll', scrollHandler, { passive: true });
}

export function destroyScrollBehavior() {
    if (scrollHandler) {
        window.removeEventListener('scroll', scrollHandler);
        scrollHandler = null;
    }
    headerEl = null;
}
