import csv
import os

in_csv = r"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\switch_untranslated_subtitles.csv"
pkgs = set()
with open(in_csv, "r", encoding="utf-8-sig") as f:
    r = csv.reader(f)
    next(r)
    for row in r:
        for p in row[3].split("; "):
            pkgs.add(p.strip())

print("Packages needing Joker DLC injection:")
mod_dir = r"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\modthai\0100E870163CA000\romfs\BmGame\CookedSwitch"
sw_dir = r"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\romfs\BmGame\CookedSwitch"

for p in sorted(pkgs):
    in_mod = os.path.exists(os.path.join(mod_dir, p))
    in_sw = os.path.exists(os.path.join(sw_dir, p))
    print(f"  {p}: in_romfs={in_sw}, in_modthai={in_mod}")
