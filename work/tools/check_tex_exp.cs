
using System;

class CheckTexExp
{
    static void Main()
    {
        string vanillaPath = @"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\romfs\BmGame\CookedSwitch\Startup.xxx";
        var p = Package.Load(vanillaPath);
        var bf = BmFontData.Load(p, 6);
        Console.WriteLine("Font 6 Tex 0 export index: " + bf.Textures[0]);
        var tx = BmTexture.Load(p, bf.Textures[0]);
        Console.WriteLine(string.Format("SizeX={0}, SizeY={1}, Format={2}, NativeStart={3}, MipDataPos={4}, MipDataLen={5}",
            tx.SizeX, tx.SizeY, tx.Format, tx.NativeStart, tx.MipDataPos, tx.MipDataLen));
    }
}
