
using System;
using System.IO;

class ExportRemap
{
    static void Main()
    {
        string p = @"C:\Users\Supakiat\AppData\Roaming\yuzu\load\0100E870163CA000\MODTHAI\romfs\BmGame\CookedSwitch\CommonGame_LOC_INT.xxx";
        var pkg = Package.Load(p);
        var bf = BmFontData.Load(pkg, 3);
        using (var sw = new StreamWriter(@"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools\remap_keys.txt"))
        {
            foreach (var k in bf.Remap.Keys)
            {
                sw.WriteLine(k);
            }
        }
        Console.WriteLine("Exported " + bf.Remap.Count + " remap keys.");
    }
}
