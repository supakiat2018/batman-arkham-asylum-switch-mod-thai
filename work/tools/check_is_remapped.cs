
using System;

class CheckIsRemapped
{
    static void CheckPkg(string pkgPath, string label)
    {
        Console.WriteLine("=== " + label + " ===");
        var p = Package.Load(pkgPath);
        for (int i = 1; i <= p.Exports.Count; i++)
        {
            var e = p.Exports[i - 1];
            string cname = e.Class < 0 ? p.Imports[-e.Class - 1].Name : (e.Class > 0 ? p.Exports[e.Class - 1].Name : "None");
            if (cname == "Font")
            {
                bool hasRemappedProp = false;
                int isRemappedVal = -1;
                Subs.Walk(p, e.SerialOffset + 4, e.SerialOffset + e.SerialSize, delegate(int t, string n, int sp, int v, int s)
                {
                    if (n == "IsRemapped")
                    {
                        hasRemappedProp = true;
                        isRemappedVal = BitConverter.ToInt32(p.Data, v);
                    }
                });
                Console.WriteLine(string.Format("Exp {0} ({1}): HasIsRemapped={2}, Val={3}", i, e.Name, hasRemappedProp, isRemappedVal));
            }
        }
    }

    static void Main()
    {
        CheckPkg(@"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\romfs\BmGame\CookedSwitch\Startup.xxx", "Vanilla Switch Startup");
        CheckPkg(@"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\decompressed\CommonGame_LOC_INT.xxx", "Vanilla Switch CommonGame");
        string pcStartup = @"C:\program files (x86)\steam\steamapps\common\Batman Arkham Asylum GOTY\BmGame\CookedPC\Startup_INT.upk";
        if (System.IO.File.Exists(pcStartup))
        {
            CheckPkg(pcStartup, "PC Startup_INT.upk");
        }
    }
}
