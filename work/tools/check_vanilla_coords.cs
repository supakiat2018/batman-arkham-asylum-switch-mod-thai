
using System;

class CheckVanillaCoords
{
    static void Main()
    {
        string vanillaPath = @"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\romfs\BmGame\CookedSwitch\Startup.xxx";
        var p = Package.Load(vanillaPath);
        var bf = BmFontData.Load(p, 6);
        foreach (char ch in "ABCDEFGHIJ01234")
        {
            int idx = bf.Remap[ch];
            var c = bf.Chars[idx];
            Console.WriteLine(string.Format("Char '{0}': Tex={1}, U={2}, V={3}, W={4}, H={5}, VOff={6}",
                ch, c.Tex, c.U, c.V, c.W, c.H, c.VOff));
        }
    }
}
