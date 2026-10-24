import os, struct, sys
sys.path.append(r"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools")
from switch_inject import decompress_package

orig_f = r"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\romfs\BmGame\CookedSwitch\Max_IntroParty_LOC_INT.xxx"
mod_f = r"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\modthai\0100E870163CA000\romfs\BmGame\CookedSwitch\Max_IntroParty_LOC_INT.xxx"

with open(orig_f, "rb") as f: d_orig = decompress_package(f.read())
with open(mod_f, "rb") as f: d_mod = decompress_package(f.read())

e_orig = d_orig[244200:244200+11519]
e_mod = d_mod[831168:831168+11663]

nat_o = e_orig[4052:]
nat_m = e_mod[4196:]

print(f"Nat orig len: {len(nat_o)}")
print(f"Nat mod len:  {len(nat_m)}")

if nat_o == nat_m:
    print("Native data is 100% IDENTICAL!")
else:
    diffs = [i for i in range(min(len(nat_o), len(nat_m))) if nat_o[i] != nat_m[i]]
    print(f"Number of differing bytes: {len(diffs)}")
    print("Diffs at bytes:", diffs[:15])
    for d in diffs[:8]:
        val_o = struct.unpack_from("<i", nat_o, d)[0]
        val_m = struct.unpack_from("<i", nat_m, d)[0]
        print(f"  At offset {d}: orig={val_o} (0x{val_o:X}), mod={val_m} (0x{val_m:X})")
