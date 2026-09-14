/**
 * Consentimento de cookies (LGPD, linha de base prática).
 * O Google Tag Manager (GTM-5FJ38X8C) só é injetado após "Aceitar"
 * ou quando já houver consentimento gravado como aceito.
 *
 * Noscript do GTM não é usado: sem JavaScript não há como registrar
 * a escolha do titular, então não carregamos tags de analytics.
 */
(function () {
    var STORAGE_KEY = "materdomus.cookieConsent";
    var GTM_ID = "GTM-5FJ38X8C";
    var ACCEPTED = "accepted";
    var REJECTED = "rejected";
    var gtmLoaded = false;

    function getConsent() {
        try {
            return localStorage.getItem(STORAGE_KEY);
        } catch (e) {
            return null;
        }
    }

    function setConsent(value) {
        try {
            localStorage.setItem(STORAGE_KEY, value);
        } catch (e) {
            /* private mode / quota — banner may reappear next visit */
        }
    }

    function loadGtm() {
        if (gtmLoaded || document.querySelector("script[src*='googletagmanager.com/gtm.js']")) {
            gtmLoaded = true;
            return;
        }

        gtmLoaded = true;
        window.dataLayer = window.dataLayer || [];
        window.dataLayer.push({
            "gtm.start": new Date().getTime(),
            event: "gtm.js"
        });

        var f = document.getElementsByTagName("script")[0];
        var j = document.createElement("script");
        j.async = true;
        j.src = "https://www.googletagmanager.com/gtm.js?id=" + GTM_ID;
        f.parentNode.insertBefore(j, f);
    }

    function bannerEl() {
        return document.getElementById("cookie-banner");
    }

    function showBanner() {
        var banner = bannerEl();
        if (!banner) return;
        banner.hidden = false;
    }

    function hideBanner() {
        var banner = bannerEl();
        if (!banner) return;
        banner.hidden = true;
    }

    function focusBanner() {
        var banner = bannerEl();
        if (!banner || banner.hidden) return;
        var btn = banner.querySelector("button");
        if (btn) btn.focus();
    }

    function accept() {
        setConsent(ACCEPTED);
        hideBanner();
        loadGtm();
    }

    function reject() {
        var withdrawing = gtmLoaded || getConsent() === ACCEPTED;
        setConsent(REJECTED);
        hideBanner();
        if (withdrawing) {
            location.reload();
        }
    }

    function init() {
        var consent = getConsent();
        if (consent === ACCEPTED) {
            loadGtm();
            return;
        }
        if (consent === REJECTED) {
            return;
        }
        showBanner();
    }

    function bind() {
        var acceptBtn = document.getElementById("cookie-accept");
        var rejectBtn = document.getElementById("cookie-reject");
        if (acceptBtn) acceptBtn.addEventListener("click", accept);
        if (rejectBtn) rejectBtn.addEventListener("click", reject);

        document.addEventListener("keydown", function (event) {
            if (event.key !== "Escape") return;
            var banner = bannerEl();
            if (!banner || banner.hidden) return;
            if (getConsent()) hideBanner();
        });

        init();
    }

    window.materDomusOpenCookiePreferences = function () {
        showBanner();
        focusBanner();
    };

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", bind);
    } else {
        bind();
    }
})();
