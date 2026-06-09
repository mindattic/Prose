// UserCircle — avatar dropdown + logout. Idempotent (pin-footer.js pattern). Delegated click
// handler; supports two modes per .uc-root[data-uc-click]:
//   "menu"   (default) — clicking the circle toggles the dropdown (whose item is the logout form)
//   "logout"           — clicking the circle directly submits the nearest .uc-logout-form
// Logout ALWAYS submits the real <form> (requestSubmit) so the AntiforgeryToken + __Host- cookie
// semantics hold. Never fetch/XHR the logout.
(function () {
    'use strict';
    if (window.__userCircleInited) return;
    window.__userCircleInited = true;

    function closeAll(except) {
        document.querySelectorAll('.uc-root.uc-open').forEach(function (r) {
            if (r !== except) r.classList.remove('uc-open');
        });
    }

    document.addEventListener('click', function (e) {
        var btn = e.target.closest ? e.target.closest('.uc-btn') : null;
        if (btn) {
            var root = btn.closest('.uc-root');
            if (!root) return;
            var mode = root.dataset ? root.dataset.ucClick : 'menu';
            if (mode === 'logout') {
                var form = root.querySelector('.uc-logout-form');
                if (form) { e.preventDefault(); form.requestSubmit(); }
                return;
            }
            // menu mode: toggle this root, close others
            var willOpen = !root.classList.contains('uc-open');
            closeAll(root);
            root.classList.toggle('uc-open', willOpen);
            return;
        }
        // click outside any .uc-root closes all open menus
        if (!(e.target.closest && e.target.closest('.uc-root'))) closeAll(null);
    });

    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') closeAll(null);
    });
})();
