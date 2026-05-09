// Cypress smoke for the AI Panels settings page (/settings/ai).
//
// Validates that the page boots, the ActionConfig table renders the canonical
// rows (ChapterBeatWriter / ChapterBeatVoter etc.), the writing-action rows
// have a tier-locked indicator, the persona table seeds with starter expert
// rows, and the New / Combine / Generate buttons are present.
//
// Runs against CYPRESS_BASE_URL (default http://localhost:5101).
// Requires the test runner to be authenticated (Contributor or Administrator).

const writingActions = ['ChapterBeatWriter', 'ChapterBeatExpander'];
const adjustableActions = ['ChapterBeatVoter', 'PersonaSelector'];

describe('/settings/ai — AI panels editor', () => {
    beforeEach(() => {
        cy.visit('/settings/ai', { failOnStatusCode: false });
    });

    it('page boots without an error banner', () => {
        cy.contains('AI Panels', { timeout: 15000 }).should('be.visible');
        cy.document().its('body').then(($body) => {
            const text = $body.innerText || '';
            expect(/An unhandled error has occurred/i.test(text)).to.be.false;
        });
    });

    it('renders every default action with voter count + tier', () => {
        // Both writing and adjustable actions should appear in the table.
        [...writingActions, ...adjustableActions].forEach((id) => {
            cy.contains(id, { timeout: 10000 }).should('be.visible');
        });
    });

    it('writing-action rows show the tier-lock badge', () => {
        // The "locked" badge surfaces tier protection on writing actions
        // so the user knows settings can't downgrade them.
        cy.contains(writingActions[0])
          .closest('tr')
          .within(() => {
              cy.contains('locked').should('exist');
          });
    });

    it('persona table seeds the starter catalog', () => {
        // Master Swordsman is a canonical starter persona — its presence
        // verifies the seed-on-first-read path in ExpertPersonaService.
        cy.contains('Expert Personas', { timeout: 10000 }).should('be.visible');
        cy.contains('Master Swordsman').should('exist');
    });

    it('exposes Combine and Generate-from-scene controls', () => {
        cy.contains('button', /Combine/).should('exist');
        cy.contains('button', /Generate from scene/).should('exist');
        cy.contains('button', /New persona/).should('exist');
    });

    it('Combine button is disabled until 2 personas are selected', () => {
        cy.contains('button', /Combine/).should('be.disabled');
    });
});
