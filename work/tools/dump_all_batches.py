import csv, sys
sys.stdout.reconfigure(encoding="utf-8")

csv_path = r"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\output\switch_untranslated_subtitles.csv"
with open(csv_path, "r", encoding="utf-8-sig") as f:
    r = list(csv.reader(f))

for i in range(1, len(r), 25):
    print(f"--- BATCH {i} to {min(len(r)-1, i+24)} ---")
    for row in r[i:min(len(r), i+25)]:
        print(f"{row[0]}\t{row[1]}")
