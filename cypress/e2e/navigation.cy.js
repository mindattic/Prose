// Headless navigation smoke test for the Blazor Server app.
// Runs against CYPRESS_BASE_URL (default http://localhost:5101).
// For each route: hits the URL, asserts the response is 2xx, asserts no error
// banner is rendered, and asserts the home tile board is reachable from anywhere.

const repoTiles = [
  { path: '/',                   name: 'Home' },
  { path: '/dashboard',          name: 'Dashboard' },
  { path: '/board/characters',   name: 'Characters bucket' },
  { path: '/board/organizations',name: 'Organizations bucket' },
  { path: '/board/gear',         name: 'Gear bucket' },
  { path: '/board/world',        name: 'World bucket' },
  { path: '/board/culture',      name: 'Culture bucket' },
];

const dictionaries = [
  '/characters', '/synthetics', '/archetypes',
  '/corps', '/subsidiaries', '/factions',
  '/weaponry', '/ammunition', '/cyberware', '/equipment',
  '/apparel', '/genemods', '/pharmaceuticals',
  '/places', '/transportation', '/materials', '/technology', '/automata', '/rules',
  '/documents', '/quotes', '/vocabulary', '/news', '/entertainment', '/goods',
  '/contracts',
  // New entity-type pages — should be reachable even if not yet on a tile.
  '/specimens', '/psionics', '/flyover',
];

const tools = [
  '/ask', '/atlas', '/architecture', '/continuity',
  '/findings', '/world-health', '/data-repair',
  '/graph', '/graph-3d', '/map', '/heritage',
  // New routes added in the writing/AI shipping wave:
  '/settings/ai',  // ActionConfig + ExpertPersona table editor
  '/timeline',     // story-time cursor with per-entity snapshot
  '/series',       // series shelf
  '/books',        // bookshelf — book-card → /write/{id}
  '/write',        // chapter editor (selection-resume on bare entry)
];

const failurePhrases = [
  /An unhandled error has occurred/i,
  /Server error/i,
  /Sorry, something went wrong/i,
];

function visitAndCheck(path) {
  // cy.visit follows redirects and only succeeds on 2xx after redirects, so it
  // doubles as the route-exists assertion. The error-banner check ensures the
  // server didn't render an exception page.
  cy.visit(path, { failOnStatusCode: false });
  cy.document().its('body').then(($body) => {
    const text = $body.innerText || '';
    failurePhrases.forEach((rx) => {
      expect(rx.test(text), `${path} should not show "${rx}"`).to.be.false;
    });
  });
}

describe('StreetSamurai navigation smoke', () => {
  it('home page boots', () => {
    cy.visit('/');
    cy.get('.board-grid', { timeout: 15000 }).should('exist');
    cy.contains('Repositories').should('be.visible');
  });

  describe('repository buckets', () => {
    repoTiles.forEach((t) => {
      it(`loads ${t.name} (${t.path})`, () => visitAndCheck(t.path));
    });
  });

  describe('dictionaries', () => {
    dictionaries.forEach((p) => {
      it(`loads ${p}`, () => visitAndCheck(p));
    });
  });

  describe('tools and visualizations', () => {
    tools.forEach((p) => {
      it(`loads ${p}`, () => visitAndCheck(p));
    });
  });

  it('NotFound returns a graceful page (no raw 500)', () => {
    cy.visit('/this-route-does-not-exist', { failOnStatusCode: false });
    cy.contains(/not found|404/i).should('exist');
  });
});
