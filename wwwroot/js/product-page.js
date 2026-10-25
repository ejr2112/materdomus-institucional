(function () {
    var page = document.querySelector(".product-page");
    if (!page) return;

    var photo = document.getElementById("product-photo");
    if (!photo) return;

    page.addEventListener("click", function (event) {
        var thumb = event.target.closest ? event.target.closest(".product-page__thumb") : null;
        if (!thumb) return;

        var full = thumb.getAttribute("data-full");
        var alt = thumb.getAttribute("data-alt") || "";
        if (!full) return;

        photo.setAttribute("src", full);
        photo.setAttribute("alt", alt);

        var thumbs = page.querySelectorAll(".product-page__thumb");
        Array.prototype.forEach.call(thumbs, function (node) {
            var active = node === thumb;
            node.classList.toggle("is-active", active);
            if (active) node.setAttribute("aria-current", "true");
            else node.removeAttribute("aria-current");
        });
    });
})();
