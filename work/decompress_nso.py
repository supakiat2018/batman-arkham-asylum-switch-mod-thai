import struct, lz4.block

with open(r'C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\exefs\main', 'rb') as f:
    hdr = f.read(0x100)
    
    text_off, text_dst, text_sz = struct.unpack_from('<III', hdr, 0x10)
    ro_off, ro_dst, ro_sz = struct.unpack_from('<III', hdr, 0x20)
    data_off, data_dst, data_sz = struct.unpack_from('<III', hdr, 0x30)
    
    text_comp_sz, ro_comp_sz, data_comp_sz = struct.unpack_from('<III', hdr, 0x60)
    
    f.seek(text_off)
    text_data = f.read(text_comp_sz)
    if text_comp_sz != text_sz:
        text_data = lz4.block.decompress(text_data, uncompressed_size=text_sz)
        
    f.seek(ro_off)
    ro_data = f.read(ro_comp_sz)
    if ro_comp_sz != ro_sz:
        ro_data = lz4.block.decompress(ro_data, uncompressed_size=ro_sz)
        
    f.seek(data_off)
    data_data = f.read(data_comp_sz)
    if data_comp_sz != data_sz:
        data_data = lz4.block.decompress(data_data, uncompressed_size=data_sz)

decomp_path = r'C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\main_decompressed.bin'
with open(decomp_path, 'wb') as f:
    f.write(text_data)
    f.write(ro_data)
    f.write(data_data)

print(f'Decompressed main: text={len(text_data)}, ro={len(ro_data)}, data={len(data_data)}, total={len(text_data)+len(ro_data)+len(data_data)}')
