using System;
using System.IO;

class TestDirectIndex
{
    static void Main()
    {
        string inPkg = @"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\output\test_swizzled_startup_f6.xxx";
        string outPkg = @"C:\Users\Supakiat\AppData\Roaming\yuzu\load\0100E870163CA000\MODTHAI\romfs\BmGame\CookedSwitch\Startup.xxx";

        var p = Package.Load(inPkg);
        var bf = BmFontData.Load(p, 6);

        // In test_swizzled_startup_f6.xxx:
        // Cell 33 is '!' (ASCII 33)
        // Cell 277 is 'ถ' (0x0E16)
        // What if we swap Cell 33 with Cell 277?
        // So cell 33 (ASCII 33 = '!') becomes 'ถ'!
        Console.WriteLine("Swapping Cell 33 ('!') with Cell 277 ('ถ')...");
        var c33 = bf.Chars[33];
        var c277 = bf.Chars[277];

        // Replace cell 33 with cell 277
        bf.Chars[33] = c277;

        // Write modified Characters array back into package
        // Let's find where Characters array is in bf.E
        int q = bf.CharsValuePos + 4 + 33 * 21;
        // c277: U (4), V (4), W (4), H (4), Tex (1), VOff (4)
        var d = p.Data;
        BitConverter.GetBytes(c277.U).CopyTo(d, q);
        BitConverter.GetBytes(c277.V).CopyTo(d, q + 4);
        BitConverter.GetBytes(c277.W).CopyTo(d, q + 8);
        BitConverter.GetBytes(c277.H).CopyTo(d, q + 12);
        d[q + 16] = (byte)c277.Tex;
        BitConverter.GetBytes(c277.VOff).CopyTo(d, q + 17);

        File.WriteAllBytes(outPkg, d);
        Console.WriteLine("Saved test Startup.xxx to Yuzu! Cell 33 ('!') is now 'ถ'.");
    }
}
