
using System;

class InspectVanillaCg
{
    static void Main()
    {
        var p = Package.Load(@"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\decompressed\CommonGame_LOC_INT.xxx");
        for (int exp = 1; exp <= 4; exp++)
        {
            var bf = BmFontData.Load(p, exp);
            Console.WriteLine(string.Format("Vanilla CG Exp {0}: Chars={1}, Remap={2}, Kerning={3}", exp, bf.Chars.Count, bf.Remap.Count, bf.Kerning));
        }
    }
}
