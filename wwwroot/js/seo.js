window.materdomusSeo = (function () {
    var ORIGIN = 'https://www.materdomus.com.br';

    function canonicalForPath(pathname) {
        var path = String(pathname || '/').replace(/\/+$/, '');
        if (!path) {
            path = '/';
        }
        return path === '/' ? ORIGIN + '/' : ORIGIN + path;
    }

    function applyCanonical(url) {
        if (!url) {
            return;
        }

        var canonical = document.querySelector('link[rel="canonical"]');
        if (canonical) {
            canonical.setAttribute('href', url);
        }

        var ogUrl = document.querySelector('meta[property="og:url"]');
        if (ogUrl) {
            ogUrl.setAttribute('content', url);
        }
    }

    function applyFromLocation() {
        applyCanonical(canonicalForPath(window.location.pathname));
    }

    function patchHistory() {
        var pushState = window.history.pushState;
        window.history.pushState = function () {
            var result = pushState.apply(this, arguments);
            applyFromLocation();
            return result;
        };

        var replaceState = window.history.replaceState;
        window.history.replaceState = function () {
            var result = replaceState.apply(this, arguments);
            applyFromLocation();
            return result;
        };

        window.addEventListener('popstate', applyFromLocation);
    }

    applyFromLocation();
    patchHistory();

    return {
        canonicalForPath: canonicalForPath,
        applyCanonical: applyCanonical,
        applyFromLocation: applyFromLocation
    };
})();
