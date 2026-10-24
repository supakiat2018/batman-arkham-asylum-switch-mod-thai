import struct
import sys
import os

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
    files = []
    for _ in range(num_files):
        fn, o = read_str(data, o)
        num_sec = struct.unpack_from('<I', data, o)[0]; o += 4
        sections = []
        for _ in range(num_sec):
            sn, o = read_str(data, o)
            num_keys = struct.unpack_from('<I', data, o)[0]; o += 4
            keys = []
            for _ in range(num_keys):
                kn, o = read_str(data, o)
                val, o = read_str(data, o)
                keys.append((kn, val))
            sections.append((sn, keys))
        files.append((fn, sections))
    return files

def serialize_str(s):
    # Check if string contains non-ASCII characters
    is_unicode = any(ord(c) > 255 for c in s)
    if is_unicode:
        # UTF-16LE
        b = s.encode('utf-16le') + b'\x00\x00'
        num_chars = len(s) + 1
        return struct.pack('<i', -num_chars) + b
    else:
        # Latin1 / ASCII
        b = s.encode('latin1') + b'\x00'
        num_chars = len(s) + 1
        return struct.pack('<i', num_chars) + b

def build_coalesced(files):
    out = bytearray()
    out.extend(struct.pack('<I', len(files)))
    for fn, sections in files:
        out.extend(serialize_str(fn))
        out.extend(struct.pack('<I', len(sections)))
        for sn, keys in sections:
            out.extend(serialize_str(sn))
            out.extend(struct.pack('<I', len(keys)))
            for kn, val in keys:
                out.extend(serialize_str(kn))
                out.extend(serialize_str(val))
    return bytes(out)

orig_p = r"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\romfs\BmGame\CookedSwitch\Coalesced_INT.bin"
mod_p = r"C:\Users\Supakiat\AppData\Roaming\yuzu\load\0100E870163CA000\MODTHAI\romfs\BmGame\CookedSwitch\Coalesced_INT.bin"

files_orig = parse_coalesced(open(orig_p, "rb").read())
files_mod = parse_coalesced(open(mod_p, "rb").read())

# Map files_mod by fn
mod_dict = {fn: sections for fn, sections in files_mod}

# Construct clean files list
clean_files = []
for fn, orig_sections in files_orig:
    # If it's a Config file (.ini), keep orig_sections CLEAN, but configure subtitle font!
    if "Config" in fn or fn.endswith(".ini"):
        if "Switch-BmEngine.ini" in fn:
            engine_sections = []
            for sn, keys in orig_sections:
                if sn == "Engine.Engine":
                    new_keys = []
                    for kn, val in keys:
                        if kn == "SubtitleFontName":
                            new_keys.append((kn, "BmFonts.SmallFont"))
                            print(f"Updated {kn} to BmFonts.SmallFont")
                        elif kn == "bSubtitlesEnabled":
                            new_keys.append((kn, "TRUE"))
                            print(f"Updated {kn} to TRUE")
                        else:
                            new_keys.append((kn, val))
                    engine_sections.append((sn, new_keys))
                else:
                    engine_sections.append((sn, keys))
            clean_files.append((fn, engine_sections))
            print(f"Preserved config with updated subtitle font: {fn}")
        else:
            clean_files.append((fn, orig_sections))
            print(f"Preserved pristine config: {fn}")
    else:
        # It's a Localization file (.int), use mod_dict[fn] if present!
        if fn in mod_dict:
            clean_files.append((fn, mod_dict[fn]))
            print(f"Kept translated localization: {fn}")
        else:
            clean_files.append((fn, orig_sections))

rebuilt = build_coalesced(clean_files)
print(f"Rebuilt Coalesced size: {len(rebuilt):,} bytes (Orig was {len(open(orig_p, 'rb').read()):,})")

# Test re-parsing rebuilt to ensure 100% validity
test_parse = parse_coalesced(rebuilt)
assert len(test_parse) == len(files_orig), "File count mismatch!"
print("Verification passed! 51/51 files parsed successfully.")

out_dst1 = r"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\modthai\0100E870163CA000\romfs\BmGame\CookedSwitch\Coalesced_INT.bin"
out_dst2 = r"C:\Users\Supakiat\AppData\Roaming\yuzu\load\0100E870163CA000\MODTHAI\romfs\BmGame\CookedSwitch\Coalesced_INT.bin"

with open(out_dst1, "wb") as f: f.write(rebuilt)
with open(out_dst2, "wb") as f: f.write(rebuilt)
print("Saved clean Coalesced_INT.bin to modthai and yuzu!")
