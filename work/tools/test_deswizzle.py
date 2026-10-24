import struct
import numpy as np
from PIL import Image

# Read vanilla raw mip 0
with open(r'C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\vanilla_raw_mip_0.bin', 'rb') as f:
    raw_mip = f.read()

width = 256
height = 256
# For DXT5, each 4x4 block is 16 bytes
bw = width // 4   # 64 blocks
bh = height // 4  # 64 blocks
bytes_per_block = 16

# Let's test standard Tegra block linear deswizzle for BC textures
# In Tegra, GOB is 64 bytes wide x 8 rows high
# For 16 bytes per block, gob_w = 4 blocks, gob_h = 8 blocks
gob_w = 64 // bytes_per_block # 4 blocks
gob_h = 8                     # 8 blocks
gob_size = 512                # bytes

def deswizzle_bc(swizzled_bytes, bw, bh, block_height=1):
    block_h = gob_h * block_height
    block_size = gob_size * block_height
    
    width_in_gobs = (bw + gob_w - 1) // gob_w
    height_in_blocks = (bh + block_h - 1) // block_h
    
    linear_blocks = bytearray(bw * bh * bytes_per_block)
    
    for by in range(height_in_blocks):
        for bx in range(width_in_gobs):
            for gy in range(block_height):
                block_start = (by * width_in_gobs + bx) * block_size + gy * gob_size
                y_base = by * block_h + gy * gob_h
                x_base = bx * gob_w
                
                for y_in_gob in range(8):
                    block_y = y_base + y_in_gob
                    if block_y >= bh:
                        continue
                    for x_in_gob in range(gob_w):
                        block_x = x_base + x_in_gob
                        if block_x >= bw:
                            continue
                        
                        # Calculate Tegra GOB offset
                        # byte offset within GOB for x in [0..63], y in [0..7]
                        gx = x_in_gob * bytes_per_block
                        # x: 16-byte aligned
                        offset_in_gob = (
                            (gx & 0x0F) |
                            ((y_in_gob & 1) << 4) |
                            (((gx >> 4) & 1) << 5) |
                            (((y_in_gob >> 1) & 1) << 6) |
                            (((y_in_gob >> 2) & 1) << 7) |
                            (((gx >> 5) & 1) << 8)
                        )
                        src_idx = block_start + offset_in_gob
                        dst_idx = (block_y * bw + block_x) * bytes_per_block
                        linear_blocks[dst_idx : dst_idx + bytes_per_block] = swizzled_bytes[src_idx : src_idx + bytes_per_block]
                        
    return bytes(linear_blocks)

# Let's decode DXT5 blocks to RGBA
def decode_dxt5(linear_bytes, w, h):
    px = np.zeros((h, w, 4), dtype=np.uint8)
    p = 0
    for by in range(0, h, 4):
        for bx in range(0, w, 4):
            # 8 bytes alpha
            a0 = linear_bytes[p]
            a1 = linear_bytes[p+1]
            abits = int.from_bytes(linear_bytes[p+2:p+8], 'little')
            al = [a0, a1, 0, 0, 0, 0, 0, 0]
            if a0 > a1:
                for i in range(1, 7): al[i+1] = ((7 - i)*a0 + i*a1)//7
            else:
                for i in range(1, 5): al[i+1] = ((5 - i)*a0 + i*a1)//5
                al[6] = 0; al[7] = 255
            # 8 bytes color
            c0 = int.from_bytes(linear_bytes[p+8:p+10], 'little')
            c1 = int.from_bytes(linear_bytes[p+10:p+12], 'little')
            cb = int.from_bytes(linear_bytes[p+12:p+16], 'little')
            def c565(c):
                r = ((c >> 11) & 31) * 255 // 31
                g = ((c >> 5) & 63) * 255 // 63
                b = (c & 31) * 255 // 31
                return [r, g, b]
            col = [c565(c0), c565(c1), [0,0,0], [0,0,0]]
            for i in range(3):
                col[2][i] = (2*col[0][i] + col[1][i])//3
                col[3][i] = (col[0][i] + 2*col[1][i])//3
            
            for y in range(4):
                for x in range(4):
                    if by + y < h and bx + x < w:
                        idx = y*4 + x
                        a_idx = (abits >> (3*idx)) & 7
                        c_idx = (cb >> (2*idx)) & 3
                        a = al[a_idx]
                        c = col[c_idx]
                        px[by+y, bx+x] = [c[0], c[1], c[2], a]
            p += 16
    return px

# Test block heights 1, 2, 4, 8, 16
for bh_val in [1, 2, 4, 8, 16]:
    desw = deswizzle_bc(raw_mip, bw, bh, block_height=bh_val)
    decoded = decode_dxt5(desw, width, height)
    im = Image.fromarray(decoded, 'RGBA')
    out_path = rf'C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\test_desw_bh{bh_val}.png'
    im.save(out_path)
    # Check crop at 'H' (113, 62, 20, 18)
    crop = im.crop((113, 62, 113+20, 62+18))
    crop.save(rf'C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\test_desw_crop_H_bh{bh_val}.png')
    print(f'bh={bh_val} saved')
