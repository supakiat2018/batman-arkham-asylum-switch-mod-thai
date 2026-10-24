import csv
import sys
import os

sys.stdout.reconfigure(encoding="utf-8")

csv_path = r"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\output\switch_untranslated_subtitles.csv"
rows = []
with open(csv_path, "r", encoding="utf-8-sig") as f:
    reader = csv.reader(f)
    header = next(reader)
    for r in reader:
        rows.append(r)

print(f"Total untranslated rows: {len(rows)}")
for r in rows[:20]:
    print(f"[{r[0]}] ({r[3]}) {r[4]}: {r[1]}")
