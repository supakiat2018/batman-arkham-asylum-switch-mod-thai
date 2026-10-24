import os
import json
import struct

mover_map_path = r"C:\Users\Supakiat\Desktop\Mover\สร้างฟ้อน\Mapping.json"
with open(mover_map_path, "r", encoding="utf-8") as f:
    mover_map = json.load(f)

print(f"Mover Mapping has {len(mover_map)} entries.")
sample_items = list(mover_map.items())[:10]
for k, v in sample_items:
    print(f"  {repr(k)} -> {repr(v)} (U+{ord(v):04X})")

# Check what character ranges exist in Mover Mapping
chars_in_v = [ord(v) for v in mover_map.values()]
print(f"Mover Map value range: U+{min(chars_in_v):04X} to U+{max(chars_in_v):04X}")

# Now let's check what PUA or shaped characters the Batman Arkham Asylum PC mod uses!
# Read some dialogue from pc_mod_all.tsv
pc_mod_tsv = r"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\output\pc_mod_all.tsv"
bm_pua_chars = set()
with open(pc_mod_tsv, "r", encoding="utf-8", errors="replace") as f:
    for line in f:
        parts = line.strip().split("\t")
        if len(parts) >= 5 and "Subtitles" in parts[3]:
            for ch in parts[4]:
                code = ord(ch)
                if code >= 0x0080 and code < 0x0E00:
                    bm_pua_chars.add(code)
                elif code >= 0xF000:
                    bm_pua_chars.add(code)

print(f"Batman PC mod unique non-ASCII / PUA char codes: {len(bm_pua_chars)}")
sorted_codes = sorted(list(bm_pua_chars))
print("Batman PC mod codes:", [f"0x{c:02X} (U+{c:04X})" for c in sorted_codes])
