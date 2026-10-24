using System;

class CheckAdminB4Imports
{
    static void Main()
    {
        string p = @"C:\Users\Supakiat\AppData\Roaming\yuzu\load\0100E870163CA000\MODTHAI\romfs\BmGame\CookedSwitch\Admin_B4_LOC_INT.xxx";
        var pkg = Package.Load(p);
        Console.WriteLine("Imports count: " + pkg.Imports.Count);
        for (int i = 0; i < pkg.Imports.Count; i++)
        {
            var imp = pkg.Imports[i];
            // if (imp.Name.Contains("Font") || imp.Name.Contains("Startup") || imp.Name.Contains("Common"))
            {
                Console.WriteLine(string.Format("Import {0}: {1}", i + 1, imp.Name));
            }
        }
    }
}
