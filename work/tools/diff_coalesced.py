import struct

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

print(f"Orig files: {len(tree_orig)}")
print(f"Mod files:  {len(tree_mod)}")

diffs = []
for fn in tree_orig:
    if fn not in tree_mod:
        diffs.append(f"File missing in mod: {fn}")
        continue
    for sn in tree_orig[fn]:
        if sn not in tree_mod[fn]:
            diffs.append(f"Section missing in mod: [{fn}] {sn}")
            continue
        orig_kv = tree_orig[fn][sn]
        mod_kv = tree_mod[fn][sn]
        if orig_kv != mod_kv:
            diffs.append(f"Diff in [{fn}] {sn}: orig={len(orig_kv)} keys, mod={len(mod_kv)} keys")
            # print differences
            orig_d = dict(orig_kv)
            mod_d = dict(mod_kv)
            for k in orig_d:
                if k not in mod_d:
                    print(f"  Key missing: {k}")
                elif orig_d[k] != mod_d[k]:
                    print(f"  [{fn}] [{sn}] {k}:\n    ORIG: {orig_d[k][:60]}\n    MOD:  {mod_d[k][:60]}")

print(f"\nTotal section diffs: {len(diffs)}")
