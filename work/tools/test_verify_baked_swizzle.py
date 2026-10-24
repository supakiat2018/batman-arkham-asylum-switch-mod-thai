import numpy as np
from PIL import Image

with open(r'C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\baked_raw_mip.bin', 'rb') as f:
    raw_mip = f.read()

width = 512
height = 512
bw = width // 4   # 128
bh = height // 4  # 128
bytes_per_block = 16

gob_w = 4
gob_h = 8
gob_size = 512

def get_gob_offset(gx, gy):
    return (
        (gx & 0x0F) |
        ((gy & 1) << 4) |
        (((gx >> 4) & 1) << 5) |
        (((gy >> 1) & 1) << 6) |
        (((gy >> 2) & 1) << 7) |
        (((gx >> 5) & 1) << 8)
    )

def deswizzle_bc(swizzled_bytes, bw, bh, block_height=16):
    block_h = gob_h * block_height
    block_size = gob_size * block_height
    width_in_gobs = (bw + gob_w - 1) // gob_w
    height_in_blocks = (bh + block_h - 1) // block_h
    linear = bytearray(bw * bh * bytes_per_block)
    for by in range(height_in_blocks):
        for bx in range(width_in_gobs):
            for gy in range(block_height):
                block_start = (by * width_in_gobs + bx) * block_size + gy * gob_size
                y_base = by * block_h + gy * gob_h
                x_base = bx * gob_w
                for y_in_gob in range(8):
                    block_y = y_base + y_in_gob
                    if block_y >= bh: continue
                    for x_in_gob in range(gob_w):
                        block_x = x_base + x_in_gob
                        if block_x >= bw: continue
                        gx = x_in_gob * bytes_per_block
                        offset_in_gob = get_gob_offset(gx, y_in_gob)
                        src_idx = block_start + offset_in_gob
                        dst_idx = (block_y * bw + block_x) * bytes_per_block
                        linear[dst_idx : dst_idx + bytes_per_block] = swizzled_bytes[src_idx : src_idx + bytes_per_block]
    return bytes(linear)

def decode_dxt5(linear_bytes, w, h):
    px = np.zeros((h, w, 4), dtype=np.uint8)
    p = 0
    for by in range(0, h, 4):
        for bx in range(0, w, 4):
            a0 = linear_bytes[p]; a1 = linear_bytes[p+1]
            abits = int.from_bytes(linear_bytes[p+2:p+8], 'little')
            al = [a0, a1, 0, 0, 0, 0, 0, 0]
            if a0 > a1:
                for i in range(1, 7): al[i+1] = ((7 - i)*a0 + i*a1)//7
            else:
                for i in range(1, 5): al[i+1] = ((5 - i)*a0 + i*a1)//5
                al[6] = 0; al[7] = 255
            c0 = int.from_bytes(linear_bytes[p+8:p+10], 'little')
            c1 = int.from_bytes(linear_bytes[p+10:p+12], 'little')
            cb = int.from_bytes(linear_bytes[p+12:p+16], 'little')
            def c565(c):
                return [((c >> 11) & 31) * 255 // 31, ((c >> 5) & 63) * 255 // 63, (c & 31) * 255 // 31]
            col = [c565(c0), c565(c1), [0,0,0], [0,0,0]]
            for i in range(3):
                col[2][i] = (2*col[0][i] + col[1][i])//3
                col[3][i] = (col[0][i] + 2*col[1][i])//3
            for y in range(4):
                for x in range(4):
                    if by + y < h and bx + x < w:
                        idx = y*4 + x
                        a = al[(abits >> (3*idx)) & 7]
                        c = col[(cb >> (2*idx)) & 3]
                        px[by+y, bx+x] = [c[0], c[1], c[2], a]
            p += 16
    return px

desw = deswizzle_bc(raw_mip, bw, bh, block_height=16)
decoded = decode_dxt5(desw, width, height)
im = Image.fromarray(decoded, 'RGBA')
im.save(r'C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\baked_deswizzled_atlas.png')
print('Successfully deswizzled baked atlas! Saved to baked_deswizzled_atlas.png')
