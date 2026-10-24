import struct
import sys

sys.stdout.reconfigure(encoding="utf-8")

from list_coalesced_diffs import parse_coalesced

orig_p = r"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\romfs\BmGame\CookedSwitch\Coalesced_INT.bin"
mod_p = r"C:\Users\Supakiat\AppData\Roaming\yuzu\load\0100E870163CA000\MODTHAI\romfs\BmGame\CookedSwitch\Coalesced_INT.bin"

with open(orig_p, "rb") as f: d_orig = f.read()
with open(mod_p, "rb") as f: d_mod = f.read()

tree_orig = parse_coalesced(d_orig)
tree_mod = parse_coalesced(d_mod)

for fn in ["..\\BmGame\\Config\\Switch-BmInput.ini", "..\\BmGame\\Config\\Switch-BmGame.ini"]:
    print(f"=== {fn} ===")
    for sn in tree_orig[fn]:
        orig_kv = dict(tree_orig[fn][sn])
        mod_kv = dict(tree_mod[fn][sn])
        for k, ov in orig_kv.items():
            if k in mod_kv and mod_kv[k] != ov:
                print(f"[{sn}] {k} = {repr(ov)} -> {repr(mod_kv[k])}")
