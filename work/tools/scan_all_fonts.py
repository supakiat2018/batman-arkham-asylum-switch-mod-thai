import os
import struct
import sys

sys.stdout.reconfigure(encoding="utf-8")

sw_dir = r"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\romfs\BmGame\CookedSwitch"
work_dir = r"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work"

font_packages = []

# Scan all packages for Font classes
all_files = [os.path.join(sw_dir, f) for f in os.listdir(sw_dir) if f.endswith((".xxx", ".upk"))]
all_files.sort()

print(f"Scanning {len(all_files)} Switch packages for fonts...")

for fpath in all_files:
    fname = os.path.basename(fpath)
    with open(fpath, "rb") as f:
        header = f.read(1024)
    if len(header) < 32: continue
    magic = struct.unpack_from("<I", header, 0)[0]
    if magic != 0x9E2A83C1: continue
    
    # We can check if file is in work/output/switch_vanilla_all.tsv
    # Wait, earlier we ran survey on all switch packages!
    pass

# Read survey results from switch_vanilla_all.tsv
survey_tsv = os.path.join(work_dir, "output", "switch_vanilla_all.tsv")
fonts_found = set()
with open(survey_tsv, "r", encoding="utf-8", errors="replace") as f:
    for line in f:
        parts = line.strip().split("\t")
        if len(parts) >= 5:
            pkg, cls, obj, path, val = parts[0], parts[1], parts[2], parts[3], parts[4]
            if "font" in cls.lower() or "font" in obj.lower():
                fonts_found.add((pkg, cls, obj))

print(f"Found {len(fonts_found)} font-related objects in Switch survey:")
for pkg, cls, obj in sorted(fonts_found):
    print(f"  [{pkg}] {cls} -> {obj}")
