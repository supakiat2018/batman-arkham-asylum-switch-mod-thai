using System;
using System.IO;

class VerifyFinalCg
{
    static void Main()
    {
        var p = Package.Load(@"C:\Users\Supakiat\AppData\Roaming\yuzu\load\0100E870163CA000\MODTHAI\romfs\BmGame\CookedSwitch\CommonGame_LOC_INT.xxx");
        Console.WriteLine("=== Final Deployed CommonGame_LOC_INT.xxx ===");
        int[] exps = { 1, 2, 3, 4 };
        foreach (int exp in exps)
        {
            var bf = BmFontData.Load(p, exp);
            Console.WriteLine(string.Format("Exp {0}: Kerning={1}, Chars={2}, Remap={3}", exp, bf.Kerning, bf.Chars.Count, bf.Remap.Count));
        }
    }
}
