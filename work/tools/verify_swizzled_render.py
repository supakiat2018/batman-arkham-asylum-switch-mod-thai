import numpy as np
from PIL import Image
import sys
sys.path.append(r'C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools')
from test_roundtrip import deswizzle_bc
from test_tex1 import decode_dxt5

# Let's read the mip data of the baked texture from test_swizzled_startup_f6.xxx
with open(r'C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\output\test_swizzled_startup_f6.xxx', 'rb') as f:
    d = f.read()

# We can find the texture by looking at the end of the file (since it was appended)
# Size is 512x512 DXT5 = 262144 bytes
# Look for DXT5 data of length 262144
target_len = 512 * 512
# The texture mip was appended near the end before the font export
# Let's inspect BmTexture using C# or read via Package
code = """
using System;
using System.IO;

class DumpBakedTex
{
    static void Main()
    {
        var p = Package.Load(@"C:\\Users\\Supakiat\\Downloads\\Compressed\\Batman Arkham Asylum\\work\\output\\test_swizzled_startup_f6.xxx");
        var bf = BmFontData.Load(p, 6);
        var tx = BmTexture.Load(p, bf.Textures[0]);
        byte[] raw = new byte[tx.MipDataLen];
        Buffer.BlockCopy(p.Data, tx.MipDataPos, raw, 0, tx.MipDataLen);
        File.WriteAllBytes(@"C:\\Users\\Supakiat\\Downloads\\Compressed\\Batman Arkham Asylum\\work\\baked_raw_mip.bin", raw);
        Console.WriteLine(string.Format("Dumped baked tex: {0}x{1}, len={2}", tx.SizeX, tx.SizeY, tx.MipDataLen));
    }
}
"""
with open(r'C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools\dump_baked.cs', 'w') as f:
    f.write(code)
