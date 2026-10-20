window.trackPageView = function (path) {
    window.dataLayer = window.dataLayer || [];
    window.dataLayer.push({
        event: 'spa_page_view',
        page_path: path
    });
};

window.trackEvent = function (eventName, params) {
    window.dataLayer = window.dataLayer || [];
    window.dataLayer.push({
        event: eventName,
        ...params
    });
};

function readTrackPayload(el, attr) {
    var raw = el.getAttribute(attr);
    if (!raw) return null;
    try {
        return JSON.parse(raw);
    } catch (e) {
        return null;
    }
}

function emitTrack(data) {
    if (!data || !data.event || typeof window.trackEvent !== "function") return;
    var params = Object.assign({}, data);
    delete params.event;
    window.trackEvent(data.event, params);
}

document.addEventListener("click", function (event) {
    var el = event.target && event.target.closest ? event.target.closest("[data-track]") : null;
    if (!el) return;
    emitTrack(readTrackPayload(el, "data-track"));
});

document.addEventListener("focusin", function (event) {
    var section = event.target && event.target.closest ? event.target.closest("[data-track-focus]") : null;
    if (!section || section.getAttribute("data-tracked") === "1") return;
    section.setAttribute("data-tracked", "1");
    emitTrack(readTrackPayload(section, "data-track-focus"));
});

function trackInitialPageView() {
    window.trackPageView(window.location.pathname + window.location.search);
}

if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", trackInitialPageView);
} else {
    trackInitialPageView();
}
