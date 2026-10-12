# cleaning.py - тут все функции очистки данных
# каждая функция берет таблицу и возвращает починенную таблицу
import re
import pandas as pd
from .config import COLUMN_MAP, MONTHS, SCHEMA

# скрытые пропуски, которые надо заменить на NaN
HIDDEN_NA = ["-", "—", "–", "нет данных", "н/д", "N/A", "NA", "NULL", "null", "None"]


# 1. переименовываем столбцы на английский
def rename_columns(df):
    return df.rename(columns=COLUMN_MAP)


# 2. убираем пробелы по краям во всех текстовых столбцах
def strip_strings(df):
    for c in df.select_dtypes(include="object").columns:
        df[c] = df[c].astype("string").str.strip()
    return df


# 3. чиним значения, которые эксель превратил в даты
# например "мар.73" это на самом деле число 3.73
def fix_excel_dates(s):
    def fix_one(v):
        if not isinstance(v, str):
            return v
        v = v.strip().lower()
        # проверяем, похоже ли на "мес.число"
        parts = v.split(".")
        if len(parts) == 2 and parts[0][:3] in MONTHS:
            return str(MONTHS[parts[0][:3]]) + "." + parts[1]
        return v
    return s.map(fix_one)


# 4. столбец value переводим в числа
def coerce_value(df):
    s = df["value"].astype("string")
    s = fix_excel_dates(s)  # сначала чиним даты
    s = s.str.replace(",", ".", regex=False)  # запятые в точки
    s = s.str.replace(" ", "", regex=False)
    for bad in HIDDEN_NA:  # скрытые пропуски в NaN
        s = s.mask(s == bad)
    s = s.mask(s.str.strip() == "")
    df["value"] = pd.to_numeric(s, errors="coerce").astype("float32")
    return df


# 5. год в число
def coerce_year(df):
    df["year"] = pd.to_numeric(df["year"], errors="coerce").astype("Int16")
    return df


# 6. категории приводим в порядок (пробелы, пустые в NaN)
def normalize_categories(df):
    for c in ["region", "income_group", "topic", "country", "country_code", "indicator_code", "indicator_name"]:
        if c in df.columns:
            df[c] = df[c].astype("string").str.strip()
            df[c] = df[c].mask(df[c] == "")
    for c in ["region", "income_group", "topic", "country", "country_code", "indicator_code", "indicator_name"]:
        if c in df.columns:
            df[c] = df[c].astype("category")
    return df


# 7. чистим текст (убираем лишние пробелы)
def clean_text(df):
    for c in ["definition", "footnote"]:
        if c in df.columns:
            df[c] = df[c].astype("string").str.replace(r"\s+", " ", regex=True).str.strip()
    return df


# 8. удаляем полностью одинаковые строки
def drop_full_duplicates(df):
    before = len(df)
    df = df.drop_duplicates()
    print("полных дубликатов удалено:", before - len(df))
    return df


# 9. удаляем дубликаты по ключу (страна + показатель + год), оставляем последнюю
def drop_key_duplicates(df):
    key = ["country_code", "indicator_code", "year"]
    before = len(df)
    df = df.sort_values(key).drop_duplicates(subset=key, keep="last")
    print("дубликатов по ключу удалено:", before - len(df))
    return df


# 10. ищем выбросы через квартили, но не удаляем, а помечаем флажком
def mark_outliers(df):
    q1 = df["value"].quantile(0.25)
    q3 = df["value"].quantile(0.75)
    iqr = q3 - q1
    lo = q1 - 1.5 * iqr
    hi = q3 + 1.5 * iqr
    flag = (df["value"] < lo) | (df["value"] > hi)
    df["is_outlier"] = flag.fillna(False).astype("boolean")
    print("выбросов помечено:", int(flag.sum()), "границы:", round(float(lo), 2), "-", round(float(hi), 2))
    return df


# 11. ставим экономные типы + удаляем сноску (она почти везде пустая)
def apply_schema(df):
    for c, t in SCHEMA.items():
        if c in df.columns:
            df[c] = df[c].astype(t)
    if "footnote" in df.columns:
        print("сноска пуста на", round(float(df["footnote"].isna().mean()) * 100, 1), "% - удаляю столбец")
        df = df.drop(columns=["footnote"])
    return df


# главная функция - прогоняет все шаги по очереди
def clean(df):
    print("строк на входе:", len(df))
    df = rename_columns(df)
    df = strip_strings(df)
    df = normalize_categories(df)
    df = clean_text(df)
    df = coerce_value(df)
    df = coerce_year(df)
    df = drop_full_duplicates(df)
    df = drop_key_duplicates(df)
    df = mark_outliers(df)
    df = apply_schema(df)
    print("строк на выходе:", len(df))
    return df
