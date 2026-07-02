"""Pull reviewer gripe texts from StrandReviews into a Parquet cache."""
import sys
import os
sys.path.insert(0, os.path.dirname(os.path.dirname(__file__)))

import pandas as pd
from rich.console import Console
from db import get_connection, fetchdf
from config import GRIPES_CACHE_PATH

console = Console()

GRIPES_SQL = """
SELECT
    s.Slug       AS StrandSlug,
    sr.ContentHash,
    sr.ReviewedAt,
    sr.PersonaId,
    sr.Score,
    sr.Improvements AS GripeText,
    sr.ClusterLabel
FROM StrandReviews sr
JOIN Strands s ON s.Id = sr.StrandId
WHERE s.IsWIP = 0
  AND sr.Improvements IS NOT NULL
  AND LEN(TRIM(sr.Improvements)) > 10
"""


def run() -> pd.DataFrame:
    with get_connection() as conn:
        df = fetchdf(conn, GRIPES_SQL)
    console.print(f"[green]{len(df):,} gripe rows from {df['StrandSlug'].nunique()} strands[/green]")
    df.to_parquet(GRIPES_CACHE_PATH, index=False)
    return df


if __name__ == "__main__":
    run()
