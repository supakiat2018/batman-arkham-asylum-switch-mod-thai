import os, struct, sys
sys.path.append(r"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools")
from switch_inject import decompress_package

orig_f = r"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\romfs\BmGame\CookedSwitch\Max_IntroParty_LOC_INT.xxx"
with open(orig_f, "rb") as f: d_orig = decompress_package(f.read())

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

e_orig = d_orig[244200:244200+11519]

from debug_export import dump_tags
dump_tags(e_orig)
