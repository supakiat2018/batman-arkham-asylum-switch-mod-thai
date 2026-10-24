
using System;

class CheckCells
{
    static void Main()
    {
        string yuzuDir = @"C:\Users\Supakiat\AppData\Roaming\yuzu\load\0100E870163CA000\MODTHAI\romfs\BmGame\CookedSwitch";
        var p = Package.Load(yuzuDir + "\\CommonGame_LOC_INT.xxx");
        var bf = BmFontData.Load(p, 3);
        Console.WriteLine("CG Exp 3: Remap[33]='!' -> cell " + bf.Remap[33] + " (W=" + bf.Chars[bf.Remap[33]].W + ")");
        Console.WriteLine("CG Exp 3: Remap[0x0E16]='ถ' -> cell " + bf.Remap[0x0E16] + " (W=" + bf.Chars[bf.Remap[0x0E16]].W + ")");
    }
}
