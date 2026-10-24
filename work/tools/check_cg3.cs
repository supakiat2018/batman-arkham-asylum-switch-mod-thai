using System;

class CheckCg3
{
    static void Main()
    {
        string p = @"C:\Users\Supakiat\AppData\Roaming\yuzu\load\0100E870163CA000\MODTHAI\romfs\BmGame\CookedSwitch\CommonGame_LOC_INT.xxx";
        var pkg = Package.Load(p);
        var bf = BmFontData.Load(pkg, 3);
        Console.WriteLine("CG Exp 3: Kerning=" + bf.Kerning + ", Chars=" + bf.Chars.Count + ", Remap=" + bf.Remap.Count);
        Console.WriteLine("Cell 33: W=" + bf.Chars[33].W + ", H=" + bf.Chars[33].H);
        Console.WriteLine("Cell 277: W=" + bf.Chars[277].W + ", H=" + bf.Chars[277].H);
    }
}
