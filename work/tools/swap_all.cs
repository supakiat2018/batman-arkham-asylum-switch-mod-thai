using System;
using System.IO;

class SwapAll
{
    static void SwapInFont(Package p, int exp)
    {
        var bf = BmFontData.Load(p, exp);
        if (bf.Chars.Count <= 277)
        {
            Console.WriteLine(string.Format("Exp {0} has only {1} chars, skip", exp, bf.Chars.Count));
            return;
        }

        var c277 = bf.Chars[277];
        int q = bf.CharsValuePos + 4 + 33 * 21;
        var d = p.Data;
        BitConverter.GetBytes(c277.U).CopyTo(d, q);
        BitConverter.GetBytes(c277.V).CopyTo(d, q + 4);
        BitConverter.GetBytes(c277.W).CopyTo(d, q + 8);
        BitConverter.GetBytes(c277.H).CopyTo(d, q + 12);
        d[q + 16] = (byte)c277.Tex;
        BitConverter.GetBytes(c277.VOff).CopyTo(d, q + 17);
        Console.WriteLine(string.Format("Exp {0}: Swapped Cell 33 with Cell 277 ('ถ')", exp));
    }

    static void Main()
    {
        string yuzuDir = @"C:\Users\Supakiat\AppData\Roaming\yuzu\load\0100E870163CA000\MODTHAI\romfs\BmGame\CookedSwitch";
        string modDir = @"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\modthai\0100E870163CA000\romfs\BmGame\CookedSwitch";

        // 1. CommonGame_LOC_INT.xxx
        string cgPath = Path.Combine(yuzuDir, "CommonGame_LOC_INT.xxx");
        var pCg = Package.Load(cgPath);
        for (int exp = 1; exp <= 4; exp++) SwapInFont(pCg, exp);
        File.WriteAllBytes(cgPath, pCg.Data);
        File.WriteAllBytes(Path.Combine(modDir, "CommonGame_LOC_INT.xxx"), pCg.Data);
        Console.WriteLine("Saved CommonGame_LOC_INT.xxx");

        // 2. Startup.xxx
        string startPath = Path.Combine(yuzuDir, "Startup.xxx");
        var pStart = Package.Load(startPath);
        foreach (int exp in new int[] { 4, 5, 6, 7, 8, 9, 20, 21 }) SwapInFont(pStart, exp);
        File.WriteAllBytes(startPath, pStart.Data);
        File.WriteAllBytes(Path.Combine(modDir, "Startup.xxx"), pStart.Data);
        Console.WriteLine("Saved Startup.xxx");
    }
}
