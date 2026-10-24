import struct
import sys

sys.stdout.reconfigure(encoding="utf-8")

def parse_coalesced(data):
    def read_str(d, o):
        l = struct.unpack_from('<i', d, o)[0]; o += 4
        if l > 0:
            s = d[o:o+l-1].decode('latin1', errors='replace')
            return s, o + l
        elif l < 0:
            l = -l * 2
            s = d[o:o+l-2].decode('utf-16le', errors='replace')
            return s, o + l
        return '', o

    o = 0
    num_files = struct.unpack_from('<I', data, o)[0]; o += 4
    tree = {}
    for _ in range(num_files):
        fn, o = read_str(data, o)
        num_sec = struct.unpack_from('<I', data, o)[0]; o += 4
        tree[fn] = {}
        for _ in range(num_sec):
            sn, o = read_str(data, o)
            num_keys = struct.unpack_from('<I', data, o)[0]; o += 4
            tree[fn][sn] = []
            for _ in range(num_keys):
                kn, o = read_str(data, o)
                val, o = read_str(data, o)
                tree[fn][sn].append((kn, val))
    return tree

orig_p = r"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\romfs\BmGame\CookedSwitch\Coalesced_INT.bin"
mod_p = r"C:\Users\Supakiat\AppData\Roaming\yuzu\load\0100E870163CA000\MODTHAI\romfs\BmGame\CookedSwitch\Coalesced_INT.bin"

with open(orig_p, "rb") as f: d_orig = f.read()
with open(mod_p, "rb") as f: d_mod = f.read()

tree_orig = parse_coalesced(d_orig)
tree_mod = parse_coalesced(d_mod)

print("Files modified in Coalesced_INT.bin:")
for fn in tree_orig:
    if fn in tree_mod and tree_orig[fn] != tree_mod[fn]:
        print(f"  {fn}")
