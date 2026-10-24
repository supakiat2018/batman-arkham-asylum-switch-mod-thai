import os
import sys
import glob
import time

sys.path.append(r"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools")
from switch_inject import decompress_package, inject_package

# Paths
base_dir = r"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum"
subs_path = os.path.join(base_dir, "work", "data", "subs.tsv")
src_dir = os.path.join(base_dir, "romfs", "BmGame", "CookedSwitch")
dst_dir = os.path.join(base_dir, "modthai", "0100E870163CA000", "romfs", "BmGame", "CookedSwitch")

os.makedirs(dst_dir, exist_ok=True)

# Load translations
translations = {}
with open(subs_path, "r", encoding="utf-8") as f:
    for line in f:
        line = line.strip("\r\n")
        if "\t" not in line: continue
        k, v = line.split("\t", 1)
        translations[k] = v

print(f"Loaded {len(translations)} subtitle translations.")

# Get all _LOC_INT.xxx files
files = glob.glob(os.path.join(src_dir, "*_LOC_INT.xxx"))
files.sort()
print(f"Found {len(files)} LOC_INT packages to process.")

start_time = time.time()
processed_count = 0
injected_count = 0
skipped_count = 0

for idx, fpath in enumerate(files):
    fname = os.path.basename(fpath)
    out_path = os.path.join(dst_dir, fname)

    with open(fpath, "rb") as f:
        raw = f.read()

    try:
        decomp = decompress_package(raw)
        patched = inject_package(decomp, translations)
        if patched:
            with open(out_path, "wb") as f:
                f.write(patched)
            injected_count += 1
            print(f"[{idx+1}/{len(files)}] [INJECTED] {fname} ({len(raw):,} -> {len(patched):,} bytes)")
        else:
            skipped_count += 1
    except Exception as ex:
        print(f"[{idx+1}/{len(files)}] [ERROR] {fname}: {ex}")

    processed_count += 1

elapsed = time.time() - start_time
print("=" * 60)
print(f"Batch injection completed in {elapsed:.1f} seconds!")
print(f"Total processed: {processed_count}")
print(f"Successfully injected: {injected_count} packages")
print(f"Skipped (no subtitles): {skipped_count} packages")
print(f"Saved into: {dst_dir}")
print("=" * 60)
