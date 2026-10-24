using System;

class CheckOrder
{
    static void Main()
    {
        var p = Package.Load(@"C:\Users\Supakiat\AppData\Roaming\yuzu\load\0100E870163CA000\MODTHAI\romfs\BmGame\CookedSwitch\Startup.xxx");
        var bf = BmFontData.Load(p, 6);
        Console.WriteLine("Remap count: " + bf.Remap.Count);
        int i = 0;
        foreach (var kv in bf.Remap)
        {
            if (i < 15 || i > bf.Remap.Count - 15)
            {
                Console.WriteLine(string.Format("  [{0}] Key=0x{1:X4} ({2}) -> Val={3}", i, kv.Key, kv.Key, kv.Value));
            }
            i++;
        }
    }
}
