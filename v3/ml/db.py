"""pyodbc connection factory for the StreetSamurai LocalDB."""
import pyodbc
import pandas as pd
from contextlib import contextmanager
from config import DB_CONN_STR


@contextmanager
def get_connection():
    """Context manager yielding a pyodbc connection. Commits on exit, rolls back on error."""
    conn = pyodbc.connect(DB_CONN_STR, autocommit=False)
    try:
        yield conn
        conn.commit()
    except Exception:
        conn.rollback()
        raise
    finally:
        conn.close()


def fetchdf(conn, sql: str, params=()) -> pd.DataFrame:
    """Execute SQL and return a pandas DataFrame."""
    cursor = conn.cursor()
    if params:
        cursor.execute(sql, params)
    else:
        cursor.execute(sql)
    cols = [col[0] for col in cursor.description]
    return pd.DataFrame.from_records(cursor.fetchall(), columns=cols)


def execute(conn, sql: str, params=()) -> None:
    """Execute SQL with optional params (no result set)."""
    cursor = conn.cursor()
    if params:
        cursor.execute(sql, params)
    else:
        cursor.execute(sql)


def fmt_ts(dt) -> str:
    """Format a datetime for use in FOR SYSTEM_TIME AS OF literals."""
    return dt.strftime("%Y-%m-%dT%H:%M:%S.%f")[:-3]  # trim to milliseconds
