# ЛР1 - обработка и очистка данных

Делал по методичке. Данные World Bank, 3 файла, 3 млн строк.

## Что нужно

- Python 3.12
- VS Code с расширениями Python, Jupyter

## Как запустить

```powershell
py -3.12 -m venv .venv
.\.venv\Scripts\Activate.ps1
pip install -r requirements.txt
```

Сначала лучше на срезе (200 тысяч строк), а потом на полном:

```powershell
python -m src.pipeline --sample
python -m src.pipeline
```

Блокноты лежат в `notebooks/`, открывать в VS Code.

## Что где

- `data/raw/` - исходные файлы, их не трогаем
- `data/interim/` - промежуточное (склеенный parquet, срез)
- `data/processed/` - готовое (clean.parquet и dataset.db)
- `src/` - код: config (пути), cleaning (очистка), db (загрузка в базу), pipeline (запуск всего)
- `sql/` - схемы таблиц для SQLite и Postgres
- `docs/` - отчет и картинки

## Что получилось

- Parquet вышел в 48 раз меньше чем CSV, поэтому его и выбрал
- Типы поменял (категории, int16, float32) - памяти стало в 2 раза меньше
- Polars быстрее pandas на фильтре и сортировке
- Строк было 3145725, стало 3145725 - ничего не потерял
- База SQLite: 3 таблицы (страны, показатели, записи), грузится 23 сек, индекс ускоряет выборку в 3-4 раза
