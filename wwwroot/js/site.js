(function () {
    function closeAll(except) {
        document.querySelectorAll('[data-bk-split].is-open').forEach(function (el) {
            if (el === except) return;
            el.classList.remove('is-open');
            var t = el.querySelector('[data-bk-split-toggle]');
            if (t) t.setAttribute('aria-expanded', 'false');
        });
    }

    // Toggle flyout (stopPropagation keeps the profile dropdown open)
    document.addEventListener('click', function (e) {
        var toggle = e.target.closest('[data-bk-split-toggle]');
        if (toggle) {
            e.preventDefault();
            e.stopPropagation();
            var wrap = toggle.closest('[data-bk-split]');
            var open = !wrap.classList.contains('is-open');
            closeAll(open ? wrap : null);
            wrap.classList.toggle('is-open', open);
            toggle.setAttribute('aria-expanded', open ? 'true' : 'false');
            return;
        }
        // Click inside the flyout (e.g. the static "current" row): don't close the profile menu
        if (e.target.closest('.bk-split-flyout') && !e.target.closest('button[type="submit"]')) {
            e.stopPropagation();
        }
    }, true);

    // Escape closes the flyout first
    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape' && document.querySelector('[data-bk-split].is-open')) {
            closeAll();
        }
    });

    // Reset when the profile dropdown closes
    document.addEventListener('hidden.bs.dropdown', function () { closeAll(); });
})();