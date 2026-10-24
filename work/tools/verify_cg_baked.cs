
using System;

class VerifyBaked
{
    static void Main()
    {
        var p = Package.Load(@"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools\test_cg_baked.xxx");
        var bf = BmFontData.Load(p, 3);
        Console.WriteLine(string.Format("Baked CG Exp 3: HasIsRemapped={0}, IsRemapped={1}, Kerning={2}, Chars={3}, Remap={4}",
            bf.HasIsRemapped, bf.IsRemapped, bf.Kerning, bf.Chars.Count, bf.Remap.Count));
        
        // Check order of remap keys
        int prev = -1;
        bool sorted = true;
        foreach (var k in bf.Remap.Keys)
        {
            if (k < prev) { sorted = false; break; }
            prev = k;
        }
        Console.WriteLine("Remap keys sorted: " + sorted);

        // Check specific keys
        Console.WriteLine("Key '!' (33): " + bf.Remap[33]);
        Console.WriteLine("Key 'ถ' (3606): " + bf.Remap[0x0E16]);
        Console.WriteLine("Key PUA U+F70A: " + bf.Remap[0xF70A]);
    }
}
