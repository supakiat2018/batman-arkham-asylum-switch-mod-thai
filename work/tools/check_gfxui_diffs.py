import sys
sys.path.append(r'C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools')
sys.stdout.reconfigure(encoding='utf-8')
from list_coalesced_diffs import parse_coalesced

orig_p = r'C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\romfs\BmGame\CookedSwitch\Coalesced_INT.bin'
mod_p = r'C:\Users\Supakiat\AppData\Roaming\yuzu\load\0100E870163CA000\MODTHAI\romfs\BmGame\CookedSwitch\Coalesced_INT.bin'

tree_orig = parse_coalesced(open(orig_p, 'rb').read())
tree_mod = parse_coalesced(open(mod_p, 'rb').read())

fn = '..\\BmGame\\Localization\\Int\\GFxUI.int'
for sn in tree_orig[fn]:
    orig_kv = dict(tree_orig[fn][sn])
    mod_kv = dict(tree_mod[fn][sn])
    for k in orig_kv:
        if k in mod_kv and mod_kv[k] != orig_kv[k]:
            if 'CONTINUE' in orig_kv[k].upper() or 'A' in k.upper() or 'BUTTON' in k.upper() or 'SELECT' in orig_kv[k].upper():
                print(f'[{sn}] {k} = {repr(orig_kv[k])} -> {repr(mod_kv[k])}')
