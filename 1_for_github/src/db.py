# db.py - загрузка почищенных данных в SQLite
# схема в 3-й нормальной форме: страны и показатели отдельно,
# в главной таблице только ключи и значения
import sqlite3
import time

# запросы для создания таблиц
CREATE_TABLES = """
CREATE TABLE IF NOT EXISTS countries (
  country_code TEXT PRIMARY KEY,
  country TEXT NOT NULL,
  region TEXT,
  income_group TEXT
);
CREATE TABLE IF NOT EXISTS indicators (
  indicator_code TEXT PRIMARY KEY,
  indicator_name TEXT NOT NULL,
  topic TEXT,
  definition TEXT
);
CREATE TABLE IF NOT EXISTS records (
  country_code TEXT NOT NULL,
  indicator_code TEXT NOT NULL,
  year INTEGER NOT NULL,
  value REAL,
  is_outlier INTEGER,
  PRIMARY KEY (country_code, indicator_code, year)
);
CREATE INDEX IF NOT EXISTS idx_records_indicator ON records(indicator_code);
CREATE INDEX IF NOT EXISTS idx_records_year ON records(year);
"""


# режем одну широкую таблицу на три (убираем повторы)
def split_tables(df):
    countries = df[["country_code", "country", "region", "income_group"]].drop_duplicates("country_code")
    indicators = df[["indicator_code", "indicator_name", "topic", "definition"]].drop_duplicates("indicator_code")
    cols = ["country_code", "indicator_code", "year", "value", "is_outlier"]
    records = df[[c for c in cols if c in df.columns]].copy()
    return countries, indicators, records


# грузим все в sqlite, возвращаем время в секундах
def load_sqlite(df, db_path):
    if db_path.exists():
        db_path.unlink()  # удаляем старую базу, чтобы не было дублей
    con = sqlite3.connect(db_path)
    con.executescript(CREATE_TABLES)
    countries, indicators, records = split_tables(df)
    t0 = time.perf_counter()
    countries.to_sql("countries", con, if_exists="append", index=False)
    indicators.to_sql("indicators", con, if_exists="append", index=False)
    # главную таблицу льем кусками, а то памяти не хватит
    for i in range(0, len(records), 50000):
        records.iloc[i:i + 50000].to_sql("records", con, if_exists="append", index=False)
    con.commit()
    t = time.perf_counter() - t0
    con.close()
    return t
