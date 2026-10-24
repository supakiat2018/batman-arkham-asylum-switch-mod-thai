using System;
using System.IO;

class TexInspector
{
    static void Main(string[] args)
    {
        var p = Package.Load(args[0]);
        var bf = BmFontData.Load(p, int.Parse(args[1]));
        Console.WriteLine("Font " + Path.GetFileName(args[0]) + " chars=" + bf.Chars.Count + ", textures=" + bf.Textures.Count);
        foreach (int t in bf.Textures)
        {
            var tx = BmTexture.Load(p, t);
            Console.WriteLine(string.Format("  Tex exp={0}: {1}x{2}, format={3}, mipDataLen={4}", t, tx.SizeX, tx.SizeY, tx.Format, tx.MipDataLen));
        }
    }
}
