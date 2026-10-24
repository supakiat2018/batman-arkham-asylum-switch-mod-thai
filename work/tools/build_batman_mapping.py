import os
import sys
import json
import re

sys.stdout.reconfigure(encoding="utf-8")

work_dir = r"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work"
subs_tsv = os.path.join(work_dir, "data", "subs.tsv")
pm_tsv = os.path.join(work_dir, "output", "pc_mod_all.tsv")

clean_dict = {}
with open(subs_tsv, "r", encoding="utf-8") as f:
    for line in f:
        line = line.strip("\r\n")
        if "\t" in line:
            k, v = line.split("\t", 1)
            clean_dict[k] = v

mod_dict = {}
with open(pm_tsv, "r", encoding="utf-8", errors="replace") as f:
    for line in f:
        parts = line.strip("\r\n").split("\t")
        if len(parts) >= 5 and "Subtitles" in parts[3]:
            mod_dict[f"{parts[2]}|{parts[3]}"] = parts[4]

# Also read UI clean vs UI mod
# In work/data/ui.tsv:
ui_tsv = os.path.join(work_dir, "data", "ui.tsv")
ui_clean = {}
if os.path.exists(ui_tsv):
    with open(ui_tsv, "r", encoding="utf-8") as f:
        for line in f:
            line = line.strip("\r\n")
            if "\t" in line:
                k, v = line.split("\t", 1)
                ui_clean[k] = v

print(f"Loaded {len(clean_dict)} subtitle lines, {len(ui_clean)} UI lines")

# Let's align clean and mod text to discover all cluster mappings
mapping = {}

# Thai character classification
consonants = set("กขฃคฅฆงจฉชซฌญฎฏฐฑฒณดตถทธนบปผฝพฟภมยรฤลฦวศษสหฬอฮ")
upper_vowels = set("ัิีึื็ํ")
lower_vowels = set("ฺุู")
tones = set("่้๊๋์")

def extract_clusters(text):
    # Returns list of (start, end, cluster)
    # A Thai cluster typically is:
    # Leading vowel (optional) + Consonant + (Upper/Lower vowel optional) + (Tone optional)
    # Or Consonant + Upper vowel + Tone, etc.
    # We want clusters that have marks:
    # 1. Consonant + Upper vowel + Tone (3 chars)
    # 2. Consonant + Lower vowel + Tone (3 chars)
    # 3. Consonant + Upper vowel (2 chars)
    # 4. Consonant + Lower vowel (2 chars)
    # 5. Consonant + Tone (2 chars)
    clusters = []
    i = 0
    n = len(text)
    while i < n:
        c = text[i]
        if c in consonants:
            # check what follows
            end = i + 1
            has_mark = False
            while end < n and (text[end] in upper_vowels or text[end] in lower_vowels or text[end] in tones):
                has_mark = True
                end += 1
            if has_mark:
                clusters.append((i, end, text[i:end]))
            i = end
        else:
            i += 1
    return clusters

# Map each clean cluster to what it becomes in mod
import difflib

cluster_votes = {} # clean_cluster -> { mod_cluster: count }

matched_pairs = 0
for k, clean_text in clean_dict.items():
    if k in mod_dict:
        mod_text = mod_dict[k]
        if clean_text == mod_text:
            continue
        matched_pairs += 1
        # Use SequenceMatcher to align
        sm = difflib.SequenceMatcher(None, clean_text, mod_text)
        for tag, i1, i2, j1, j2 in sm.get_opcodes():
            c_sub = clean_text[i1:i2]
            m_sub = mod_text[j1:j2]
            if tag == 'replace':
                # Check if this matches a Thai cluster
                # Extend to include base consonant if needed
                # If c_sub starts with a mark, include previous char if it's consonant
                if i1 > 0 and clean_text[i1] in (upper_vowels | lower_vowels | tones):
                    c_sub = clean_text[i1-1:i2]
                    m_sub = mod_text[max(0, j1-1):j2]
                if any(ch in (upper_vowels | lower_vowels | tones) for ch in c_sub):
                    if c_sub not in cluster_votes:
                        cluster_votes[c_sub] = {}
                    cluster_votes[c_sub][m_sub] = cluster_votes[c_sub].get(m_sub, 0) + 1

print(f"Matched {matched_pairs} different subtitle strings.")
print(f"Extracted {len(cluster_votes)} raw cluster replacements.")

# Let's inspect the most frequent cluster mappings
batman_mapping = {}
for c_clus, m_dict in cluster_votes.items():
    # Pick the most voted mapping
    best_m = max(m_dict.items(), key=lambda x: x[1])[0]
    # Filter out junk/alignment errors: length should be reasonable
    if len(c_clus) <= 4 and len(best_m) <= 4 and c_clus != best_m:
        batman_mapping[c_clus] = best_m

print(f"Total valid cluster mappings found from translation: {len(batman_mapping)}")

# Also, combine with all canonical ThaiShaper rules:
# In Font.cs, ThaiShaper defines all permutations of:
# AC (ป ฝ ฟ ฬ), RC (ญ ฐ), DC (ฎ ฏ), NC (others) x AV, BV, T
# Let's generate all permutations through ThaiShaper and add any missing ones!
from switch_inject import shape_thai

all_consonants = list("กขฃคฅฆงจฉชซฌญฎฏฐฑฒณดตถทธนบปผฝพฟภมยรฤลฦวศษสหฬอฮ")
all_marks = list("ัิีึืุู็่้๊๋์ํ")

for cons in all_consonants:
    # Consonant + mark
    for m in all_marks:
        pair = cons + m
        shaped = shape_thai(pair)
        if shaped != pair:
            # Check how it maps in PC Mod
            if pair not in batman_mapping:
                batman_mapping[pair] = shaped
    # Consonant + vowel + tone
    for v in "ัิีึืุู็":
        for t in "่้๊๋์":
            triplet = cons + v + t
            shaped = shape_thai(triplet)
            if shaped != triplet:
                if triplet not in batman_mapping:
                    batman_mapping[triplet] = shaped

print(f"Total entries in Batman Mapping after rule expansion: {len(batman_mapping)}")

# Sort by key length descending, then key alphabetically (Longest Match First)
sorted_mapping = dict(sorted(batman_mapping.items(), key=lambda x: (-len(x[0]), x[0])))

# Output to C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\Mapping.json
out_json1 = r"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\Mapping.json"
out_json2 = r"C:\Users\Supakiat\Desktop\Mover\สร้างฟ้อน\Batman_Mapping.json"

with open(out_json1, "w", encoding="utf-8") as f:
    json.dump(sorted_mapping, f, ensure_ascii=False, indent=2)

try:
    with open(out_json2, "w", encoding="utf-8") as f:
        json.dump(sorted_mapping, f, ensure_ascii=False, indent=2)
except Exception as ex:
    print(f"Notice: {ex}")

print(f"Saved Batman Mapping.json to {out_json1} ({len(sorted_mapping)} mappings)!")
