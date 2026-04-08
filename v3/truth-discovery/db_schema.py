"""
Database schema for the truth discovery pipeline.
SQLite database with tables for triples, clusters, truth scores, and flags.
"""
import sqlite3
import os
from dotenv import load_dotenv

load_dotenv()

DB_PATH = os.getenv("DB_PATH", "facts.db")


def get_connection():
    return sqlite3.connect(DB_PATH)


def init_db():
    """Create all tables if they don't exist."""
    conn = get_connection()
    c = conn.cursor()

    # Raw extracted triples from each source file
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

    # Cluster assignments after HDBSCAN
    c.execute("""
        CREATE TABLE IF NOT EXISTS clusters (
            cluster_id INTEGER PRIMARY KEY,
            representative_sentence TEXT,
            triple_count INTEGER DEFAULT 0,
            unique_sources INTEGER DEFAULT 0
        )
    """)

    # Consensus determined by consensus
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

    # Triples that disagree with ground truth
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

    # Processing log
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

    # Indexes for fast lookups
    c.execute("CREATE INDEX IF NOT EXISTS idx_triples_subject ON triples(subject)")
    c.execute("CREATE INDEX IF NOT EXISTS idx_triples_cluster ON triples(cluster_id)")
    c.execute("CREATE INDEX IF NOT EXISTS idx_triples_source ON triples(source_file)")
    c.execute("CREATE INDEX IF NOT EXISTS idx_truth_subject ON fact_scores(subject)")
    c.execute("CREATE INDEX IF NOT EXISTS idx_flagged_source ON flagged_triples(source_file)")

    conn.commit()
    conn.close()
    print(f"Database initialized at {DB_PATH}")


if __name__ == "__main__":
    init_db()
