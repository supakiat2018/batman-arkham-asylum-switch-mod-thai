
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

class DumpVanilla
{
    static void Main()
    {
        // Load vanilla Startup.xxx from romfs (decompressed or compressed?)
        // Let's check decompressed Startup
        string vanillaPath = @"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\romfs\BmGame\CookedSwitch\Startup.xxx";
        // Wait, is romfs Startup.xxx compressed?
        // Let's load with Package.Load
        var p = Package.Load(vanillaPath);
        var bf = BmFontData.Load(p, 6);
        Console.WriteLine("Font 6 textures count: " + bf.Textures.Count);
        for (int i = 0; i < bf.Textures.Count; i++)
        {
            var tx = BmTexture.Load(p, bf.Textures[i]);
            Console.WriteLine(string.Format("Tex {0}: {1}x{2}, format {3}, mdataLen {4}", i, tx.SizeX, tx.SizeY, tx.Format, tx.MipDataLen));
            var px = Dxt5.Decode(p.Data, tx.MipDataPos, tx.SizeX, tx.SizeY);
            var bmp = new Bitmap(tx.SizeX, tx.SizeY, PixelFormat.Format32bppArgb);
            for (int y = 0; y < tx.SizeY; y++)
                for (int x = 0; x < tx.SizeX; x++)
                    bmp.SetPixel(x, y, Color.FromArgb((int)px[y * tx.SizeX + x]));
            bmp.Save(string.Format(@"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\vanilla_tex_{0}.png", i));
            Console.WriteLine("Saved vanilla_tex_" + i + ".png");

            // Also dump raw mip bytes
            byte[] rawMip = new byte[tx.MipDataLen];
            Buffer.BlockCopy(p.Data, tx.MipDataPos, rawMip, 0, tx.MipDataLen);
            File.WriteAllBytes(string.Format(@"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\vanilla_raw_mip_{0}.bin", i), rawMip);
        }
    }
}
