// UserLogin — the sign-in form is a native HTML POST owned by MindAttic.Authentication's
// /_ma-auth/login endpoint, so this component has no behavior of its own. The only enhancement
// is focusing the username field on load. Idempotent (pin-footer.js pattern).
(function () {
    'use strict';
    if (window.__userLoginInited) return;
    window.__userLoginInited = true;

    function focusFirst() {
        var el = document.querySelector('.ul-card .ma-auth-field input[name="userName"]');
        if (el && !el.value) el.focus();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', focusFirst);
    } else {
        focusFirst();
    }
})();
