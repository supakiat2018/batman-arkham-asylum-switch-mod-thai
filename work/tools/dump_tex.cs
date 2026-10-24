using System;
using System.IO;

class TextureDumper
{
    static void Main(string[] args)
    {
        var p = Package.Load(args[0]);
        var bf = BmFontData.Load(p, int.Parse(args[1]));
        var tx = BmTexture.Load(p, bf.Textures[0]);
        var px = Dxt5.Decode(p.Data, tx.MipDataPos, tx.SizeX, tx.SizeY);
        using (var bmp = new System.Drawing.Bitmap(tx.SizeX, tx.SizeY))
        {
            for (int y = 0; y < tx.SizeY; y++)
                for (int x = 0; x < tx.SizeX; x++)
                {
                    uint v = px[y * tx.SizeX + x];
                    int a = (int)(v >> 24);
                    int r = (int)(v >> 16 & 255);
                    int g = (int)(v >> 8 & 255);
                    int b = (int)(v & 255);
                    bmp.SetPixel(x, y, System.Drawing.Color.FromArgb(a, r, g, b));
                }
            bmp.Save(args[2], System.Drawing.Imaging.ImageFormat.Png);
        }
        Console.WriteLine("Saved " + args[2]);
    }
}
