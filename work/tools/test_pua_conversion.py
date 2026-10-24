import csv
import json
import os
import sys

sys.stdout.reconfigure(encoding="utf-8")

mapping_path = r"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\Mapping.json"
with open(mapping_path, "r", encoding="utf-8") as f:
    mapping = json.load(f)

max_len = max(len(k) for k in mapping.keys())
print(f"Loaded Mapping.json: {len(mapping)} keys, max_len = {max_len}")

def convert_text_to_pua(text: str) -> str:
    if not text:
        return text
    result = []
    i = 0
    n = len(text)
    while i < n:
        matched = False
        for length in range(min(max_len, n - i), 0, -1):
            substr = text[i : i + length]
            if substr in mapping:
                result.append(mapping[substr])
                i += length
                matched = True
                break
        if not matched:
            result.append(text[i])
            i += 1
    return "".join(result)

# Test on a few sample phrases
test_phrases = [
    "แกไม่มีวันชนะหรอก โจ๊กเกอร์ ฉันจะคอยขัดขวางแกเสมอ",
    "มึงเสร็จแน่ โจ๊กเกอร์!",
    "กูจะย่างมึงให้เกรียมเลย โจ๊กเกอร์!"
]

for tp in test_phrases:
    pua = convert_text_to_pua(tp)
    print(f"Original: {tp}")
    print(f"PUA:      {repr(pua)}")
    print("---")
