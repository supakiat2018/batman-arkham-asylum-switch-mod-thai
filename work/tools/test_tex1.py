import numpy as np
from PIL import Image
from test_roundtrip import deswizzle_bc

with open(r'C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\vanilla_raw_mip_1.bin', 'rb') as f:
    raw_mip = f.read()

width = 256
height = 128
bw = width // 4   # 64
bh = height // 4  # 32

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

for bh_val in [2, 4, 8]:
    desw = deswizzle_bc(raw_mip, bw, bh, block_height=bh_val)
    decoded = decode_dxt5(desw, width, height)
    im = Image.fromarray(decoded, 'RGBA')
    im.save(rf'C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\test_tex1_bh{bh_val}.png')
    print(f'Tex 1 bh={bh_val} saved')
