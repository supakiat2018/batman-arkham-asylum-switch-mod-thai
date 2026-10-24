import os, struct, sys
sys.path.append(r"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools")
from switch_inject import decompress_package

mod_f = r"C:\Users\Supakiat\AppData\Roaming\yuzu\load\0100E870163CA000\MODTHAI\romfs\BmGame\CookedSwitch\Max_IntroParty_LOC_INT.xxx"
with open(mod_f, "rb") as f: d_mod = decompress_package(f.read())

from debug_export import dump_tags
pos = 8; pos += 4; flen = struct.unpack_from("<i", d_mod, pos)[0]; pos += 4
if flen > 0: pos += flen
elif flen < 0: pos += (-flen)*2
pos += 4
name_count, name_offset = struct.unpack_from("<ii", d_mod, pos); pos += 8
names = []
p = name_offset
for _ in range(name_count):
    sl = struct.unpack_from("<i", d_mod, p)[0]; p += 4
    if sl > 0: names.append(d_mod[p:p+sl-1].decode("latin1", "replace")); p += sl
    elif sl < 0: names.append(d_mod[p:p+(-sl-1)*2].decode("utf-16le", "replace")); p += (-sl)*2
    p += 8

# Find string properties in Export 17 (Max_B1_CH1_BM_J_ELEV_Top_04)
# In mod, Export 17 is at 831168, size 11663
e = d_mod[831168:831168+11663]
from switch_inject import collect_strings
refs, _ = collect_strings(e, 4, len(e), "", [], names)
print(f"Export 17 collected strings: {len(refs)}")
for r in refs:
    print(f"  {r.path} = {repr(r.text)}")
