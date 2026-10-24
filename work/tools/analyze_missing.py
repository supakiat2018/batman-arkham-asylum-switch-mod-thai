import os
import sys
import re
import csv

work_dir = r"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work"
pv_tsv = os.path.join(work_dir, "output", "pc_vanilla_all.tsv")
pm_tsv = os.path.join(work_dir, "output", "pc_mod_all.tsv")
sw_tsv = os.path.join(work_dir, "output", "switch_vanilla_all.tsv")
subs_tsv = os.path.join(work_dir, "data", "subs.tsv")

clean_subs = {}
with open(subs_tsv, "r", encoding="utf-8") as f:
    for line in f:
        line = line.strip("\r\n")
        if "\t" in line:
            k, v = line.split("\t", 1)
            clean_subs[k] = v

# PC Vanilla English mapping
pv_data = {}
with open(pv_tsv, "r", encoding="utf-8", errors="replace") as f:
    for line in f:
        parts = line.strip("\r\n").split("\t")
        if len(parts) >= 5 and parts[3] == "Subtitles[0].Text":
            pv_data[parts[2]] = parts[4]

en_to_th = {}
obj_to_th = {}
for obj, en_text in pv_data.items():
    th = clean_subs.get(f"{obj}|Subtitles[0].Text")
    if th:
        en_to_th[en_text.strip()] = th
        obj_to_th[obj] = th

tag_rx = re.compile(r"<[^>]+>")

# Normalize function
def norm(s):
    # strip tags
    s = tag_rx.sub("", s)
    s = s.replace("…", "...").replace("’", "'").replace("‘", "'").replace("“", '"').replace("”", '"')
    return " ".join(s.split())

norm_en_to_th = {norm(k): v for k, v in en_to_th.items()}

sw_entries = []
with open(sw_tsv, "r", encoding="utf-8", errors="replace") as f:
    for line in f:
        parts = line.strip("\r\n").split("\t")
        if len(parts) >= 5 and parts[3] == "Subtitles[0].Text":
            sw_entries.append((parts[0], parts[2], parts[4]))

print(f"Total Switch Subtitles[0].Text entries: {len(sw_entries)}")

matched_exact = 0
matched_norm = 0
matched_obj = 0
untranslated = []

for fpath, obj, sw_text in sw_entries:
    if sw_text in en_to_th:
        matched_exact += 1
    elif norm(sw_text) in norm_en_to_th:
        matched_norm += 1
    elif obj in obj_to_th:
        matched_obj += 1
    else:
        untranslated.append((fpath, obj, sw_text))

total_matched = matched_exact + matched_norm + matched_obj
print(f"Matched Exact: {matched_exact}")
print(f"Matched after Tag/Quote Normalization: {matched_norm}")
print(f"Matched by Object Name: {matched_obj}")
print(f"Total Matched: {total_matched} / {len(sw_entries)} ({total_matched/len(sw_entries)*100:.1f}%)")
print(f"Untranslated count: {len(untranslated)}")

# Write untranslated to CSV for the user
csv_out = os.path.join(work_dir, "output", "switch_untranslated_subtitles.csv")
# Deduplicate untranslated by English text while keeping occurrences
seen_texts = {}
for fpath, obj, text in untranslated:
    clean = tag_rx.sub("", text).strip()
    if clean not in seen_texts:
        seen_texts[clean] = []
    seen_texts[clean].append((fpath, obj, text))

print(f"Unique untranslated English lines: {len(seen_texts)}")

with open(csv_out, "w", encoding="utf-8-sig", newline="") as f:
    writer = csv.writer(f)
    writer.writerow(["Index", "English Text (Clean)", "Original Text With Tags", "Package", "SoundNodeWave Object", "Thai Translation (ใส่คำแปลภาษาไทยที่นี่)"])
    idx = 1
    for clean_text, occurrences in seen_texts.items():
        first = occurrences[0]
        all_pkgs = "; ".join(sorted(list(set(x[0] for x in occurrences))))
        all_objs = "; ".join(sorted(list(set(x[1] for x in occurrences))))
        writer.writerow([idx, clean_text, first[2], all_pkgs, all_objs, ""])
        idx += 1

print(f"Saved untranslated subtitles to CSV: {csv_out}")

# Print sample 15
print("\n=== Sample 15 Untranslated Lines ===")
for i, (clean_text, occs) in enumerate(list(seen_texts.items())[:15]):
    print(f"[{i+1}] ({occs[0][0]}) {occs[0][1]}: \"{clean_text}\"")
