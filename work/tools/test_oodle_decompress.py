import os
import struct
import ctypes

TOOL_DIR = os.path.dirname(os.path.abspath(__file__))
DLL_PATH = os.path.join(TOOL_DIR, "oo2core_9_win64.dll")

if not os.path.exists(DLL_PATH):
    raise FileNotFoundError(f"Missing {DLL_PATH}")

oodle = ctypes.CDLL(DLL_PATH)

# OodleLZ_Decompress(compBuf, compLen, rawBuf, rawLen, fuzzSafe, checkCRC, verbosity, decBufBase, decBufSize, fpCallback, callbackUserData, decoderMemory, decoderMemorySize, threadPhase)
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
    ret = OodleLZ_Decompress(
        comp_data, len(comp_data),
        out_buf, uncomp_len,
        0, 0, 0,
        None, 0,
        None, None,
        None, 0,
        3
    )
    if ret != uncomp_len:
        raise RuntimeError(f"OodleLZ_Decompress returned {ret}, expected {uncomp_len}")
    return bytes(out_buf)

def decompress_ue3_package(src_path: str, dst_path: str):
    print(f"Reading {src_path}...")
    with open(src_path, "rb") as f:
        raw = f.read()

    magic = struct.unpack_from("<I", raw, 0)[0]
    if magic != 0x9E2A83C1:
        raise ValueError(f"Not a valid UE3 package (magic 0x{magic:08X})")

    ver, lic = struct.unpack_from("<HH", raw, 4)
    print(f"Package version: {ver}/{lic}")

    # Read header fields to locate compression info
    # Standard UE3 Package header:
    # 0: Tag (4)
    # 4: Version (2), Licensee (2)
    # 8: TotalHeaderSize (4)
    # 12: FolderName (FString: length int32 then chars)
    pos = 8
    total_header_size = struct.unpack_from("<I", raw, pos)[0]; pos += 4
    folder_len = struct.unpack_from("<i", raw, pos)[0]; pos += 4
    if folder_len > 0:
        pos += folder_len
    elif folder_len < 0:
        pos += (-folder_len) * 2

    pkg_flags_pos = pos
    pkg_flags = struct.unpack_from("<I", raw, pos)[0]; pos += 4

    name_count, name_offset = struct.unpack_from("<ii", raw, pos); pos += 8
    exp_count, exp_offset = struct.unpack_from("<ii", raw, pos); pos += 8
    imp_count, imp_offset = struct.unpack_from("<ii", raw, pos); pos += 8
    pos += 4 # depends offset

    if ver >= 584:
        pos += 4 # thumbnail table offset
    if ver >= 623:
        pos += 12 # import/export guids

    pos += 16 # GUID
    gens = struct.unpack_from("<i", raw, pos)[0]; pos += 4
    generations_pos = pos
    pos += gens * 12 # generations table (exp_count, name_count, net_obj_count)

    pos += 8 # engine ver, cooker ver

    comp_flags_pos = pos
    comp_flags, chunk_count = struct.unpack_from("<ii", raw, pos); pos += 8
    print(f"Compression flags: {comp_flags}, chunk count: {chunk_count}")

    if chunk_count == 0:
        print("Package is already uncompressed!")
        with open(dst_path, "wb") as f:
            f.write(raw)
        return

    chunks = []
    for _ in range(chunk_count):
        uoff, usize, coff, csize = struct.unpack_from("<iiii", raw, pos)
        pos += 16
        chunks.append({"uoff": uoff, "usize": usize, "coff": coff, "csize": csize})

    tail_start = pos

    total_uncompressed = max(c["uoff"] + c["usize"] for c in chunks)
    decompressed_data = bytearray(total_uncompressed)

    first_coff = min(c["coff"] for c in chunks)

    # Reconstruct uncompressed header:
    # 1. Everything up to comp_flags_pos
    decompressed_data[0:comp_flags_pos] = raw[0:comp_flags_pos]
    hp = comp_flags_pos

    # 2. comp_flags = 0, chunk_count = 0
    struct.pack_into("<ii", decompressed_data, hp, 0, 0); hp += 8

    # 3. Everything between tail_start and first_coff (names, imports, exports header parts if any)
    tail_len = first_coff - tail_start
    decompressed_data[hp:hp + tail_len] = raw[tail_start:first_coff]
    hp += tail_len

    # Clear PKG_StoreCompressed flag (0x02000000)
    pf = struct.unpack_from("<I", decompressed_data, pkg_flags_pos)[0]
    struct.pack_into("<I", decompressed_data, pkg_flags_pos, pf & ~0x02000000)

    # Decompress each chunk
    for idx, c in enumerate(chunks):
        c_pos = c["coff"]
        c_tag, block_size, c_csize, c_usize = struct.unpack_from("<IIii", raw, c_pos)
        if c_tag != 0x9E2A83C1:
            raise ValueError(f"Bad chunk tag 0x{c_tag:08X} at 0x{c_pos:X}")

        num_blocks = (c_usize + block_size - 1) // block_size
        block_table_pos = c_pos + 16
        data_pos = block_table_pos + num_blocks * 8
        out_pos = c["uoff"]

        print(f"Chunk {idx+1}/{chunk_count}: {num_blocks} blocks, uncompressed size {c_usize} bytes")
        for b in range(num_blocks):
            cs, us = struct.unpack_from("<ii", raw, block_table_pos + b * 8)
            comp_block = raw[data_pos:data_pos + cs]
            uncomp_block = decompress_oodle_block(comp_block, us)
            decompressed_data[out_pos:out_pos + us] = uncomp_block
            data_pos += cs
            out_pos += us

    print(f"Writing decompressed package ({len(decompressed_data)} bytes) to {dst_path}...")
    with open(dst_path, "wb") as f:
        f.write(decompressed_data)
    print("Done! Successfully decompressed.")

if __name__ == "__main__":
    src = r"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\romfs\BmGame\CookedSwitch\CommonGame_LOC_INT.xxx"
    dst = r"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\decompressed\CommonGame_LOC_INT.xxx"
    decompress_ue3_package(src, dst)
