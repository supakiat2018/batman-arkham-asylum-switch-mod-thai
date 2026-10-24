import os
import sys
import csv

sys.path.append(r"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools")
from switch_inject import decompress_package, inject_package

base_dir = r"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum"
csv_path = os.path.join(base_dir, "switch_untranslated_subtitles.csv")
subs_tsv = os.path.join(base_dir, "work", "data", "subs.tsv")
src_dir = os.path.join(base_dir, "romfs", "BmGame", "CookedSwitch")
dst_dir = os.path.join(base_dir, "modthai", "0100E870163CA000", "romfs", "BmGame", "CookedSwitch")
yuzu_dir = r"C:\Users\Supakiat\AppData\Roaming\yuzu\load\0100E870163CA000\MODTHAI\romfs\BmGame\CookedSwitch"

# Read existing subs.tsv
translations = {}
with open(subs_tsv, "r", encoding="utf-8") as f:
    for line in f:
        line = line.strip("\r\n")
        if "\t" in line:
            k, v = line.split("\t", 1)
            translations[k] = v

# Add the 188 translated lines to translations
new_sub_lines = []
with open(csv_path, "r", encoding="utf-8-sig") as f:
    r = csv.reader(f)
    next(r)
    for row in r:
        th = row[5] # clean Thai
        pkgs = [p.strip() for p in row[3].split("; ")]
        objs = [o.strip() for o in row[4].split("; ")]
        for obj in objs:
            key1 = f"{obj}|Subtitles[0].Text"
            translations[key1] = th
            new_sub_lines.append(f"{key1}\t{th}\n")

# Append new lines to subs.tsv
with open(subs_tsv, "a", encoding="utf-8") as f:
    f.writelines(new_sub_lines)

print(f"Total translations now in dictionary: {len(translations)}")

# List of target DLC packages
target_pkgs = [
    "Cave_B1_MixSyrum_CH567_LOC_INT.xxx",
    "Death_Batman_LOC_INT.xxx",
    "Death_Cash_LOC_INT.xxx",
    "LH-J-G1-Combat_SF_LOC_INT.xxx",
    "LH-J-G1_SF_LOC_INT.xxx",
    "LH-J-G2-Combat_SF_LOC_INT.xxx",
    "LH-J-G2_SF_LOC_INT.xxx",
    "LH-J-Predator-Cash_SF_LOC_INT.xxx",
    "Max_C1_CH5_LOC_INT.xxx"
]

injected = 0
for pkg in target_pkgs:
    src_f = os.path.join(src_dir, pkg)
    dst_f = os.path.join(dst_dir, pkg)
    yuzu_f = os.path.join(yuzu_dir, pkg)
    
    if not os.path.exists(src_f):
        print(f"File not found in romfs: {pkg}")
        continue
        
    with open(src_f, "rb") as f:
        raw = f.read()
        
    decomp = decompress_package(raw)
    patched = inject_package(decomp, translations)
    if patched:
        with open(dst_f, "wb") as f: f.write(patched)
        with open(yuzu_f, "wb") as f: f.write(patched)
        injected += 1
        print(f"Successfully injected: {pkg} ({len(raw):,} -> {len(patched):,} bytes)")
    else:
        print(f"No match in: {pkg}")

print(f"Finished injecting {injected}/{len(target_pkgs)} Joker DLC packages!")
