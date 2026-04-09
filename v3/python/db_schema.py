"""
Database schema for the truth discovery pipeline.
SQLite database with tables for triples, clusters, truth scores, and flags.

This file defines the STRUCTURE of the database -- think of it like a blueprint.
When you run it, it creates a SQLite file (facts.db) with 5 tables that store
all the data the pipeline produces as it moves through each phase.

The data flows through these tables like this:
  1. triples      -- raw claims extracted from JSON files ("X is made by Y")
  2. clusters     -- groups of triples that say the same thing differently
  3. fact_scores  -- the "voted-on" truth for each group of claims
  4. flagged_triples -- claims that disagree with what the majority says
  5. processing_log  -- a diary of what the pipeline did and when
"""

import sqlite3  # Python's built-in library for working with SQLite databases (no server needed, just a file)
import os       # Provides access to operating system features like reading environment variables

# load_dotenv() reads a file called ".env" in the current directory and loads
# its key=value pairs into environment variables, so we can keep secrets
# (like API keys and file paths) out of the code itself
from dotenv import load_dotenv

# Actually read the .env file and set the environment variables
load_dotenv()

# Look for a DB_PATH variable in the environment (or .env file).
# If it's not set, default to "facts.db" in the current directory.
# This is where all pipeline data gets stored -- a single file on disk.
DB_PATH = os.getenv("DB_PATH", "facts.db")


def get_connection():
    # Open (or create) the SQLite database file and return a connection object.
    # A "connection" is like opening a phone line to the database -- you can
    # send queries through it and get results back.
    return sqlite3.connect(DB_PATH)


def init_db():
    """Create all tables if they don't exist."""

    # Open a connection to the database
    conn = get_connection()

    # A "cursor" is the object you use to actually run SQL commands.
    # Think of the connection as the phone line, and the cursor as the person
    # on the other end who executes your instructions.
    c = conn.cursor()

    # TABLE 1: triples
    # This is where Phase 1 (extract.py) stores its output.
    # Each row is one atomic claim like "Hearthstone HM-7 is manufactured by Hearthstone Firearms"
    # broken into three parts: subject ("Hearthstone HM-7"), predicate ("manufactured_by"),
    # and object ("Hearthstone Firearms") -- this format is called an SPO triple.
    #
    # CREATE TABLE IF NOT EXISTS means: only create this table if it doesn't already exist.
    # This makes the script safe to run multiple times without losing data.
    c.execute("""
        CREATE TABLE IF NOT EXISTS triples (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            source_file TEXT NOT NULL,
            source_repo TEXT NOT NULL,
            entity_name TEXT NOT NULL,
            subject TEXT NOT NULL,
            predicate TEXT NOT NULL,
            object TEXT NOT NULL,
            full_sentence TEXT NOT NULL,
            embedding BLOB,
            cluster_id INTEGER DEFAULT -1,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
        )
    """)
    # Column breakdown:
    #   id              -- auto-incrementing unique number for each triple
    #   source_file     -- the JSON file this claim was extracted from (for traceability)
    #   source_repo     -- which subfolder/category the file came from (e.g., "weapons", "places")
    #   entity_name     -- the name of the entity (e.g., "Hearthstone HM-7")
    #   subject         -- the thing being described ("Hearthstone HM-7")
    #   predicate       -- the relationship ("manufactured_by")
    #   object          -- the value or target ("Hearthstone Firearms")
    #   full_sentence   -- the human-readable version ("Hearthstone HM-7 is manufactured by...")
    #   embedding       -- a BLOB (Binary Large Object) storing the 384-number vector from Phase 2
    #                      stored as raw bytes because SQLite doesn't have an array type
    #   cluster_id      -- which cluster this triple belongs to (set in Phase 3, -1 = not yet clustered)
    #   created_at      -- timestamp of when this row was inserted (auto-filled by SQLite)

    # TABLE 2: clusters
    # This is where Phase 3 (cluster.py) stores its output.
    # Each row represents a GROUP of triples that all say roughly the same thing.
    # For example, "treats headaches" and "helps with headaches" would end up in the same cluster.
    c.execute("""
        CREATE TABLE IF NOT EXISTS clusters (
            cluster_id INTEGER PRIMARY KEY,
            representative_sentence TEXT,
            triple_count INTEGER DEFAULT 0,
            unique_sources INTEGER DEFAULT 0
        )
    """)
    # Column breakdown:
    #   cluster_id              -- the cluster number assigned by HDBSCAN
    #   representative_sentence -- one example sentence from this cluster (for human readability)
    #   triple_count            -- how many triples are in this cluster
    #   unique_sources          -- how many DIFFERENT source files contributed triples to this cluster
    #                              (more sources agreeing = more trustworthy)

    # TABLE 3: fact_scores
    # This is where Phase 4 (score.py) stores its output.
    # After clustering, the pipeline "votes" within each cluster to decide what's true.
    # The most common answer wins, and the confidence is how many sources agreed.
    c.execute("""
        CREATE TABLE IF NOT EXISTS fact_scores (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            cluster_id INTEGER NOT NULL,
            subject TEXT NOT NULL,
            predicate TEXT NOT NULL,
            consensus_object TEXT NOT NULL,
            confidence REAL NOT NULL,
            agreeing_sources INTEGER NOT NULL,
            dissenting_sources INTEGER NOT NULL,
            total_sources INTEGER NOT NULL,
            FOREIGN KEY (cluster_id) REFERENCES clusters(cluster_id)
        )
    """)
    # Column breakdown:
    #   cluster_id        -- which cluster this score is for (links back to clusters table)
    #   subject           -- e.g., "Hearthstone HM-7"
    #   predicate         -- e.g., "manufactured_by"
    #   consensus_object  -- the winning answer that most sources agreed on (e.g., "Hearthstone Firearms")
    #   confidence        -- a number from 0.0 to 1.0 (agreeing / total). 1.0 = everyone agrees.
    #   agreeing_sources  -- how many sources said the consensus answer
    #   dissenting_sources -- how many sources said something different
    #   total_sources     -- agreeing + dissenting
    #   FOREIGN KEY       -- tells SQLite that cluster_id must reference a real row in the clusters table
    #                        (this is a "relationship" between tables, enforcing data integrity)

    # TABLE 4: flagged_triples
    # Also populated in Phase 4 (score.py).
    # Any triple that DISAGREES with the consensus gets flagged here.
    # These are potential errors in the source data that need fixing.
    c.execute("""
        CREATE TABLE IF NOT EXISTS flagged_triples (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            triple_id INTEGER NOT NULL,
            source_file TEXT NOT NULL,
            entity_name TEXT NOT NULL,
            subject TEXT NOT NULL,
            predicate TEXT NOT NULL,
            incorrect_object TEXT NOT NULL,
            correct_object TEXT NOT NULL,
            confidence REAL NOT NULL,
            repaired INTEGER DEFAULT 0,
            FOREIGN KEY (triple_id) REFERENCES triples(id)
        )
    """)
    # Column breakdown:
    #   triple_id        -- which triple in the triples table is wrong (links back)
    #   source_file      -- the file that contains the error (so repair.py knows what to fix)
    #   entity_name      -- the entity with the error
    #   subject/predicate -- what the claim is about
    #   incorrect_object -- what the source file currently says (the "wrong" answer)
    #   correct_object   -- what the consensus says it SHOULD be (the "right" answer)
    #   confidence       -- how confident we are in the correction (higher = more sources agree)
    #   repaired         -- 0 = not yet fixed, 1 = repair.py has already corrected this in the source file

    # TABLE 5: processing_log
    # A journal of pipeline activity. Each phase writes a row when it finishes.
    # Useful for debugging and knowing what has already been run.
    c.execute("""
        CREATE TABLE IF NOT EXISTS processing_log (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            phase TEXT NOT NULL,
            status TEXT NOT NULL,
            files_processed INTEGER DEFAULT 0,
            triples_extracted INTEGER DEFAULT 0,
            message TEXT,
            timestamp TIMESTAMP DEFAULT CURRENT_TIMESTAMP
        )
    """)
    # Column breakdown:
    #   phase             -- which pipeline step ran (e.g., "extraction", "embedding", "clustering")
    #   status            -- "complete", "error", etc.
    #   files_processed   -- how many files were handled
    #   triples_extracted -- how many triples were produced
    #   message           -- a human-readable summary of what happened
    #   timestamp         -- when this log entry was created (auto-filled)

    # INDEXES: These make database lookups much faster.
    # Without an index, SQLite has to scan every single row to find what you're looking for
    # (like searching a phonebook page by page). With an index, it can jump directly to the
    # right spot (like using the alphabetical tabs on the side of a phonebook).
    # The tradeoff: indexes use extra disk space and slow down inserts slightly.
    c.execute("CREATE INDEX IF NOT EXISTS idx_triples_subject ON triples(subject)")
    c.execute("CREATE INDEX IF NOT EXISTS idx_triples_cluster ON triples(cluster_id)")
    c.execute("CREATE INDEX IF NOT EXISTS idx_triples_source ON triples(source_file)")
    c.execute("CREATE INDEX IF NOT EXISTS idx_truth_subject ON fact_scores(subject)")
    c.execute("CREATE INDEX IF NOT EXISTS idx_flagged_source ON flagged_triples(source_file)")

    # conn.commit() saves all the changes we just made to the database file on disk.
    # Without this, all the CREATE TABLE statements would be lost when the connection closes.
    # SQLite uses "transactions" -- changes are temporary until you commit them.
    conn.commit()

    # Close the database connection to free up resources.
    # Always close connections when you're done, like hanging up the phone.
    conn.close()

    # Let the user know the database is ready
    print(f"Database initialized at {DB_PATH}")


# This block only runs when you execute this file directly (python db_schema.py),
# NOT when another file imports it (like "from db_schema import init_db").
# The special variable __name__ is set to "__main__" only when the file is run directly.
if __name__ == "__main__":
    init_db()
