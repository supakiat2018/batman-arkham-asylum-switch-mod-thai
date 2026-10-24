import os, struct, sys
sys.path.append(r"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools")
from switch_inject import decompress_package

orig_f = r"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\romfs\BmGame\CookedSwitch\Max_IntroParty_LOC_INT.xxx"
with open(orig_f, "rb") as f: d_orig = decompress_package(f.read())
e_orig = d_orig[4484:4484+22167]
nat_orig = e_orig[0x0F7D:]

print("First 128 bytes of nat_orig:")
print(nat_orig[:128].hex(" "))
print("\nUnpacked as ints:")
for i in range(0, 64, 4):
    val = struct.unpack_from("<i", nat_orig, i)[0]
    uval = struct.unpack_from("<I", nat_orig, i)[0]
    print(f"[{i:02d}] = {val} (0x{uval:08X})")
