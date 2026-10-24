
using System;

class CheckLower
{
    static void Main()
    {
        var p = Package.Load(@"C:\Users\Supakiat\AppData\Roaming\yuzu\load\0100E870163CA000\MODTHAI\romfs\BmGame\CookedSwitch\Startup.xxx");
        var bf = BmFontData.Load(p, 6);
        foreach (char ch in "abcdefghijklmnopqrstuvwxyz")
        {
            int idx;
            if (bf.Remap.TryGetValue(ch, out idx))
            {
                var c = bf.Chars[idx];
                if (c.W <= 0 || c.H <= 0)
                    Console.WriteLine(string.Format("Char '{0}' has zero size: W={1}, H={2}", ch, c.W, c.H));
            }
            else
            {
                Console.WriteLine(string.Format("Char '{0}' NOT IN REMAP!", ch));
            }
        }
        Console.WriteLine("Check finished.");
    }
}
