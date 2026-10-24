
using System;

class CheckPcFont
{
    static void Main()
    {
        string pcDir = @"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\BmGame\CookedPC";
        // Check if pcDir exists, or find where PC upk is
        if (!System.IO.Directory.Exists(pcDir))
        {
            Console.WriteLine("PC dir not found: " + pcDir);
            return;
        }
        var p = Package.Load(pcDir + "\\Startup_INT.upk");
        for (int i = 1; i <= p.Exports.Count; i++)
        {
            var e = p.Exports[i - 1];
            if (e.Class < 0 && p.Imports[-e.Class - 1].Name == "Font")
            {
                var bf = BmFontData.Load(p, i);
                Console.WriteLine(string.Format("PC Exp {0} ({1}): Chars={2}, Remap={3}", i, e.Name, bf.Chars.Count, bf.Remap.Count));
            }
        }
    }
}
