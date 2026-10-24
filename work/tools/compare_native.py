import os, struct, sys
sys.path.append(r"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools")
from switch_inject import decompress_package

orig_f = r"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\romfs\BmGame\CookedSwitch\Max_IntroParty_LOC_INT.xxx"
mod_f = r"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\modthai\0100E870163CA000\romfs\BmGame\CookedSwitch\Max_IntroParty_LOC_INT.xxx"

with open(orig_f, "rb") as f: d_orig = decompress_package(f.read())
with open(mod_f, "rb") as f: d_mod = decompress_package(f.read())

e_orig = d_orig[4484:4484+22167]
e_mod = d_mod[639521:639521+22607]

nat_orig = e_orig[0x0F7D:]
nat_mod = e_mod[0x1135:]

print(f"Native orig len: {len(nat_orig)}")
print(f"Native mod len:  {len(nat_mod)}")

if nat_orig == nat_mod:
    print("Native data is 100% IDENTICAL!")
else:
    diffs = [i for i in range(min(len(nat_orig), len(nat_mod))) if nat_orig[i] != nat_mod[i]]
    print(f"Number of differing bytes: {len(diffs)}")
    print("Diffs at bytes:", diffs[:10])
    for d in diffs[:5]:
        val_o = struct.unpack_from("<i", nat_orig, d)[0]
        val_m = struct.unpack_from("<i", nat_mod, d)[0]
        print(f"  At offset {d}: orig={val_o} (0x{val_o:X}), mod={val_m} (0x{val_m:X})")
