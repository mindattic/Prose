// Cypress smoke for the per-book routes added 2026-05-09.
//
// Picks an arbitrary book id from /books's data-book-id attribute (no
// fixture creation needed — uses whatever's on the bookshelf today),
// then visits each per-book route and asserts the page boots without
// the generic error banner.
//
// If the bookshelf is empty (fresh DB), every per-book test skips with a
// log line — no false failures from a deliberately-clean test environment.
//
// Routes covered:
//   /publish/{id}                     — EPUB / HTML / Markdown export page
//   /publish/{id}/audio               — TTS audiobook export page
//   /books/{id}/motifs                — motif management
//   /books/{id}/continuity-extract    — continuity extraction trigger

const failurePhrases = [
    /An unhandled error has occurred/i,
    /Server error/i,
    /Sorry, something went wrong/i,
];

function visitAndCheck(path) {
    cy.visit(path, { failOnStatusCode: false });
    cy.document().its('body').then(($body) => {
        const text = $body.innerText || '';
        failurePhrases.forEach((rx) => {
            expect(rx.test(text), `${path} should not show "${rx}"`).to.be.false;
        });
    });
}

describe('per-book routes (resolved from /books)', () => {
    let bookId = null;

    before(() => {
        // Visit the bookshelf and try to extract any book id. The card
        // markup carries data-book-id attribute (Books.razor) so we don't
        // depend on link hrefs (book-card uses @onclick, not href).
        cy.visit('/books', { failOnStatusCode: false });
        cy.get('body').then(($body) => {
            const cards = $body.find('[data-book-id]');
            if (cards.length > 0) {
                bookId = cards.first().attr('data-book-id');
                cy.log(`Resolved book id for per-book tests: ${bookId}`);
            } else {
                cy.log('No books on the shelf — per-book tests will skip.');
            }
        });
    });

    function skipIfNoBook() {
        if (!bookId) {
            cy.log('No book id available — skipping.');
            return false;
        }
        return true;
    }

    it('/publish/{id} renders the export page', function () {
        if (!skipIfNoBook()) this.skip();
        visitAndCheck(`/publish/${bookId}`);
        cy.contains('Publish', { timeout: 10000 }).should('be.visible');
    });

    it('/publish/{id}/audio renders the recording page', function () {
        if (!skipIfNoBook()) this.skip();
        visitAndCheck(`/publish/${bookId}/audio`);
        cy.contains('Recording', { timeout: 10000 }).should('be.visible');
        // Cost warning is part of the page contract — surfaces ElevenLabs billing
        cy.contains(/api cost/i).should('exist');
    });

    it('/books/{id}/motifs renders motif management', function () {
        if (!skipIfNoBook()) this.skip();
        visitAndCheck(`/books/${bookId}/motifs`);
        cy.contains('Motifs', { timeout: 10000 }).should('be.visible');
        cy.contains(/auto-detect/i).should('exist');
    });

    it('/books/{id}/continuity-extract renders the extract page', function () {
        if (!skipIfNoBook()) this.skip();
        visitAndCheck(`/books/${bookId}/continuity-extract`);
        cy.contains('Continuity Extract', { timeout: 10000 }).should('be.visible');
        cy.contains(/extract from every chapter/i).should('exist');
    });

    it('unknown book id renders a graceful "not found" alert', () => {
        // Doesn't need a real book — just verifies the not-found path doesn't
        // explode. The pages all render an alert-danger when LoadBook returns null.
        const fakeId = '00000000000000000000000000000000';
        visitAndCheck(`/publish/${fakeId}`);
        cy.contains(/book not found/i, { timeout: 10000 }).should('exist');
    });
});
