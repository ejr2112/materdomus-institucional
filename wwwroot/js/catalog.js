(function () {
    var page = document.querySelector(".produtos-page");
    if (!page) return;

    var search = page.querySelector(".filter-bar__search");
    var category = page.querySelector(".filter-bar__select");
    var favButton = page.querySelector(".filter-bar__favorites-btn");
    var favBadge = document.getElementById("favorites-badge");
    var emptyFavorites = document.getElementById("empty-favorites");
    var emptySearch = document.getElementById("empty-search");
    var unavailable = document.getElementById("detail-unavailable");
    var backdrop = document.getElementById("product-detail-backdrop");
    var dialog = backdrop ? backdrop.querySelector(".product-detail") : null;
    var favorites = new Set();
    var favoritesOnly = false;
    var lastFocus = null;
    var keyHandler = null;

    function cards() {
        return Array.prototype.slice.call(page.querySelectorAll(".product-card"));
    }

    function text(id, value) {
        var node = document.getElementById(id);
        if (node) node.textContent = value || "";
    }

    function show(node, visible) {
        if (!node) return;
        node.hidden = !visible;
    }

    function applyFilters() {
        var query = (search && search.value ? search.value : "").trim().toLowerCase();
        var selected = category ? category.value : "";
        var any = false;

        cards().forEach(function (card) {
            var name = (card.getAttribute("data-name") || "").toLowerCase();
            var description = (card.getAttribute("data-description") || "").toLowerCase();
            var cat = card.getAttribute("data-category") || "";
            var id = card.getAttribute("data-id") || "";
            var matchesQuery = !query || name.indexOf(query) !== -1 || description.indexOf(query) !== -1;
            var matchesCategory = !selected || cat === selected;
            var matchesFav = !favoritesOnly || favorites.has(id);
            var visible = matchesQuery && matchesCategory && matchesFav;
            card.hidden = !visible;
            if (visible) any = true;
        });

        page.querySelectorAll(".products-section").forEach(function (section) {
            var visibleCard = section.querySelector(".product-card:not([hidden])");
            section.hidden = !visibleCard;
        });

        var total = cards().length;
        show(emptyFavorites, total > 0 && favoritesOnly && !any);
        show(emptySearch, total > 0 && !favoritesOnly && !any);
        show(unavailable, false);

        if (favBadge) {
            var count = favorites.size;
            favBadge.hidden = count === 0;
            var countNode = favBadge.querySelector(".favorites-badge__count");
            if (countNode) countNode.textContent = String(count);
            var label = favBadge.querySelector(".favorites-badge__label");
            if (label) {
                label.textContent = count === 1 ? "produto favoritado" : "produtos favoritados";
            }
        }
    }

    function ensureSrc(img) {
        if (!img) return;
        var source = img.getAttribute("data-src");
        if (source && img.getAttribute("src") !== source) img.setAttribute("src", source);
    }

    function showSlide(gallery, index, userChose) {
        var slides = gallery.querySelectorAll(".product-card__slide");
        if (!slides.length) return;
        if (index < 0) index = slides.length - 1;
        if (index >= slides.length) index = 0;

        if (userChose) gallery.classList.add("is-browsing");
        gallery.setAttribute("data-active-index", String(index));

        Array.prototype.forEach.call(slides, function (slide, i) {
            var active = i === index;
            slide.classList.toggle("product-card__slide--active", active);
            if (active) ensureSrc(slide);
            slide.setAttribute("aria-hidden", active ? "false" : "true");
            if (!active) slide.setAttribute("aria-hidden", "true");
        });

        gallery.querySelectorAll(".product-card__dot").forEach(function (dot, i) {
            var active = i === index;
            dot.classList.toggle("product-card__dot--active", active);
            dot.setAttribute("aria-pressed", active ? "true" : "false");
            if (active) dot.setAttribute("aria-current", "true");
            else dot.removeAttribute("aria-current");
        });
    }

    function preloadGallery(gallery) {
        gallery.querySelectorAll(".product-card__slide").forEach(ensureSrc);
    }

    if (search) search.addEventListener("input", applyFilters);
    if (category) category.addEventListener("change", applyFilters);
    if (favButton) {
        favButton.addEventListener("click", function () {
            favoritesOnly = !favoritesOnly;
            favButton.classList.toggle("filter-bar__favorites-btn--active", favoritesOnly);
            favButton.setAttribute("aria-pressed", favoritesOnly ? "true" : "false");
            applyFilters();
        });
    }

    page.addEventListener("pointerover", function (event) {
        var gallery = event.target.closest ? event.target.closest(".product-card__gallery") : null;
        if (gallery) preloadGallery(gallery);
    });

    page.addEventListener("click", function (event) {
        var dot = event.target.closest ? event.target.closest(".product-card__dot") : null;
        if (dot) {
            var gallery = dot.closest(".product-card__gallery");
            var dots = Array.prototype.slice.call(gallery.querySelectorAll(".product-card__dot"));
            showSlide(gallery, dots.indexOf(dot), true);
            return;
        }

        var prev = event.target.closest ? event.target.closest(".product-card__arrow--prev") : null;
        var next = event.target.closest ? event.target.closest(".product-card__arrow--next") : null;
        if (prev || next) {
            var arrowGallery = (prev || next).closest(".product-card__gallery");
            var current = parseInt(arrowGallery.getAttribute("data-active-index") || "0", 10);
            showSlide(arrowGallery, current + (next ? 1 : -1), true);
            return;
        }

        var fav = event.target.closest ? event.target.closest(".product-card__favorite-btn") : null;
        if (fav) {
            var card = fav.closest(".product-card");
            var id = card.getAttribute("data-id");
            var active = favorites.has(id);
            if (active) favorites.delete(id);
            else favorites.add(id);
            fav.classList.toggle("product-card__favorite-btn--active", !active);
            fav.setAttribute("aria-pressed", !active ? "true" : "false");
            fav.setAttribute("aria-label", !active ? "Remover dos favoritos" : "Favoritar produto");
            fav.textContent = !active ? "♥" : "♡";
            applyFilters();
            return;
        }

        var details = event.target.closest ? event.target.closest(".product-card__details-btn") : null;
        if (details) {
            openDetail(details.closest(".product-card"), details);
        }
    });

    page.addEventListener("touchstart", function (event) {
        var gallery = event.target.closest ? event.target.closest(".product-card__gallery") : null;
        if (!gallery || !event.touches || !event.touches.length) return;
        gallery.dataset.touchX = String(event.touches[0].clientX);
        gallery.dataset.touchY = String(event.touches[0].clientY);
    }, { passive: true });

    page.addEventListener("touchend", function (event) {
        var gallery = event.target.closest ? event.target.closest(".product-card__gallery") : null;
        if (!gallery || gallery.dataset.touchX == null || !event.changedTouches || !event.changedTouches.length) return;
        var startX = parseFloat(gallery.dataset.touchX);
        var startY = parseFloat(gallery.dataset.touchY || "0");
        delete gallery.dataset.touchX;
        delete gallery.dataset.touchY;
        var dx = event.changedTouches[0].clientX - startX;
        var dy = event.changedTouches[0].clientY - startY;
        if (Math.abs(dx) < 40 || Math.abs(dx) <= Math.abs(dy)) return;
        var current = parseInt(gallery.getAttribute("data-active-index") || "0", 10);
        showSlide(gallery, current + (dx < 0 ? 1 : -1), true);
    }, { passive: true });

    function focusable(root) {
        return Array.prototype.slice.call(root.querySelectorAll("a[href], button:not([disabled]), input, [tabindex]:not([tabindex='-1'])"))
            .filter(function (el) { return !el.hidden && el.offsetParent !== null; });
    }

    function closeDetail() {
        if (!backdrop || backdrop.hidden) return;
        backdrop.hidden = true;
        if (keyHandler) {
            document.removeEventListener("keydown", keyHandler, true);
            keyHandler = null;
        }
        if (lastFocus && typeof lastFocus.focus === "function") lastFocus.focus();
        lastFocus = null;
    }

    function openDetail(card, trigger) {
        if (!card || !backdrop || !dialog) {
            show(unavailable, true);
            return;
        }

        var comingSoon = card.getAttribute("data-coming-soon") === "true";
        var amazon = card.getAttribute("data-amazon") || "";
        var name = card.getAttribute("data-name") || "";
        text("detail-category", card.getAttribute("data-category") || "");
        text("detail-name", name);
        text("detail-price", card.getAttribute("data-price") || "");
        text("detail-copy", card.getAttribute("data-copy") || card.getAttribute("data-description") || "");
        show(document.getElementById("detail-badge"), comingSoon);
        show(document.getElementById("detail-soon"), comingSoon);

        var image = document.getElementById("detail-image");
        if (image) {
            image.src = card.getAttribute("data-image") || "images/placeholder-product.png";
            image.alt = card.getAttribute("data-alt") || name;
        }

        var cta = document.getElementById("detail-amazon");
        if (cta) {
            var showCta = !comingSoon && amazon.indexOf("https://www.amazon.com.br/dp/") === 0;
            cta.hidden = !showCta;
            if (showCta) cta.href = amazon;
        }

        dialog.setAttribute("aria-label", name);
        lastFocus = trigger || document.activeElement;
        backdrop.hidden = false;

        keyHandler = function (event) {
            if (event.key === "Escape") {
                event.preventDefault();
                closeDetail();
                return;
            }
            if (event.key !== "Tab") return;
            var items = focusable(dialog);
            if (!items.length) {
                event.preventDefault();
                dialog.focus();
                return;
            }
            var first = items[0];
            var last = items[items.length - 1];
            var active = document.activeElement;
            if (event.shiftKey) {
                if (active === first || !dialog.contains(active)) {
                    event.preventDefault();
                    last.focus();
                }
            } else if (active === last || !dialog.contains(active)) {
                event.preventDefault();
                first.focus();
            }
        };
        document.addEventListener("keydown", keyHandler, true);
        var items = focusable(dialog);
        (items[0] || dialog).focus();
    }

    if (backdrop) {
        backdrop.addEventListener("click", function (event) {
            if (event.target === backdrop) closeDetail();
        });
        var closeBtn = backdrop.querySelector(".product-detail__close-btn");
        if (closeBtn) closeBtn.addEventListener("click", closeDetail);
    }
})();
