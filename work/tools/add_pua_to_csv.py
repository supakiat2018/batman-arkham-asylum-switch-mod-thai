import csv
import json
import os
import sys

sys.stdout.reconfigure(encoding="utf-8")

mapping_path = r"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\Mapping.json"
with open(mapping_path, "r", encoding="utf-8") as f:
    mapping = json.load(f)

max_len = max(len(k) for k in mapping.keys())

def convert_to_pua(text: str) -> str:
    if not text: return text
    res = []
    i = 0
    n = len(text)
    while i < n:
        matched = False
        for l in range(min(max_len, n - i), 0, -1):
            sub = text[i:i+l]
            if sub in mapping:
                res.append(mapping[sub])
                i += l
                matched = True
                break
        if not matched:
            res.append(text[i])
            i += 1
    return "".join(res)

in_csv = r"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\switch_untranslated_subtitles.csv"
rows = []
with open(in_csv, "r", encoding="utf-8-sig") as f:
    r = csv.reader(f)
    header = next(r)
    # add PUA column
    header.append("Thai Translation PUA (แปลงรหัส PUA)")
    for row in r:
        th = row[5]
        pua = convert_to_pua(th)
        row.append(pua)
        rows.append(row)

out_paths = [
    r"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\switch_untranslated_subtitles.csv",
    r"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\modthai\0100E870163CA000\switch_untranslated_subtitles.csv",
    r"C:\Users\Supakiat\.gemini\antigravity\brain\4d9b1845-8621-48f6-b3e7-fd17e325d2ca\switch_untranslated_subtitles.csv"
]

for p in out_paths:
    with open(p, "w", encoding="utf-8-sig", newline="") as f:
        w = csv.writer(f)
        w.writerow(header)
        w.writerows(rows)

print(f"Updated CSV with both Standard Thai and PUA Thai ({len(rows)} rows)!")
