const { defineConfig } = require('cypress');

module.exports = defineConfig({
  e2e: {
    baseUrl: process.env.CYPRESS_BASE_URL || 'http://localhost:5101',
    specPattern: 'cypress/e2e/**/*.cy.js',
    supportFile: 'cypress/support/e2e.js',
    video: false,
    screenshotOnRunFailure: true,
    defaultCommandTimeout: 15000,
    // Some dictionary pages (characters, synthetics) deserialize 1k+ records
    // from Records.Json on first hit. Cold-cache load can take ~50s; warm hits
    // are sub-second. These budgets cover the first-visit case.
    // /dashboard cold-cache iterates Graph.AllNodes() × GetAllEdges(n) — ~90 s
    // first hit. Subsequent visits are sub-second. Budget covers the cold case.
    pageLoadTimeout: 180000,
    responseTimeout: 180000,
    requestTimeout: 30000,
    viewportWidth: 1600,
    viewportHeight: 900,
    chromeWebSecurity: false,
  },
});
