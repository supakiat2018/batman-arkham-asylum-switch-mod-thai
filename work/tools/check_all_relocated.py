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
exp_count, exp_offset = struct.unpack_from("<ii", d_orig, pos); pos += 8

names = []
p = name_offset
for _ in range(name_count):
    sl = struct.unpack_from("<i", d_orig, p)[0]; p += 4
    if sl > 0: names.append(d_orig[p:p+sl-1].decode("latin1", "replace")); p += sl
    elif sl < 0: names.append(d_orig[p:p+(-sl-1)*2].decode("utf-16le", "replace")); p += (-sl)*2
    p += 8

p0 = exp_offset
net0 = struct.unpack_from("<i", d_orig, p0 + 44)[0]
entry_size = 44 + 4 + net0 * 4 + 16 + 4

for i in range(exp_count):
    ep = exp_offset + i * entry_size
    ssize_o, soffset_o = struct.unpack_from("<ii", d_orig, ep + 32)
    ssize_m, soffset_m = struct.unpack_from("<ii", d_mod, ep + 32)
    if soffset_o != soffset_m:
        n_idx, n_num = struct.unpack_from("<ii", d_orig, ep + 12)
        n = names[n_idx] if n_idx < len(names) else f"exp_{i+1}"
        e_orig = d_orig[soffset_o:soffset_o+ssize_o]
        e_mod = d_mod[soffset_m:soffset_m+ssize_m]
        
        # Check bulk data offsets in e_orig
        unfixed_pointers = []
        for r in range(0, len(e_orig)-16, 4):
            flags, count, sz, off = struct.unpack_from("<IIii", e_orig, r)
            if sz > 0 and count == sz and soffset_o <= off <= soffset_o + ssize_o:
                # check if e_mod has updated offset
                # relative pos in e_mod?
                rel_in_orig = off - soffset_o
                # search for sz and off in e_mod
                found = False
                for r2 in range(0, len(e_mod)-16, 4):
                    f2, c2, s2, o2 = struct.unpack_from("<IIii", e_mod, r2)
                    if s2 == sz and o2 == soffset_m + (o2 - soffset_m):
                        if o2 == soffset_m + r2 + 4:
                            found = True
                            break
                if not found:
                    unfixed_pointers.append((r, sz, off))
        if unfixed_pointers:
            print(f"Export {i+1} ({n}) HAS UNFIXED POINTERS: {len(unfixed_pointers)}")
        else:
            print(f"Export {i+1} ({n}) OK")
