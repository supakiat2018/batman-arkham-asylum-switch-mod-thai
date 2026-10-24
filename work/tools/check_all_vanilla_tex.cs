
using System;

class CheckAllVanillaTex
{
    static void Main()
    {
        string vanillaPath = @"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\romfs\BmGame\CookedSwitch\Startup.xxx";
        var p = Package.Load(vanillaPath);
        int[] exps = { 4, 5, 6, 7, 8, 9, 20, 21 };
        foreach (int exp in exps)
        {
            var bf = BmFontData.Load(p, exp);
            Console.WriteLine(string.Format("Font Exp {0}: Textures={1}", exp, bf.Textures.Count));
            for (int i = 0; i < bf.Textures.Count; i++)
            {
                var tx = BmTexture.Load(p, bf.Textures[i]);
                Console.WriteLine(string.Format("  Tex {0} (exp {1}): {2}x{3}, format={4}, MipDataLen={5}",
                    i, bf.Textures[i], tx.SizeX, tx.SizeY, tx.Format, tx.MipDataLen));
            }
        }
    }
}
