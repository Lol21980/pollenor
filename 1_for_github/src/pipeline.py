# pipeline.py - главный скрипт, запускает все по очереди:
# raw -> interim -> processed
# запуск: python -m src.pipeline
# на срезе: python -m src.pipeline --sample
import argparse
import pandas as pd
from .config import DATA_RAW, DATA_INTERIM, DATA_PROCESSED, RAW_FILES
from .cleaning import clean
from .io_utils import make_sample


def main():
    parser = argparse.ArgumentParser(description="ЛР1: обработка данных")
    parser.add_argument("--sample", action="store_true", help="работать на срезе 200к")
    args = parser.parse_args()

    DATA_INTERIM.mkdir(parents=True, exist_ok=True)
    DATA_PROCESSED.mkdir(parents=True, exist_ok=True)

    if args.sample:
        # делаем срез 200 тысяч строк
        sample_path = DATA_INTERIM / "sample_200k.csv"
        if not sample_path.exists():
            n = make_sample([p for p in RAW_FILES if p.exists()], sample_path)
            print("срез готов, строк:", n)
        inputs = [sample_path]
        suffix = "_sample"
    else:
        inputs = [p for p in RAW_FILES if p.exists()]
        suffix = ""

    # шаг 1: склеиваем файлы и сохраняем в parquet
    parts = []
    for p in inputs:
        print("читаю", p.name)
        parts.append(pd.read_csv(p, sep=";", encoding="utf-8-sig", engine="python"))
    df_raw = pd.concat(parts, ignore_index=True)
    print("всего строк после склейки:", len(df_raw))
    df_raw.to_parquet(DATA_INTERIM / ("source" + suffix + ".parquet"), index=False)

    # шаг 2: чистим
    df_raw = pd.read_parquet(DATA_INTERIM / ("source" + suffix + ".parquet"))
    df = clean(df_raw)
    out = DATA_PROCESSED / ("clean" + suffix + ".parquet")
    df.to_parquet(out, index=False)
    print("готово, сохранил в", out, "строк:", len(df))


if __name__ == "__main__":
    main()
