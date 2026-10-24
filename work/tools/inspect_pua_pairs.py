import os
import sys

sys.stdout.reconfigure(encoding="utf-8")

work_dir = r"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work"
subs_tsv = os.path.join(work_dir, "data", "subs.tsv")
pm_tsv = os.path.join(work_dir, "output", "pc_mod_all.tsv")

clean_map = {}
with open(subs_tsv, "r", encoding="utf-8") as f:
    for line in f:
        line = line.strip("\r\n")
        if "\t" in line:
            k, v = line.split("\t", 1)
            clean_map[k] = v

mod_map = {}
with open(pm_tsv, "r", encoding="utf-8", errors="replace") as f:
    for line in f:
        parts = line.strip("\r\n").split("\t")
        if len(parts) >= 5 and "Subtitles" in parts[3]:
            mod_map[f"{parts[2]}|{parts[3]}"] = parts[4]

# Compare characters and n-grams
diff_chars = {}
diff_ngrams = {}

for k, clean_s in clean_map.items():
    if k in mod_map:
        mod_s = mod_map[k]
        # align characters
        # Both clean_s and mod_s should have similar lengths
        i = 0
        j = 0
        while i < len(clean_s) and j < len(mod_s):
            c_char = clean_s[i]
            m_char = mod_s[j]
            if c_char == m_char:
                i += 1
                j += 1
            else:
                # Find context: up to 3 chars around
                # e.g. clean: ไว + ้ vs mod: ไว + \x84
                # or clean: ป + ิ + ้ vs mod: ป + ...
                c_sub = clean_s[max(0, i-2):min(len(clean_s), i+3)]
                m_sub = mod_s[max(0, j-2):min(len(mod_s), j+3)]
                pair = (c_char, m_char)
                diff_chars[pair] = diff_chars.get(pair, 0) + 1
                i += 1
                j += 1

print(f"Total unique char differences: {len(diff_chars)}")
for (c, m), cnt in sorted(diff_chars.items(), key=lambda x: -x[1])[:30]:
    print(f"Clean: '{c}' (U+{ord(c):04X}) -> Mod: '{m}' (U+{ord(m):04X}) | count={cnt}")
