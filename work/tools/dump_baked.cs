using System;
using System.IO;

class DumpBakedTex
{
    static void Main()
    {
        var p = Package.Load(@"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\output\test_swizzled_startup_f6.xxx");
        var bf = BmFontData.Load(p, 6);
        var tx = BmTexture.Load(p, bf.Textures[0]);
        byte[] raw = new byte[tx.MipDataLen];
        Buffer.BlockCopy(p.Data, tx.MipDataPos, raw, 0, tx.MipDataLen);
        File.WriteAllBytes(@"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\baked_raw_mip.bin", raw);
        Console.WriteLine(string.Format("Dumped baked tex: {0}x{1}, len={2}", tx.SizeX, tx.SizeY, tx.MipDataLen));
    }
}
