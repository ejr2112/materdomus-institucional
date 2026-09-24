window.materdomusSeo = (function () {
    var ORIGIN = 'https://www.materdomus.com.br';

    // Tags sociais da home. Precisam bater com index.html — /produtos troca
    // estes valores e as demais rotas voltam para eles. O <title> e a
    // meta description de cada página continuam com o SeoMeta do Blazor,
    // exceto na vitrine, onde o HTML estático e este script também os definem.
    var HOME_SOCIAL = {
        title: 'Mater Domus | Utilidades domésticas Ou e Linha Flow',
        description: 'Loja brasileira de utilidades domésticas Ou e Linha Flow, com catálogo na Amazon. Organização e itens para o lar. Também falamos com fornecedores.',
        image: ORIGIN + '/images/logo.png',
        imageAlt: 'Logotipo da Mater Domus'
    };

    var vitrineRequest = 0;

    function normalizePath(pathname) {
        var path = String(pathname || '/').replace(/\/+$/, '');
        if (!path) {
            path = '/';
        }
        return path;
    }

    function canonicalForPath(pathname) {
        var path = normalizePath(pathname);
        return path === '/' ? ORIGIN + '/' : ORIGIN + path;
    }

    function setContent(selector, value) {
        var node = document.querySelector(selector);
        if (node && value) {
            node.setAttribute('content', value);
        }
    }

    function applySocial(meta) {
        if (!meta) {
            return;
        }

        setContent('meta[property="og:title"]', meta.title);
        setContent('meta[property="og:description"]', meta.description);
        setContent('meta[property="og:image"]', meta.image);
        setContent('meta[property="og:image:alt"]', meta.imageAlt);
        setContent('meta[name="twitter:title"]', meta.title);
        setContent('meta[name="twitter:description"]', meta.description);
        setContent('meta[name="twitter:image"]', meta.image);
        setContent('meta[name="twitter:image:alt"]', meta.imageAlt);
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

    function removeVitrineJsonLd() {
        var node = document.getElementById('vitrine-itemlist');
        if (node) {
            node.remove();
        }
    }

    function injectVitrineJsonLd(jsonText) {
        var existing = document.getElementById('vitrine-itemlist');
        if (existing) {
            return;
        }

        var script = document.createElement('script');
        script.type = 'application/ld+json';
        script.id = 'vitrine-itemlist';
        script.text = jsonText;
        document.head.appendChild(script);
    }

    function applyVitrine() {
        var requestId = ++vitrineRequest;

        fetch('/data/vitrine-meta.json')
            .then(function (response) {
                return response.ok ? response.json() : null;
            })
            .then(function (meta) {
                if (!meta || requestId !== vitrineRequest) {
                    return;
                }
                if (normalizePath(window.location.pathname) !== '/produtos') {
                    return;
                }

                applySocial(meta);
                if (meta.title) {
                    document.title = meta.title;
                }
                setContent('meta[name="description"]', meta.description);
            })
            .catch(function () { });

        if (document.getElementById('vitrine-itemlist')) {
            return;
        }

        fetch('/data/vitrine-itemlist.json')
            .then(function (response) {
                return response.ok ? response.text() : null;
            })
            .then(function (jsonText) {
                if (!jsonText || requestId !== vitrineRequest) {
                    return;
                }
                if (normalizePath(window.location.pathname) !== '/produtos') {
                    return;
                }

                injectVitrineJsonLd(jsonText);
            })
            .catch(function () { });
    }

    function applyFromLocation() {
        var path = normalizePath(window.location.pathname);
        applyCanonical(canonicalForPath(path));

        if (path === '/produtos') {
            applyVitrine();
            return;
        }

        vitrineRequest++;
        removeVitrineJsonLd();
        applySocial(HOME_SOCIAL);
        setContent('meta[name="description"]', HOME_SOCIAL.description);
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
