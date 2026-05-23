// Strand workbench UX coverage. Covers the 2026-05-23 redesign:
//
//   - Positional Beat #N display (rank within the strand, not the global
//     Beat.Number column).
//   - In-footer beat-actions (copy-text, LLM, re-record, delete). The
//     Edit / Split / Join / Copy-Id buttons were removed.
//   - Inline gap-after editor inside each beat-card footer (replaces the
//     standalone gap row between beats).
//   - IsChapterStart flag + chapter divider rendering + chapter index
//     jump-links at the top of the strand.
//   - Beat.Kind dropdown in the meta panel (prose / book-title / dedication
//     / quote) and its kind-aware prose render.
//   - LLM bottom sheet: opens on the ✨ button, shows strand.beat handle,
//     id-copy button, free-text textarea.
//
// Strategy: spin up a fresh strand per test, seed two beats, drive the
// real UI, and assert observable DOM state. Anything that would normally
// reach for the network (clipboard writes, audio playback, LLM calls) is
// stubbed at the window object so the test doesn't need browser permissions
// or real audio/API.
//
// Selectors use data-cy hooks declared in Strand.razor.

const TIMEOUT = 12000;

function seedStrand(title) {
  cy.visit('/strands');
  cy.get('input[placeholder*="New strand title"]').type(title);
  cy.contains('button', /Create/i).click();
  cy.url().should('match', /\/strand\/[a-z0-9-]+/i);
}

function addBeat(text) {
  // Insert via the in-strand insert bar (works for first and later beats).
  cy.get('body').then(($b) => {
    if ($b.find('[data-cy="insert-first"]').length) {
      cy.get('[data-cy="insert-first"]').click();
    } else {
      cy.get('[data-cy="insert-after"]').first().click({ force: true });
    }
  });
  cy.get('textarea.beat-textarea').should('be.visible').and('be.focused');
  cy.focused().type(text, { delay: 0 });
  cy.get('[data-cy="beat-save"]').click();
}

describe('Strand workbench UX', () => {
  before(() => {
    cy.ensureAuthenticated();
  });

  beforeEach(() => {
    const title = `UX Test ${Date.now()}`;
    seedStrand(title);
    addBeat('First beat. The neon bleeds through cracked concrete.');
    addBeat('Second beat. He took the long way home, like always.');
    cy.get('[data-cy="beat-row"]').should('have.length', 2);
  });

  it('shows positional Beat #001-style labels (1-based, zero-padded)', () => {
    cy.get('[data-cy="beat-id"]').should('have.length', 2);
    cy.get('[data-cy="beat-id"]').first().should('have.text', 'Beat #001');
    cy.get('[data-cy="beat-id"]').last().should('have.text', 'Beat #002');
  });

  // ── 3-row layout structural checks ───────────────────────────────────────

  it('every beat renders the three documented rows (header / body / footer)', () => {
    cy.get('[data-cy="beat-row"]').each(($card) => {
      cy.wrap($card).find('.beat-row-1').should('have.length', 1);
      cy.wrap($card).find('.beat-row-2').should('have.length', 1);
      cy.wrap($card).find('.beat-row-3').should('have.length', 1);
    });
  });

  it('Row 1 holds checkbox + beat id + format toolbar + right-aligned actions', () => {
    cy.get('[data-cy="beat-row"]').first().find('.beat-row-1').within(() => {
      cy.get('.beat-select-box-inline').should('exist');
      cy.get('[data-cy="beat-id"]').should('exist');
      cy.get('.beat-format-toolbar').should('exist');
      cy.get('[data-cy="beat-copy-text"]').should('exist');
      cy.get('[data-cy="beat-llm"]').should('exist');
      cy.get('[data-cy="beat-rerecord"]').should('exist');
      cy.get('[data-cy="beat-delete"]').should('exist');
    });
  });

  it('Row 2 holds the readonly prose (or textarea in edit mode)', () => {
    cy.get('[data-cy="beat-row"]').first().find('.beat-row-2 [data-cy="beat-prose"]').should('exist');
    // Switch to edit mode — textarea takes Row 2.
    cy.get('[data-cy="beat-row"]').first().find('[data-cy="beat-prose"]').click();
    cy.get('[data-cy="beat-row"]').first().find('.beat-row-2 textarea.beat-textarea').should('be.visible');
    cy.get('[data-cy="beat-row"]').first().find('[data-cy="beat-cancel"]').click();
  });

  it('Row 3 holds the gap-after editor on non-last beats', () => {
    // First beat (not last) — gap-after editor present in Row 3.
    cy.get('[data-cy="beat-row"]').first().find('.beat-row-3 .beat-gap-inline').should('exist');
    // Last beat — no gap-after editor.
    cy.get('[data-cy="beat-row"]').last().find('.beat-gap-inline').should('not.exist');
  });

  it('seamless edit toggle: prose and textarea share box dimensions', () => {
    // Capture the body width in read mode, then again in edit mode. Both
    // should be the same since .beat-prose and .beat-textarea share the
    // same box (no padding/border shift). One-pixel tolerance for sub-pixel
    // rendering.
    cy.get('[data-cy="beat-row"]').first().find('.beat-row-2').then(($row) => {
      const readWidth = $row[0].getBoundingClientRect().width;
      cy.get('[data-cy="beat-row"]').first().find('[data-cy="beat-prose"]').click();
      cy.get('[data-cy="beat-row"]').first().find('textarea.beat-textarea').should('be.visible');
      cy.get('[data-cy="beat-row"]').first().find('.beat-row-2').then(($row2) => {
        const editWidth = $row2[0].getBoundingClientRect().width;
        expect(Math.abs(editWidth - readWidth)).to.be.lessThan(1.5);
      });
      cy.get('[data-cy="beat-row"]').first().find('[data-cy="beat-cancel"]').click();
    });
  });

  // ── Format toolbar (Row 1) ───────────────────────────────────────────────

  it('format toolbar shows four icon-only buttons disabled in read mode', () => {
    cy.get('[data-cy="beat-row"]').first().within(() => {
      cy.get('[data-cy="beat-fmt-bold"]').should('exist').and('be.disabled');
      cy.get('[data-cy="beat-fmt-italic"]').should('exist').and('be.disabled');
      cy.get('[data-cy="beat-fmt-underline"]').should('exist').and('be.disabled');
      cy.get('[data-cy="beat-fmt-strike"]').should('exist').and('be.disabled');
    });
  });

  it('format toolbar enables in edit mode and wraps the selection with markers', () => {
    // Enter edit mode.
    cy.get('[data-cy="beat-row"]').first().find('[data-cy="beat-prose"]').click();
    cy.get('[data-cy="beat-row"]').first().find('textarea.beat-textarea').should('be.visible');

    // Buttons should now be enabled.
    cy.get('[data-cy="beat-row"]').first().within(() => {
      cy.get('[data-cy="beat-fmt-bold"]').should('not.be.disabled');
    });

    // Select all and apply bold; the resulting value wraps the whole text in **...**.
    cy.get('[data-cy="beat-row"]').first().find('textarea.beat-textarea').focus()
      .then(($ta) => {
        const ta = $ta[0];
        ta.setSelectionRange(0, ta.value.length);
      });
    cy.get('[data-cy="beat-row"]').first().find('[data-cy="beat-fmt-bold"]').click({ force: true });
    cy.get('[data-cy="beat-row"]').first().find('textarea.beat-textarea')
      .invoke('val').should('match', /^\*\*[\s\S]+\*\*$/);

    cy.get('[data-cy="beat-row"]').first().find('[data-cy="beat-cancel"]').click();
  });

  it('read view renders **bold** markdown and [WHISPERING] tone tag as emoji', () => {
    // Set the beat text to contain markers via the edit flow.
    cy.get('[data-cy="beat-row"]').first().find('[data-cy="beat-prose"]').click();
    cy.get('[data-cy="beat-row"]').first().find('textarea.beat-textarea')
      .focus().clear().type('She said it [WHISPERING] **quietly**.');
    cy.get('[data-cy="beat-row"]').first().find('[data-cy="beat-save"]').click();

    cy.get('[data-cy="beat-row"]').first().find('[data-cy="beat-prose"]', { timeout: TIMEOUT })
      .should('exist');
    cy.get('[data-cy="beat-row"]').first().find('[data-cy="beat-prose"] strong')
      .should('contain.text', 'quietly');
    cy.get('[data-cy="beat-row"]').first().find('[data-cy="beat-prose"] .tone-tag')
      .should('have.attr', 'title', '[WHISPERING]');
  });

  it('beat footer exposes the kept action buttons (text/LLM/mic/delete)', () => {
    cy.get('[data-cy="beat-row"]').first().within(() => {
      cy.get('[data-cy="beat-copy-text"]').should('exist');
      cy.get('[data-cy="beat-llm"]').should('exist');
      cy.get('[data-cy="beat-rerecord"]').should('exist');
      cy.get('[data-cy="beat-delete"]').should('exist');
      // Removed in the 2026-05-23 redesign.
      cy.get('[data-cy="beat-edit"]').should('not.exist');
      cy.get('[data-cy="beat-split"]').should('not.exist');
      cy.get('[data-cy="beat-join"]').should('not.exist');
      cy.get('[data-cy="beat-copy-id"]').should('not.exist');
    });
  });

  it('clicking the prose body opens the inline editor (no separate Edit button)', () => {
    cy.get('[data-cy="beat-row"]').first().find('[data-cy="beat-prose"]').click();
    cy.get('[data-cy="beat-row"]').first()
      .find('textarea.beat-textarea').should('be.visible')
      .and('contain.value', 'First beat.');
    cy.get('[data-cy="beat-row"]').first().find('[data-cy="beat-cancel"]').click();
  });

  it('Copy-text writes the beat prose to the clipboard', () => {
    cy.window().then((win) => {
      cy.stub(win.navigator.clipboard, 'writeText').as('clipboard').resolves();
    });
    cy.get('[data-cy="beat-row"]').first()
      .find('[data-cy="beat-copy-text"]').click({ force: true });
    cy.get('@clipboard').should('have.been.calledOnce');
    cy.get('@clipboard').its('firstCall.args.0').should('contain', 'First beat.');
  });

  // ── Inline gap-after editor (replaces the .beat-gap row between beats) ──

  it('every non-last beat has an inline gap-after editor in Row 3', () => {
    // Two beats → one editor (the second beat is the last; no trailing gap).
    cy.get('.beat-row-3 .beat-gap-inline').should('have.length', 1);
    cy.get('.beat-row-3 .beat-gap-input').should('have.length', 1);
    cy.get('.beat-row-3 .beat-gap-inline').should('contain.text', '(auto)');
  });

  it('saving a custom gap-after marks the beat custom and persists across reload', () => {
    cy.get('.beat-gap-input').first().focus().clear().type('1234');
    // The ✓ save button in the inline editor.
    cy.get('.beat-gap-inline').first().find('button').contains('✓').click({ force: true });
    cy.contains(/Gap after this beat . 1234 ms/i, { timeout: TIMEOUT }).should('exist');
    cy.get('.beat-gap-inline.custom', { timeout: TIMEOUT }).should('have.length', 1);
    cy.reload();
    cy.get('.beat-gap-inline.custom', { timeout: TIMEOUT }).should('have.length', 1);
  });

  it('reset clears the custom gap-after override', () => {
    cy.get('.beat-gap-input').first().focus().clear().type('1500');
    cy.get('.beat-gap-inline').first().find('button').contains('✓').click({ force: true });
    cy.get('.beat-gap-inline.custom', { timeout: TIMEOUT }).should('have.length', 1);
    // ↺ reset button only appears when a custom value is set.
    cy.get('.beat-gap-inline.custom').first().find('button').contains('↺').click({ force: true });
    cy.get('.beat-gap-inline.custom').should('not.exist');
    cy.contains(/Reset to auto pause/i).should('exist');
  });

  // ── Multi-select bar (unchanged behaviour, kept for regression coverage) ─

  it('multi-select checkbox toggles the selection bar and count', () => {
    cy.get('[data-cy="selection-bar"]').should('not.exist');
    cy.get('[data-cy="beat-row"]').first()
      .find('.beat-select-box-inline').check({ force: true });
    cy.get('[data-cy="selection-bar"]').should('be.visible');
    cy.get('[data-cy="selection-count"]').should('contain.text', '1 selected');
    cy.get('[data-cy="beat-row"]').eq(1)
      .find('.beat-select-box-inline').check({ force: true });
    cy.get('[data-cy="selection-count"]').should('contain.text', '2 selected');
  });

  it('Esc clears the selection when the bar is open', () => {
    cy.get('[data-cy="beat-row"]').first()
      .find('.beat-select-box-inline').check({ force: true });
    cy.get('[data-cy="selection-bar"]').should('be.visible');
    cy.get('body').type('{esc}');
    cy.get('[data-cy="selection-bar"]', { timeout: TIMEOUT }).should('not.exist');
  });

  // ── IsChapterStart + chapter divider + chapter index ────────────────────

  it('marking a beat as chapter-start renders a divider and adds an index entry', () => {
    // Mark the second beat as a chapter start with a heading via the meta panel.
    cy.get('[data-cy="beat-row"]').eq(1).find('[data-cy="beat-meta-toggle"]').click();
    cy.get('[data-cy="beat-row"]').eq(1).within(() => {
      // BeatTitle doubles as the chapter heading text.
      cy.get('input[placeholder*="The threshold"]').clear().type('2. The story continues');
      cy.get('[id$="-chapstart"]').check({ force: true });
      cy.get('[data-cy="beat-meta-save"]').click();
    });
    // After reload-on-save the divider is rendered above the second beat.
    cy.get('[data-cy="chapter-divider"]', { timeout: TIMEOUT }).should('have.length.at.least', 1);
    cy.get('[data-cy="chapter-divider"]').last().should('contain.text', '2. The story continues');
  });

  it('chapter index card lists every IsChapterStart beat once 2+ exist', () => {
    // Mark both beats as chapter starts so the index threshold (>=2) is hit.
    function markAsChapter(rowIdx, title) {
      cy.get('[data-cy="beat-row"]').eq(rowIdx).find('[data-cy="beat-meta-toggle"]').click();
      cy.get('[data-cy="beat-row"]').eq(rowIdx).within(() => {
        cy.get('input[placeholder*="The threshold"]').clear().type(title);
        cy.get('[id$="-chapstart"]').check({ force: true });
        cy.get('[data-cy="beat-meta-save"]').click();
      });
    }
    markAsChapter(0, '1. The thing that happened');
    markAsChapter(1, '2. The story continues');
    cy.get('.chapter-index', { timeout: TIMEOUT }).should('exist');
    cy.get('.chapter-index-link').should('have.length', 2);
    cy.get('.chapter-index-link').first().should('contain.text', '1. The thing that happened');
    cy.get('.chapter-index-link').last().should('contain.text', '2. The story continues');
    // Anchor target matches the beat row's id="beat-{guid:N}".
    cy.get('.chapter-index-link').first().invoke('attr', 'href').should('match', /^#beat-[0-9a-f]{32}$/);
  });

  // ── Beat.Kind dropdown + kind-aware prose render ────────────────────────

  it('changing Beat kind to quote renders the beat as a blockquote', () => {
    cy.get('[data-cy="beat-row"]').first().find('[data-cy="beat-meta-toggle"]').click();
    cy.get('[data-cy="beat-row"]').first().within(() => {
      cy.get('select').filter(':contains("book-title")').select('quote');
      // BeatTitle is the attribution for a quote beat.
      cy.get('input[placeholder*="The threshold"]').clear().type('Bill Coolman');
      cy.get('[data-cy="beat-meta-save"]').click();
    });
    cy.get('[data-cy="beat-row"]').first()
      .find('blockquote.beat-prose-quote', { timeout: TIMEOUT })
      .should('exist')
      .and('contain.text', 'First beat.');
    cy.get('[data-cy="beat-row"]').first()
      .find('.quote-attribution').should('contain.text', 'Bill Coolman');
  });

  // ── LLM bottom sheet ────────────────────────────────────────────────────

  it('clicking the ✨ LLM button opens the bottom sheet with the strand.beat handle', () => {
    cy.get('[data-cy="beat-row"]').first().find('[data-cy="beat-llm"]').click();
    cy.get('.llm-sheet', { timeout: TIMEOUT }).should('be.visible');
    cy.get('.llm-sheet').should('contain.text', 'Beat #1');
    // Handle format: <strand-guid>.<beat-guid>.
    cy.get('.llm-sheet-ref').invoke('text').should('match',
      /^[0-9a-f-]{36}\.[0-9a-f-]{36}$/);
    // Textarea is blank by default.
    cy.get('.llm-sheet textarea').should('have.value', '');
    // Close.
    cy.get('.llm-sheet button').contains(/✕|×/).click({ force: true });
    cy.get('.llm-sheet').should('not.exist');
  });

  it('LLM-sheet id button copies the strand.beat handle to the clipboard', () => {
    cy.window().then((win) => {
      cy.stub(win.navigator.clipboard, 'writeText').as('clipboard').resolves();
    });
    cy.get('[data-cy="beat-row"]').first().find('[data-cy="beat-llm"]').click();
    cy.get('.llm-sheet', { timeout: TIMEOUT }).should('be.visible');
    cy.get('.llm-sheet .beat-copy-btn').contains(/id/).click({ force: true });
    cy.get('@clipboard').should('have.been.calledOnce');
    cy.get('@clipboard').its('firstCall.args.0').should('match',
      /^[0-9a-f-]{36}\.[0-9a-f-]{36}$/);
  });

  it('selected beat card receives the .selected highlight class', () => {
    cy.get('[data-cy="beat-row"]').first()
      .find('.beat-select-box-inline').check({ force: true });
    cy.get('[data-cy="beat-row"]').first()
      .should('have.class', 'selected');
    cy.get('[data-cy="beat-row"]').first()
      .find('.beat-select-box-inline').uncheck({ force: true });
    cy.get('[data-cy="beat-row"]').first()
      .should('not.have.class', 'selected');
  });
});
