/* =====================================================================
   SITE.JS — NHÀ HÀNG PHÌ LŨ
   Minimal vanilla JS for mobile navigation and small UI affordances.
   No frameworks. No dependencies.
   ===================================================================== */
(function () {
    "use strict";

    // ---- Mobile nav toggle -----------------------------------------
    var toggle = document.querySelector("[data-nav-toggle]");
    var nav = document.querySelector("[data-nav]");
    var header = document.querySelector("[data-header]");

    if (toggle && nav) {
        toggle.addEventListener("click", function () {
            var expanded = toggle.getAttribute("aria-expanded") === "true";
            var nextExpanded = !expanded;
            toggle.setAttribute("aria-expanded", String(nextExpanded));
            nav.classList.toggle("is-open", nextExpanded);
            toggle.setAttribute(
                "aria-label",
                nextExpanded ? "Đóng menu điều hướng" : "Mở menu điều hướng"
            );
        });

        // Close menu when clicking a nav link on small screens
        nav.addEventListener("click", function (event) {
            var target = event.target;
            if (target && target.tagName === "A") {
                toggle.setAttribute("aria-expanded", "false");
                nav.classList.remove("is-open");
                toggle.setAttribute("aria-label", "Mở menu điều hướng");
            }
        });
    }

    // ---- Close mobile nav when resizing up --------------------------
    var mq = window.matchMedia("(min-width: 768px)");
    var handleMq = function (e) {
        if (e.matches && nav && toggle) {
            nav.classList.remove("is-open");
            toggle.setAttribute("aria-expanded", "false");
        }
    };
    if (typeof mq.addEventListener === "function") {
        mq.addEventListener("change", handleMq);
    } else if (typeof mq.addListener === "function") {
        // Safari < 14 fallback
        mq.addListener(handleMq);
    }

    // ---- Smooth focus outline only via keyboard --------------------
    document.addEventListener("keydown", function (e) {
        if (e.key === "Tab") {
            document.body.classList.add("is-keyboard");
        }
    });
    document.addEventListener("mousedown", function () {
        document.body.classList.remove("is-keyboard");
    });
})();
