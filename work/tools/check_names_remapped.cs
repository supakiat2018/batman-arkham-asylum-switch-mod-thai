
using System;

class CheckNames
{
    static void Main()
    {
        var pStart = Package.Load(@"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\romfs\BmGame\CookedSwitch\Startup.xxx");
        Console.WriteLine("Startup has IsRemapped: " + pStart.Names.Contains("IsRemapped"));

        var pCg = Package.Load(@"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\decompressed\CommonGame_LOC_INT.xxx");
        Console.WriteLine("CommonGame has IsRemapped: " + pCg.Names.Contains("IsRemapped"));
    }
}
