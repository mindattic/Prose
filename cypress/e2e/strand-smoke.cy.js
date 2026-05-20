// Cypress smoke test for the unified writer/recorder/listener at /strand/{id}.
// Validates the workbench round-trip without touching TTS (no ElevenLabs credits
// burned): visit /strands → create a new strand → land on /strand/{slug} →
// insert a beat → write prose → save → split → join → delete → confirm gone.
//
// Runs against CYPRESS_BASE_URL (default http://localhost:5101). The page
// is behind [Authorize] so the harness must already be authenticated —
// run via `cypress run --env auth_cookie=...` or open a logged-in browser
// session before running interactively.
//
// Selectors use data-cy hooks declared in Strand.razor so the spec is robust
// against future CSS rewrites.

const failurePhrases = [
  /An unhandled error has occurred/i,
  /Server error/i,
  /Sorry, something went wrong/i,
];

function noErrorBanner() {
  cy.document().its('body').then(($body) => {
    const text = $body.innerText || '';
    failurePhrases.forEach((rx) => {
      expect(rx.test(text), `should not show "${rx}"`).to.be.false;
    });
  });
}

describe('Strand workbench smoke', () => {
  let strandTitle;

  before(() => {
    // Authenticate once per spec. In dev environments the auto-login
    // middleware handles this transparently; outside dev the cy.login()
    // command POSTs to /api/auth/login with a harvested CSRF token.
    cy.ensureAuthenticated();
  });

  beforeEach(() => {
    // Unique title per run so the test is replayable without manual cleanup.
    strandTitle = `Cypress Smoke ${Date.now()}`;
  });

  it('strand index renders', () => {
    cy.visit('/strands', { failOnStatusCode: false });
    noErrorBanner();
    cy.contains(/strands/i, { timeout: 10000 }).should('exist');
  });

  it('round-trip: create → insert → edit → split → join → delete', () => {
    // 1. Create a brand-new strand from the index.
    cy.visit('/strands');
    cy.get('input[placeholder*="New strand title"]').type(strandTitle);
    cy.contains('button', /Create/i).click();

    // We land on /strand/{slug}. Slug not predictable; assert URL pattern.
    cy.url().should('match', /\/strand\/[a-z0-9-]+/i);
    noErrorBanner();

    // 2. Empty-state CTA: add the first beat.
    cy.get('[data-cy="insert-first"]').click();

    // 3. Editor opens automatically. Type prose and save with Ctrl+Enter.
    cy.get('textarea.beat-textarea').should('be.visible').and('be.focused');
    cy.focused().type(
      'First sentence runs here. Second sentence picks up after a clean break.',
      { delay: 0 },
    );
    cy.get('[data-cy="beat-save"]').click();

    // 4. The beat now shows as prose, not the textarea.
    cy.get('[data-cy="beat-row"]').should('have.length', 1);
    cy.get('[data-cy="beat-prose"]').should('contain.text', 'First sentence');

    // 5. Insert a second beat after the first via the insert bar.
    cy.get('[data-cy="insert-after"]').first().click({ force: true });
    cy.get('textarea.beat-textarea').type('Another paragraph.');
    cy.get('[data-cy="beat-save"]').click();
    cy.get('[data-cy="beat-row"]').should('have.length', 2);

    // 6. Split the first beat — should produce three rows total.
    cy.get('[data-cy="beat-row"]').first().find('[data-cy="beat-split"]').click();
    cy.get('[data-cy="beat-row"]', { timeout: 5000 }).should('have.length', 3);

    // 7. Join the second beat back into the first.
    cy.get('[data-cy="beat-row"]').eq(1).find('[data-cy="beat-join"]').click();
    cy.get('[data-cy="beat-row"]').should('have.length', 2);

    // 8. Delete the second (remaining) beat — confirmation modal appears.
    cy.get('[data-cy="beat-row"]').last().find('[data-cy="beat-delete"]').click();
    cy.get('[data-cy="confirm-delete"]').click();
    cy.get('[data-cy="beat-row"]').should('have.length', 1);

    noErrorBanner();
  });
});
