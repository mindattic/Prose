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
    n.Slug       AS StrandSlug,
    nr.ContentHash,
    nr.ReviewedAt,
    nr.PersonaId,
    nr.Score,
    nr.Improvements AS GripeText,
    nr.ClusterLabel
FROM NodeReviews nr
JOIN Nodes n ON n.Id = nr.NodeId
WHERE nr.Improvements IS NOT NULL
  AND LEN(TRIM(nr.Improvements)) > 10
"""


def run() -> pd.DataFrame:
    with get_connection() as conn:
        df = fetchdf(conn, GRIPES_SQL)
    console.print(f"[green]{len(df):,} gripe rows from {df['StrandSlug'].nunique()} nodes[/green]")
    df.to_parquet(GRIPES_CACHE_PATH, index=False)
    return df


if __name__ == "__main__":
    run()
