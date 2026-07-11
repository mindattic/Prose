"""Pull current enabled beat texts into a Parquet cache for register classifier training."""
import sys
import os
sys.path.insert(0, os.path.dirname(os.path.dirname(__file__)))

import pandas as pd
from rich.console import Console
from db import get_connection, fetchdf
from config import BEATS_CACHE_PATH

console = Console()

BEATS_SQL = """
SELECT
    n.Slug AS StrandSlug,
    ROW_NUMBER() OVER (PARTITION BY bn.NodeId ORDER BY bn.SortKey) AS BeatNumber,
    CONVERT(nvarchar(36), b.Id) AS BeatId,
    b.Text AS BeatText
FROM BeatNodes bn
JOIN Beats b  ON b.Id  = bn.BeatId
JOIN Nodes n  ON n.Id  = bn.NodeId
WHERE bn.IsEnabled = 1
  AND b.Text IS NOT NULL
  AND LEN(TRIM(b.Text)) > 100
"""


def run() -> pd.DataFrame:
    with get_connection() as conn:
        df = fetchdf(conn, BEATS_SQL)
    console.print(f"[green]{len(df):,} beats from {df['StrandSlug'].nunique()} nodes[/green]")
    df.to_parquet(BEATS_CACHE_PATH, index=False)
    return df


if __name__ == "__main__":
    run()
