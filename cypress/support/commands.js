// Custom Cypress commands.
//
// ── cy.login(email?, password?) ──────────────────────────────────────────
// Authenticates by submitting the /api/auth/login form endpoint with the
// CSRF token harvested from /login. Wrapped in cy.session() so the auth
// cookie is reused across tests within a spec run (one login per email).
//
// Defaults pull from Cypress env (Cypress.env('test_email') / Cypress.env('test_password')).
// Override per-run via:
//   CYPRESS_test_email=qa@example.com CYPRESS_test_password=hunter2 npm run e2e
//
// In a dev environment the DevAutoLoginMiddleware signs the browser in
// automatically on first request, so a bare cy.visit('/') is usually
// enough to reach an authenticated state. The explicit cy.login() exists
// for non-dev runs (CI, staging) where that middleware is off.
//
// ── cy.ensureAuthenticated() ─────────────────────────────────────────────
// Tries dev auto-login first (the cheap path); falls back to cy.login()
// only if the auto-login cookie hasn't been set. This is what specs should
// call from their before/beforeEach hook.

Cypress.Commands.add('login', (email, password) => {
  const e = email || Cypress.env('test_email')    || 'admin@streetsamurai.local';
  const p = password || Cypress.env('test_password') || 'admin';

  cy.session([e], () => {
    // Step 1: hit /login to receive the antiforgery cookie + token.
    cy.request({ url: '/login', failOnStatusCode: false }).then((res) => {
      const html = res.body || '';
      const match = html.match(/name="__RequestVerificationToken"[^>]*value="([^"]+)"/);
      if (!match) {
        throw new Error('Could not find __RequestVerificationToken on /login. Is the app running on baseUrl?');
      }
      const token = match[1];

      // Step 2: POST form data to /api/auth/login. The login endpoint
      // validates the antiforgery cookie+token pair and SignsIn on success.
      cy.request({
        method: 'POST',
        url: '/api/auth/login',
        form: true,
        body: {
          email: e,
          password: p,
          returnUrl: '/',
          __RequestVerificationToken: token,
        },
        followRedirect: false,
        failOnStatusCode: false,
      }).then((loginRes) => {
        // 302 → / on success, 302 → /login?error=invalid on bad credentials.
        const loc = loginRes.headers.location || '';
        if (loc.includes('error=invalid')) {
          throw new Error(`Login failed for ${e}: invalid credentials.`);
        }
      });
    });
  });
});

Cypress.Commands.add('ensureAuthenticated', () => {
  // Cheap path: visit any authenticated-required page; the
  // DevAutoLoginMiddleware will sign us in automatically in dev environments.
  // If we then land on /login (because auto-login is off), fall back to
  // an explicit login.
  cy.visit('/strands', { failOnStatusCode: false });
  cy.url().then((url) => {
    if (url.includes('/login')) {
      cy.login();
      cy.visit('/strands');
    }
  });
});
