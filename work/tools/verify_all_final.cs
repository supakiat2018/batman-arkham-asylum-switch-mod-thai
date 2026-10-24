
using System;

class VerifyAllFinal
{
    static void CheckPkg(string pPath, int[] exps, string name)
    {
        Console.WriteLine("=== " + name + " ===");
        var p = Package.Load(pPath);
        foreach (int exp in exps)
        {
            var bf = BmFontData.Load(p, exp);
            Console.WriteLine(string.Format("Exp {0:2d} ({1,-24}): IsRemapped={2}, RemapCnt={3}, Chars={4}, Kerning={5}",
                exp, p.Exports[exp - 1].Name, bf.IsRemapped, bf.Remap.Count, bf.Chars.Count, bf.Kerning));
        }
    }

    static void Main()
    {
        string yuzuDir = @"C:\Users\Supakiat\AppData\Roaming\yuzu\load\0100E870163CA000\MODTHAI\romfs\BmGame\CookedSwitch";
        CheckPkg(yuzuDir + "\\Startup.xxx", new int[] { 4, 5, 6, 7, 8, 9, 20, 21 }, "Startup.xxx");
        CheckPkg(yuzuDir + "\\CommonGame_LOC_INT.xxx", new int[] { 1, 2, 3, 4 }, "CommonGame_LOC_INT.xxx");
    }
}
