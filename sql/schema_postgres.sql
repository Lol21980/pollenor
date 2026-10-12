-- PostgreSQL DDL в 3НФ (та же нормализованная схема; типы SERVER)
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
-- Загрузка (после справочников!):
-- пакетно: python -m src.db (to_sql chunksize=50000, method='multi')
-- потоково COPY (быстрее в разы):
-- \COPY countries FROM 'countries.csv' CSV HEADER
-- \COPY indicators FROM 'indicators.csv' CSV HEADER
-- \COPY records FROM 'records.csv' CSV HEADER
-- Обоснование 3НФ: region/income зависят только от country_code (вынесены в countries);
-- indicator_name/topic/definition зависят только от indicator_code (вынесены в indicators);
-- в records остались только ключ (country_code, indicator_code, year) + факты (value, is_outlier).
-- 1НФ: атомарные значения; 2НФ: ключ составной, факты зависят от всего ключа; 3НФ: транзитивных зависимостей нет.
