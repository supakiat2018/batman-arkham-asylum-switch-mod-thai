import os
import struct
import uharfbuzz as hb

# Load translation dictionary from subs.tsv
subs_path = r"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\data\subs.tsv"
translations = {}
with open(subs_path, "r", encoding="utf-8") as f:
    for line in f:
        line = line.strip("\r\n")
        if "\t" not in line:
            continue
        key, text = line.split("\t", 1)
        translations[key] = text

print(f"Loaded {len(translations)} subtitle translations from subs.tsv")

# Test file
src_path = r"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\decompressed\Admin_A_LOC_INT.xxx"
with open(src_path, "rb") as f:
    d = bytearray(f.read())

# Read Name table
pos = 8
pos += 4 # header size
flen = struct.unpack_from("<i", d, pos)[0]; pos += 4
if flen > 0: pos += flen
elif flen < 0: pos += (-flen)*2
pos += 4 # pkg_flags
name_count, name_offset = struct.unpack_from("<ii", d, pos); pos += 8
exp_count, exp_offset = struct.unpack_from("<ii", d, pos); pos += 8
imp_count, imp_offset = struct.unpack_from("<ii", d, pos); pos += 8

names = []
p = name_offset
for _ in range(name_count):
    sl = struct.unpack_from("<i", d, p)[0]; p += 4
    if sl > 0:
        names.append(d[p:p+sl-1].decode("latin1"))
        p += sl
    elif sl < 0:
        names.append(d[p:p+(-sl-1)*2].decode("utf-16le"))
        p += (-sl)*2
    p += 8

def get_name(idx, num=0):
    if 0 <= idx < len(names):
        s = names[idx]
        return f"{s}_{num-1}" if num > 0 else s
    return f"<bad_{idx}>"

# Read Imports and Exports
imports = []
p = imp_offset
for _ in range(imp_count):
    cp_idx, cp_num = struct.unpack_from("<ii", d, p); p += 8
    cn_idx, cn_num = struct.unpack_from("<ii", d, p); p += 8
    outer = struct.unpack_from("<i", d, p)[0]; p += 4
    n_idx, n_num = struct.unpack_from("<ii", d, p); p += 8
    imports.append({"class": get_name(cn_idx, cn_num), "name": get_name(n_idx, n_num), "outer": outer})

exports = []
p = exp_offset
for _ in range(exp_count):
    table_pos = p
    c_idx, s_idx, outer_idx = struct.unpack_from("<iii", d, p); p += 12
    n_idx, n_num = struct.unpack_from("<ii", d, p); p += 8
    arch = struct.unpack_from("<i", d, p)[0]; p += 4
    flags = struct.unpack_from("<Q", d, p)[0]; p += 8
    size_field_pos = p
    ssize, soffset = struct.unpack_from("<ii", d, p); p += 8
    p += 4 # export flags
    net = struct.unpack_from("<i", d, p)[0]; p += 4 + net * 4
    p += 16 + 4 # guid, unk
    exports.append({
        "table_pos": table_pos,
        "class_idx": c_idx,
        "outer_idx": outer_idx,
        "name": get_name(n_idx, n_num),
        "size_field_pos": size_field_pos,
        "serial_size": ssize,
        "serial_offset": soffset
    })

def full_name(obj_idx):
    parts = []
    curr = obj_idx
    while curr != 0:
        if curr > 0:
            exp = exports[curr - 1]
            parts.append(exp["name"])
            curr = exp["outer_idx"]
        else:
            imp = imports[-curr - 1]
            parts.append(imp["name"])
            curr = imp["outer"]
    parts.reverse()
    return ".".join(parts)

matched = 0
for idx, exp in enumerate(exports):
    fname = full_name(idx + 1)
    key = f"{fname}|Subtitles[0].Text"
    if key in translations:
        matched += 1
        if matched <= 5:
            print(f"Matched: {key} -> {translations[key][:40]}...")

print(f"Total matched SoundNodeWave exports in Admin_A_LOC_INT: {matched}/{exp_count}")
