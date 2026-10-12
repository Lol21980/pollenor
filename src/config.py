# config.py - все пути и настройки в одном месте,
# чтобы не разбрасывать их по коду
from pathlib import Path

# корень проекта (папка, где лежит data, src и т.д.)
ROOT = Path(__file__).resolve().parents[1]

DATA_RAW = ROOT / "data" / "raw"
DATA_INTERIM = ROOT / "data" / "interim"
DATA_PROCESSED = ROOT / "data" / "processed"

# наши три исходных файла
RAW_FILES = [
    DATA_RAW / "1_датасет_население_возраст_и_пол.csv",
    DATA_RAW / "2_датасет_рождаемость_смертность_урбанизация.csv",
    DATA_RAW / "3_датасет_миграция_фертильность_брачность.csv",
]

# перевод названий столбцов на английский, так удобнее
COLUMN_MAP = {
    "Страна": "country",
    "Код страны": "country_code",
    "Регион": "region",
    "Группа дохода": "income_group",
    "Код показателя": "indicator_code",
    "Название показателя": "indicator_name",
    "Тема показателя": "topic",
    "Определение показателя": "definition",
    "Год": "year",
    "Значение": "value",
    "Сноска": "footnote",
}

# какие типы ставим каждому столбцу (чтобы меньше памяти занимало)
SCHEMA = {
    "country": "category",
    "country_code": "category",
    "region": "category",
    "income_group": "category",
    "indicator_code": "category",
    "indicator_name": "category",
    "topic": "category",
    "definition": "string",
    "year": "int16",
    "value": "float32",
}

# месяцы для починки испорченных эксель значений ("мар.73" -> "3.73")
MONTHS = {
    "янв": 1, "фев": 2, "мар": 3, "апр": 4, "май": 5, "июн": 6,
    "июл": 7, "авг": 8, "сен": 9, "окт": 10, "ноя": 11, "дек": 12,
}
