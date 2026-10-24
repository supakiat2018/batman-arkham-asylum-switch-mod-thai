using System;

class InspectNative
{
    static void Main()
    {
        string vanillaPath = @"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\romfs\BmGame\CookedSwitch\Startup.xxx";
        var p = Package.Load(vanillaPath);
        var bf = BmFontData.Load(p, 6);
        var tx = BmTexture.Load(p, bf.Textures[0]);
        int q = tx.NativeStart;
        Console.WriteLine("NativeStart: " + q);
        byte[] bytes = new byte[tx.MipDataPos - tx.NativeStart + 20];
        Buffer.BlockCopy(p.Data, q, bytes, 0, bytes.Length);
        for (int i = 0; i < bytes.Length; i += 16)
        {
            string hex = "";
            for (int j = 0; j < 16 && i + j < bytes.Length; j++) hex += bytes[i + j].ToString("X2") + " ";
            Console.WriteLine(string.Format("{0:X4}: {1}", i, hex));
        }
    }
}
