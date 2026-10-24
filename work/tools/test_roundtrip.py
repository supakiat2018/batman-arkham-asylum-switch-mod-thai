import numpy as np

bytes_per_block = 16
gob_w = 64 // bytes_per_block # 4 blocks
gob_h = 8                     # 8 blocks
gob_size = 512                # bytes

def get_gob_offset(gx, gy):
    # gx is in bytes [0..63], gy is in rows [0..7]
    return (
        (gx & 0x0F) |
        ((gy & 1) << 4) |
        (((gx >> 4) & 1) << 5) |
        (((gy >> 1) & 1) << 6) |
        (((gy >> 2) & 1) << 7) |
        (((gx >> 5) & 1) << 8)
    )

def swizzle_bc(linear_bytes, bw, bh, block_height=8):
    block_h = gob_h * block_height
    block_size = gob_size * block_height
    
    width_in_gobs = (bw + gob_w - 1) // gob_w
    height_in_blocks = (bh + block_h - 1) // block_h
    
    swizzled = bytearray(width_in_gobs * height_in_blocks * block_size)
    
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
                        
                        gx = x_in_gob * bytes_per_block
                        offset_in_gob = get_gob_offset(gx, y_in_gob)
                        
                        src_idx = (block_y * bw + block_x) * bytes_per_block
                        dst_idx = block_start + offset_in_gob
                        
                        swizzled[dst_idx : dst_idx + bytes_per_block] = linear_bytes[src_idx : src_idx + bytes_per_block]
                        
    return bytes(swizzled)

def deswizzle_bc(swizzled_bytes, bw, bh, block_height=8):
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
                    if block_y >= bh:
                        continue
                    for x_in_gob in range(gob_w):
                        block_x = x_base + x_in_gob
                        if block_x >= bw:
                            continue
                        
                        gx = x_in_gob * bytes_per_block
                        offset_in_gob = get_gob_offset(gx, y_in_gob)
                        
                        src_idx = block_start + offset_in_gob
                        dst_idx = (block_y * bw + block_x) * bytes_per_block
                        
                        linear[dst_idx : dst_idx + bytes_per_block] = swizzled_bytes[src_idx : src_idx + bytes_per_block]
                        
    return bytes(linear)

# Test roundtrip
test_data = bytes([i % 256 for i in range(64 * 64 * 16)])
sw = swizzle_bc(test_data, 64, 64, block_height=8)
desw = deswizzle_bc(sw, 64, 64, block_height=8)
print('Roundtrip match:', test_data == desw)
