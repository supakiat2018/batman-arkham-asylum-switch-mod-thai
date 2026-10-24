
using System;

class CheckFontClass
{
    static void Main()
    {
        var p = Package.Load(@"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\romfs\BmGame\CookedSwitch\Startup.xxx");
        foreach (int exp in new int[] { 4, 5, 6, 7, 8, 9, 20, 21 })
        {
            var e = p.Exports[exp - 1];
            string cname = e.Class < 0 ? p.Imports[-e.Class - 1].Name : (e.Class > 0 ? p.Exports[e.Class - 1].Name : "None");
            Console.WriteLine(string.Format("Startup Exp {0} ({1}): Class={2}", exp, e.Name, cname));
        }
    }
}
