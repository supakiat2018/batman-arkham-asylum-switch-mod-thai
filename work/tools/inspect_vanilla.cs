using System;
using System.IO;

class VanillaInspector
{
    static void Main(string[] args)
    {
        var p = Package.Load(args[0]);
        var bf = BmFontData.Load(p, int.Parse(args[1]));
        Console.WriteLine(Path.GetFileName(args[0]) + " exp " + args[1] + ": Chars=" + bf.Chars.Count + ", Textures=" + bf.Textures.Count + ", Kerning=" + bf.Kerning);
        foreach (int t in bf.Textures)
        {
            var tx = BmTexture.Load(p, t);
            Console.WriteLine(string.Format("  Tex {0}: {1}x{2}, format={3}, mipDataLen={4}", t, tx.SizeX, tx.SizeY, tx.Format, tx.MipDataLen));
        }
    }
}
