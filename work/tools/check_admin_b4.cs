using System;

class CheckAdminB4
{
    static void Main()
    {
        string p = @"C:\Users\Supakiat\AppData\Roaming\yuzu\load\0100E870163CA000\MODTHAI\romfs\BmGame\CookedSwitch\Admin_B4_LOC_INT.xxx";
        var pkg = Package.Load(p);
        Console.WriteLine("Exports count: " + pkg.Exports.Count);
        for (int i = 0; i < pkg.Exports.Count; i++)
        {
            var e = pkg.Exports[i];
            string cname = e.Class < 0 ? pkg.Imports[-e.Class - 1].Name : (e.Class > 0 ? pkg.Exports[e.Class - 1].Name : "None");
            if (cname == "Font" || cname.Contains("Font") || e.Name.Contains("Font"))
            {
                Console.WriteLine(string.Format("Export {0}: {1} (Class: {2})", i + 1, e.Name, cname));
            }
        }
    }
}
