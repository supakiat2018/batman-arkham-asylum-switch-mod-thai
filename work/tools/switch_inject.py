import os
import sys
import struct
import ctypes

TOOL_DIR = os.path.dirname(os.path.abspath(__file__))
DLL_PATH = os.path.join(TOOL_DIR, "oo2core_9_win64.dll")

oodle = ctypes.CDLL(DLL_PATH)
OodleLZ_Decompress = oodle.OodleLZ_Decompress
OodleLZ_Decompress.argtypes = [
    ctypes.c_char_p, ctypes.c_int64,
    ctypes.c_char_p, ctypes.c_int64,
    ctypes.c_int, ctypes.c_int, ctypes.c_int,
    ctypes.c_void_p, ctypes.c_int64,
    ctypes.c_void_p, ctypes.c_void_p,
    ctypes.c_void_p, ctypes.c_int64,
    ctypes.c_int
]
OodleLZ_Decompress.restype = ctypes.c_int64

def decompress_oodle_block(comp_data: bytes, uncomp_len: int) -> bytes:
    out_buf = ctypes.create_string_buffer(uncomp_len)
    ret = OodleLZ_Decompress(comp_data, len(comp_data), out_buf, uncomp_len, 0, 0, 0, None, 0, None, None, None, 0, 3)
    if ret != uncomp_len:
        raise RuntimeError(f"Oodle decompress failed: got {ret}, expected {uncomp_len}")
    return bytes(out_buf)

def decompress_package(raw: bytes) -> bytearray:
    magic = struct.unpack_from("<I", raw, 0)[0]
    if magic != 0x9E2A83C1:
        raise ValueError(f"Invalid package magic: 0x{magic:08X}")
    ver = struct.unpack_from("<H", raw, 4)[0]

    pos = 8
    pos += 4 # total header size
    flen = struct.unpack_from("<i", raw, pos)[0]; pos += 4
    if flen > 0: pos += flen
    elif flen < 0: pos += (-flen)*2

    pkg_flags_pos = pos
    pos += 4 # pkg_flags
    pos += 24 # name, exp, imp counts and offsets
    pos += 4 # depends
    if ver >= 584: pos += 4
    if ver >= 623: pos += 12
    pos += 16 # GUID
    gens = struct.unpack_from("<i", raw, pos)[0]; pos += 4
    pos += gens * 12 # generations
    pos += 8 # engine ver, cooker ver

    comp_flags_pos = pos
    comp_flags, chunk_count = struct.unpack_from("<ii", raw, pos); pos += 8

    if chunk_count == 0:
        return bytearray(raw)

    chunks = []
    for _ in range(chunk_count):
        uoff, usize, coff, csize = struct.unpack_from("<iiii", raw, pos); pos += 16
        chunks.append({"uoff": uoff, "usize": usize, "coff": coff, "csize": csize})

    tail_start = pos
    total_uncompressed = max(c["uoff"] + c["usize"] for c in chunks)
    decomp = bytearray(total_uncompressed)
    first_coff = min(c["coff"] for c in chunks)

    decomp[0:comp_flags_pos] = raw[0:comp_flags_pos]
    hp = comp_flags_pos
    struct.pack_into("<ii", decomp, hp, 0, 0); hp += 8 # comp_flags = 0, chunk_count = 0
    tail_len = first_coff - tail_start
    decomp[hp:hp + tail_len] = raw[tail_start:first_coff]

    # clear PKG_StoreCompressed (0x02000000)
    pf = struct.unpack_from("<I", decomp, pkg_flags_pos)[0]
    struct.pack_into("<I", decomp, pkg_flags_pos, pf & ~0x02000000)

    for c in chunks:
        c_pos = c["coff"]
        c_tag, block_size, c_csize, c_usize = struct.unpack_from("<IIii", raw, c_pos)
        num_blocks = (c_usize + block_size - 1) // block_size
        bp = c_pos + 16
        data_pos = bp + num_blocks * 8
        out_pos = c["uoff"]

        for b in range(num_blocks):
            cs, us = struct.unpack_from("<ii", raw, bp + b * 8)
            decomp[out_pos:out_pos + us] = decompress_oodle_block(raw[data_pos:data_pos + cs], us)
            data_pos += cs
            out_pos += us

    return decomp

# Thai Shaper implementation
NOP, SD, SL, SDL, RD = 0, 1, 2, 3, 4
def cons_type(c):
    if c in 'ปฝฟฬ': return 1
    if c in 'ญฐ': return 2
    if c in 'ฎฏ': return 3
    return 0 if 'ก' <= c <= 'ฮ' else 4

def mark_type(c):
    if c in 'ัิีึื็ํ': return 0
    if c in 'ฺุู': return 1
    if c in '่้๊๋์': return 2
    return 3

Above = [
    [ [NOP,3], [NOP,0], [SD,3], [NOP,3] ],
    [ [SL,2],  [NOP,1], [SDL,2],[NOP,3] ],
    [ [NOP,3], [NOP,2], [SL,3], [NOP,3] ],
    [ [NOP,3], [NOP,3], [NOP,3],[NOP,3] ]
]
AboveStart = [0, 1, 0, 0, 3]
Below = [
    [ [NOP,0], [NOP,2], [NOP,0], [NOP,2] ],
    [ [NOP,1], [RD,2],  [NOP,1], [NOP,2] ],
    [ [NOP,2], [SD,2],  [NOP,2], [NOP,2] ]
]
BelowStart = [0, 0, 2, 2, 2]

def map_char(c, action):
    if action == SD:
        m = {'่':'', '้':'', '๊':'', '๋':'', '์':'', 'ุ':'', 'ู':'', 'ฺ':''}
        return m.get(c, c)
    elif action == SDL:
        m = {'่':'', '้':'', '๊':'', '๋':'', '์':''}
        return m.get(c, c)
    elif action == SL:
        m = {'่':'', '้':'', '๊':'', '๋':'', '์':'', 'ั':'', 'ิ':'', 'ี':'', 'ึ':'', 'ื':'', '็':'', 'ํ':''}
        return m.get(c, c)
    elif action == RD:
        m = {'ญ':'', 'ฐ':''}
        return m.get(c, c)
    return c

def shape_thai(text: str) -> str:
    res = []
    a_st = 3
    b_st = 2
    for c in text:
        ct = cons_type(c)
        if ct != 4:
            a_st = AboveStart[ct]
            b_st = BelowStart[ct]
            res.append(c)
        else:
            mt = mark_type(c)
            if mt == 0 or mt == 2:
                act, a_st = Above[a_st][mt]
                res.append(map_char(c, act))
            elif mt == 1:
                act, b_st = Below[b_st][mt]
                res.append(map_char(c, act))
            else:
                a_st = 3
                b_st = 2
                res.append(c)
    return "".join(res)

def unescape(s: str) -> str:
    return s.replace("\\n", "\n").replace("\\r", "\r").replace("\\t", "\t")

class StrRef:
    def __init__(self, path, text, size_pos_list, val_pos, val_len):
        self.path = path
        self.text = text
        self.size_pos = size_pos_list
        self.val_pos = val_pos
        self.val_len = val_len

def collect_strings(data, o, end, path, size_stack, names):
    refs = []
    while o + 8 <= end:
        n_idx, n_num = struct.unpack_from("<ii", data, o)
        if n_idx < 0 or n_idx >= len(names): break
        name = names[n_idx]
        if name == "None":
            return refs, o + 8
        if o + 26 > end: break
        t_idx, t_num = struct.unpack_from("<ii", data, o + 8)
        if t_idx < 0 or t_idx >= len(names): break
        ptype = names[t_idx]
        size = struct.unpack_from("<i", data, o + 16)[0]
        size_pos = o + 16
        inner_sizes = size_stack + [size_pos]
        v = o + 26
        if ptype == "StructProperty":
            v += 8
        real = 4 if ptype == "BoolProperty" else size

        if ptype == "StrProperty":
            sl = struct.unpack_from("<i", data, v)[0]
            if sl > 0:
                t = data[v+4:v+4+sl-1].decode("latin1", "replace")
            elif sl < 0:
                t = data[v+4:v+4+(-sl-1)*2].decode("utf-16le", "replace")
            else:
                t = ""
            refs.append(StrRef(path + name, t, inner_sizes, v, 4 + (sl if sl > 0 else (-sl)*2)))
        elif ptype == "ArrayProperty":
            count = struct.unpack_from("<i", data, v)[0]
            elem_p = v + 4
            for c in range(count):
                sub_refs, elem_p = collect_strings(data, elem_p, v + size, f"{path}{name}[{c}].", inner_sizes, names)
                refs.extend(sub_refs)
                if elem_p is None: break
        elif ptype == "StructProperty":
            sub_refs, _ = collect_strings(data, v, v + size, f"{path}{name}.", inner_sizes, names)
            refs.extend(sub_refs)

        o = v + real
    return refs, o

def inject_package(src_data: bytearray, translations: dict) -> bytearray:
    d = bytearray(src_data)

    pos = 8
    pos += 4 # header size
    flen = struct.unpack_from("<i", d, pos)[0]; pos += 4
    if flen > 0: pos += flen
    elif flen < 0: pos += (-flen)*2
    pos += 4 # pkg_flags
    name_count, name_offset = struct.unpack_from("<ii", d, pos); pos += 8
    exp_count, exp_offset = struct.unpack_from("<ii", d, pos); pos += 8
    imp_count, imp_offset = struct.unpack_from("<ii", d, pos); pos += 8

    names = []
    p = name_offset
    for _ in range(name_count):
        sl = struct.unpack_from("<i", d, p)[0]; p += 4
        if sl > 0:
            names.append(d[p:p+sl-1].decode("latin1"))
            p += sl
        elif sl < 0:
            names.append(d[p:p+(-sl-1)*2].decode("utf-16le"))
            p += (-sl)*2
        p += 8

    def get_name(idx, num=0):
        if 0 <= idx < len(names):
            s = names[idx]
            return f"{s}_{num-1}" if num > 0 else s
        return f"<bad_{idx}>"

    imports = []
    p = imp_offset
    for _ in range(imp_count):
        cp_idx, cp_num = struct.unpack_from("<ii", d, p); p += 8
        cn_idx, cn_num = struct.unpack_from("<ii", d, p); p += 8
        outer = struct.unpack_from("<i", d, p)[0]; p += 4
        n_idx, n_num = struct.unpack_from("<ii", d, p); p += 8
        imports.append({"name": get_name(n_idx, n_num), "outer": outer})

    exports = []
    p = exp_offset
    for _ in range(exp_count):
        c_idx, s_idx, outer_idx = struct.unpack_from("<iii", d, p); p += 12
        n_idx, n_num = struct.unpack_from("<ii", d, p); p += 8
        arch = struct.unpack_from("<i", d, p)[0]; p += 4
        flags = struct.unpack_from("<Q", d, p)[0]; p += 8
        size_field_pos = p
        ssize, soffset = struct.unpack_from("<ii", d, p); p += 8
        p += 4
        net = struct.unpack_from("<i", d, p)[0]; p += 4 + net * 4
        p += 16 + 4
        exports.append({
            "name": get_name(n_idx, n_num),
            "outer_idx": outer_idx,
            "size_field_pos": size_field_pos,
            "serial_size": ssize,
            "serial_offset": soffset
        })

    def full_name(obj_idx):
        parts = []
        curr = obj_idx
        while curr != 0:
            if curr > 0:
                parts.append(exports[curr - 1]["name"])
                curr = exports[curr - 1]["outer_idx"]
            else:
                parts.append(imports[-curr - 1]["name"])
                curr = imports[-curr - 1]["outer"]
        parts.reverse()
        return ".".join(parts)

    changed_exports = 0
    changed_lines = 0
    out_data = bytearray(d)

    import re
    loc_rx = re.compile(r"^LocalizedSubtitles\[\d+\]\.")

    for i in range(exp_count):
        e = exports[i]
        if e["serial_size"] < 12: continue

        st, _ = collect_strings(d, e["serial_offset"] + 4, e["serial_offset"] + e["serial_size"], "", [], names)
        if not st: continue

        fname = full_name(i + 1)
        mine = []
        target_texts = {}

        for s in st:
            clean_path = loc_rx.sub("", s.path)
            th = translations.get(f"{fname}|{s.path}") or translations.get(f"{fname}|{clean_path}")
            if th:
                mine.append(s)
                target_texts[s] = th

        if not mine: continue

        # Sort from back to front
        mine.sort(key=lambda s: s.val_pos, reverse=True)
        ex = bytearray(d[e["serial_offset"]:e["serial_offset"] + e["serial_size"]])
        total_delta = 0
        last_end = 0

        for s in mine:
            raw_th = unescape(target_texts[s])
            shaped = shape_thai(raw_th)
            utf16_bytes = shaped.encode("utf-16le") + b"\x00\x00"
            num_chars = len(shaped) + 1
            new_fs = struct.pack("<i", -num_chars) + utf16_bytes

            rel = s.val_pos - e["serial_offset"]
            last_end = max(last_end, rel + s.val_len)
            delta = len(new_fs) - s.val_len

            # Replace in export buffer
            nx = bytearray(len(ex) + delta)
            nx[:rel] = ex[:rel]
            nx[rel:rel + len(new_fs)] = new_fs
            nx[rel + len(new_fs):] = ex[rel + s.val_len:]

            # Adjust enclosing sizes
            for sp in s.size_pos:
                r = sp - e["serial_offset"]
                old_sz = struct.unpack_from("<i", nx, r)[0]
                struct.pack_into("<i", nx, r, old_sz + delta)

            ex = nx
            total_delta += delta
            changed_lines += 1

        new_offset = len(out_data)

        # Fix bulk data absolute file offsets inside relocated export
        # Bulk data header: flags (4), count (4), sizeOnDisk (4), offsetInFile (4)
        for r in range(last_end + total_delta + 12, len(ex) - 4):
            stored = struct.unpack_from("<i", ex, r)[0]
            old_rel = r + 4 - total_delta
            if stored == e["serial_offset"] + old_rel:
                sz = struct.unpack_from("<i", ex, r - 4)[0]
                if 0 <= sz and r + 4 + sz <= len(ex):
                    struct.pack_into("<i", ex, r, new_offset + r + 4)

        out_data.extend(ex)
        # Update export table entry with new size and new offset
        struct.pack_into("<i", out_data, e["size_field_pos"], len(ex))
        struct.pack_into("<i", out_data, e["size_field_pos"] + 4, new_offset)
        changed_exports += 1

    return out_data if changed_exports > 0 else None
