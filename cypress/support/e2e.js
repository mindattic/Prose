// Cypress support entry — loaded before every spec.
// Wires custom commands (cy.login, cy.ensureAuthenticated) into the Cypress
// namespace. Pulled in via `supportFile: 'cypress/support/e2e.js'` in
// cypress.config.js.

import './commands';
