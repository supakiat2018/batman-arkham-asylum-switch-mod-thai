import os, struct, sys
sys.path.append(r"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools")
from switch_inject import decompress_package

orig_f = r"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\romfs\BmGame\CookedSwitch\Max_IntroParty_LOC_INT.xxx"
mod_f = r"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\modthai\0100E870163CA000\romfs\BmGame\CookedSwitch\Max_IntroParty_LOC_INT.xxx"

with open(orig_f, "rb") as f: d_orig = decompress_package(f.read())
with open(mod_f, "rb") as f: d_mod = decompress_package(f.read())

pos = 8; pos += 4; flen = struct.unpack_from("<i", d_orig, pos)[0]; pos += 4
if flen > 0: pos += flen
elif flen < 0: pos += (-flen)*2
pos += 4
name_count, name_offset = struct.unpack_from("<ii", d_orig, pos); pos += 8
names = []
p = name_offset
for _ in range(name_count):
    sl = struct.unpack_from("<i", d_orig, p)[0]; p += 4
    if sl > 0: names.append(d_orig[p:p+sl-1].decode("latin1", "replace")); p += sl
    elif sl < 0: names.append(d_orig[p:p+(-sl-1)*2].decode("utf-16le", "replace")); p += (-sl)*2
    p += 8

e_orig = d_orig[4484:4484+22167]
e_mod = d_mod[639521:639521+22607]

def dump_tags(data, max_p=6000):
    p = 4
    while p + 8 <= max_p:
        n_idx, n_num = struct.unpack_from("<ii", data, p)
        if n_idx < 0 or n_idx >= len(names):
            print(f"  [0x{p:04X}] BAD NAME INDEX: {n_idx}")
            break
        name = names[n_idx]
        if name == "None":
            print(f"  [0x{p:04X}] None (End of properties)")
            return p + 8
        t_idx, t_num = struct.unpack_from("<ii", data, p + 8)
        ptype = names[t_idx] if 0 <= t_idx < len(names) else f"<bad_{t_idx}>"
        sz = struct.unpack_from("<i", data, p + 16)[0]
        val_p = p + 26
        if ptype == "StructProperty":
            val_p += 8
        real = 4 if ptype == "BoolProperty" else sz
        print(f"  [0x{p:04X}] {name} ({ptype}) sz={sz}")
        p = val_p + real
    return p

print("--- ORIG TAGS ---")
dump_tags(e_orig)

print("\n--- MOD TAGS ---")
dump_tags(e_mod)
