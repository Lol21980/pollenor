# io_utils.py - чтение файлов и замеры времени
import csv
import time
import pandas as pd


# читает первые n строк файла (чтобы глянуть структуру)
def read_head(path, n=5):
    return pd.read_csv(path, sep=";", encoding="utf-8-sig", nrows=n, engine="python")


# делает срез на n строк из нескольких файлов подряд
# читаем построчно, чтобы память не забивать
def make_sample(input_paths, output_path, n=200000):
    written = 0
    first = True
    out = open(output_path, "w", encoding="utf-8", newline="")
    writer = None
    for fp in input_paths:
        if written >= n:
            break
        with open(fp, encoding="utf-8-sig", newline="") as f:
            reader = csv.DictReader(f, delimiter=";")
            if first:
                writer = csv.DictWriter(out, fieldnames=reader.fieldnames, delimiter=";")
                writer.writeheader()
                first = False
            for row in reader:
                if written >= n:
                    break
                writer.writerow(row)
                written += 1
    out.close()
    return written


# замер времени: запускаем 4 раза, первый не считаем, берем среднее из 3
def measure_time(func, repeats=3):
    times = []
    for i in range(repeats + 1):
        t0 = time.perf_counter()
        func()
        t1 = time.perf_counter()
        if i > 0:
            times.append(t1 - t0)
    return sum(times) / len(times)


# сколько памяти занимает таблица, в мегабайтах
def memory_mb(df):
    return float(df.memory_usage(deep=True).sum() / 1024 / 1024)
