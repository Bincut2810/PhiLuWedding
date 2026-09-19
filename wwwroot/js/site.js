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

    // ---- Brand-mark page loader ------------------------------------
    // Mirrors the reveal-style intro from luxury wedding sites:
    //  • Show the overlay as soon as it is in the DOM (server-rendered)
    //  • Wait until window.load so images and web fonts have arrived
    //    — otherwise the wipe starts before the logo is visible
    //  • Then animate it out with .is-leaving and pull it from the
    //    layout once the transition finishes
    //  • On bfcache restore (back/forward navigation) re-hide it so
    //    it doesn't reappear, and re-show it if the user navigates
    //    onward to another page in the same session
    var loader = document.querySelector("[data-page-loader]");
    if (loader) {
        var minShowMs = 900;   // shortest time the overlay is ever shown
        var maxShowMs = 5000;  // safety cap in case a load event never fires
        var fadeOutMs = 620;   // matches the .is-leaving transition in site.css
        var hideTimer = null;
        var armedTimer = null;

        var startTime = Date.now();
        var stateHidden = loader.hasAttribute("hidden");

        // If the server already hid it (rare), do nothing.
        if (!stateHidden) {
            var dismiss = function () {
                if (loader.hasAttribute("hidden") ||
                    loader.classList.contains("is-leaving")) {
                    return;
                }

                var elapsed = Date.now() - startTime;
                var wait = Math.max(0, minShowMs - elapsed);

                hideTimer = setTimeout(function () {
                    loader.classList.add("is-leaving");
                    // Drop from the layout once the fade-out finishes so
                    // it never blocks the page above and never traps
                    // focus or screen-reader attention.
                    setTimeout(function () {
                        loader.setAttribute("hidden", "hidden");
                    }, fadeOutMs);
                }, wait);
            };

            // window.load fires when every resource (img, font, css)
            // below the fold is ready. If something never loads, we
            // still tear the overlay down after maxShowMs so the page
            // is never permanently stuck behind the loader.
            var onReady = function () {
                if (armedTimer) clearTimeout(armedTimer);
                dismiss();
            };

            if (document.readyState === "complete") {
                onReady();
            } else {
                window.addEventListener("load", onReady, { once: true });
            }
            armedTimer = setTimeout(onReady, maxShowMs);
        }

        // Re-hide the overlay when the page is restored from the
        // back/forward cache. Browsers keep the DOM intact in bfcache,
        // which means our prior setAttribute("hidden") is preserved —
        // but earlier browser versions did not. Be defensive.
        window.addEventListener("pageshow", function (event) {
            if (event.persisted) {
                loader.setAttribute("hidden", "hidden");
                loader.classList.remove("is-leaving");
            }
        });
    }
})();
