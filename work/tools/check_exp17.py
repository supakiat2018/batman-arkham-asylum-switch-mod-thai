import os, struct, sys
sys.path.append(r"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools")
from switch_inject import decompress_package

orig_f = r"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\romfs\BmGame\CookedSwitch\Max_IntroParty_LOC_INT.xxx"
mod_f = r"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\modthai\0100E870163CA000\romfs\BmGame\CookedSwitch\Max_IntroParty_LOC_INT.xxx"

with open(orig_f, "rb") as f: d_orig = decompress_package(f.read())
with open(mod_f, "rb") as f: d_mod = decompress_package(f.read())

e_orig = d_orig[244200:244200+11519]
e_mod = d_mod[831168:831168+11663]

# find None
for p in range(4, 5000, 4):
    if struct.unpack_from("<ii", e_orig, p) == (41, 0):
        print(f"Orig None at {p}")
        nat_o = e_orig[p+8:]
        break

for p in range(4, 5000, 4):
    if struct.unpack_from("<ii", e_mod, p) == (41, 0):
        print(f"Mod None at {p}")
        nat_m = e_mod[p+8:]
        break

print(f"Nat orig len: {len(nat_o)}")
print(f"Nat mod len:  {len(nat_m)}")

# Print all 16-byte bulk headers in nat_orig vs nat_mod
print("\n--- Orig Bulk Headers in Native ---")
for r in range(0, 128, 16):
    fl, cnt, sz, off = struct.unpack_from("<IIii", nat_o, r)
    print(f"[{r:02d}] flags=0x{fl:X}, cnt={cnt}, sz={sz}, off={off} (abs={244200 + p + 8 + r})")

print("\n--- Mod Bulk Headers in Native ---")
for r in range(0, 128, 16):
    fl, cnt, sz, off = struct.unpack_from("<IIii", nat_m, r)
    print(f"[{r:02d}] flags=0x{fl:X}, cnt={cnt}, sz={sz}, off={off} (abs={831168 + p + 8 + r})")
