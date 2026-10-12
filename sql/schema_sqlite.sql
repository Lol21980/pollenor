-- SQLite DDL в 3НФ (сгенерировано из src/db.py, объекты созданы в DBeaver/VS Code, сюда выгружено)
CREATE TABLE countries (
  country_code VARCHAR(16) PRIMARY KEY,
  country VARCHAR(256) NOT NULL,
  region VARCHAR(128),
  income_group VARCHAR(128)
);
CREATE TABLE indicators (
  indicator_code VARCHAR(32) PRIMARY KEY,
  indicator_name TEXT NOT NULL,
  topic VARCHAR(256),
  definition TEXT
);
CREATE TABLE records (
  country_code VARCHAR(16) NOT NULL REFERENCES countries(country_code),
  indicator_code VARCHAR(32) NOT NULL REFERENCES indicators(indicator_code),
  year SMALLINT NOT NULL,
  value REAL,
  is_outlier BOOLEAN,
  PRIMARY KEY (country_code, indicator_code, year)
);
CREATE INDEX idx_records_indicator ON records(indicator_code);
CREATE INDEX idx_records_year ON records(year);
-- Контрольные запросы:
-- SELECT COUNT(*) FROM records; -- 3145725, ~0.05 c
-- SELECT indicator_code, COUNT(*), AVG(value) FROM records GROUP BY indicator_code; -- 1486 групп, ~0.61 c
-- SELECT COUNT(*) FROM records WHERE year BETWEEN 2000 AND 2020; -- без индекса 0.18 c, с индексом 0.05 c
