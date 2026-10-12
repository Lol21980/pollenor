# Задание в Google Colab (шаг 8)

1. Захожу на https://colab.research.google.com, создаю новый блокнот.
2. В первой ячейке смотрю версию питона:
```python
import sys
print(sys.version)
```
У Colab версия своя (не 3.12), это надо написать в отчет.
3. Ставлю библиотеки:
```python
!pip install pandas polars
import pandas, polars
print(pandas.__version__, polars.__version__)
```
4. Загружаю файл `sample_200k.csv` (через кнопку Files -> Upload).
5. Читаю и смотрю:
```python
import pandas as pd
df = pd.read_csv('sample_200k.csv', sep=';', encoding='utf-8-sig', engine='python')
print(df.shape)
print(df.dtypes)
print((df.isna().mean() * 100).round(2))
```
6. Строю график (записей по годам):
```python
import matplotlib.pyplot as plt
df['Год'].value_counts().sort_index().plot()
plt.title('Записей по годам')
plt.show()
```
7. К каждой ячейке добавляю текстовую ячейку с пояснением.
8. Нажимаю Share -> Anyone with the link, ссылку вставляю в отчет.
9. File -> Save a copy in GitHub, чтобы копия сохранилась в репозиторий.

Важно: Colab сессия сбрасывается, файлы слетают, postgres там не поднять,
поэтому это только для experiments, а сдаю репозиторий.
