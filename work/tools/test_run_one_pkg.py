import os
import sys
sys.path.append(r"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools")
from switch_inject import decompress_package, inject_package

# Load translations
subs_path = r"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\data\subs.tsv"
translations = {}
with open(subs_path, "r", encoding="utf-8") as f:
    for line in f:
        line = line.strip("\r\n")
        if "\t" not in line: continue
        k, v = line.split("\t", 1)
        translations[k] = v

src_xxx = r"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\romfs\BmGame\CookedSwitch\Admin_A_LOC_INT.xxx"
dst_xxx = r"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\modthai\0100E870163CA000\romfs\BmGame\CookedSwitch\Admin_A_LOC_INT.xxx"

print(f"Reading {src_xxx}...")
with open(src_xxx, "rb") as f:
    raw = f.read()

print("Decompressing Oodle chunks...")
decomp = decompress_package(raw)
print(f"Decompressed size: {len(decomp)} bytes")

print("Injecting Thai translations...")
patched = inject_package(decomp, translations)

if patched:
    print(f"Success! Patched size: {len(patched)} bytes")
    os.makedirs(os.path.dirname(dst_xxx), exist_ok=True)
    with open(dst_xxx, "wb") as f:
        f.write(patched)
    print(f"Saved to {dst_xxx}")
else:
    print("No changes made.")
