
using System;
using System.IO;

class InspectCells
{
    static void Main()
    {
        var p = Package.Load(@"C:\Users\Supakiat\AppData\Roaming\yuzu\load\0100E870163CA000\MODTHAI\romfs\BmGame\CookedSwitch\Startup.xxx");
        var bf = BmFontData.Load(p, 6);
        Console.WriteLine("Font 6 Kerning: " + bf.Kerning + ", Chars count: " + bf.Chars.Count);
        int[] test = { 0x0E16, 0x0E2D, 0x0E22, 0x0E44, 0x0E1B, 0x20, 0x0E44, 0x0E2D, 0xF70B, 0x0E42, 0x0E07, 0xF70A, 0x21 };
        foreach (int ch in test)
        {
            int idx;
            if (bf.Remap.TryGetValue(ch, out idx))
            {
                var c = bf.Chars[idx];
                Console.WriteLine(string.Format("Char 0x{0:X4} -> cell {1}: U={2}, V={3}, W={4}, H={5}, VOff={6}, NetAdv={7}",
                    ch, idx, c.U, c.V, c.W, c.H, c.VOff, c.W + bf.Kerning));
            }
            else
            {
                Console.WriteLine(string.Format("Char 0x{0:X4} -> NOT FOUND", ch));
            }
        }
    }
}
