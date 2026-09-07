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
