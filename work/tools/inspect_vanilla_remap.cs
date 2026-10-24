
using System;

class InspectVanillaRemap
{
    static void Main()
    {
        var p = Package.Load(@"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\romfs\BmGame\CookedSwitch\Startup.xxx");
        foreach (int exp in new int[] { 10, 12, 16, 20 })
        {
            var bf = BmFontData.Load(p, exp);
            var e = p.Exports[exp - 1];
            int rel = bf.PropsEnd - e.SerialOffset;
            int remaining = e.SerialSize - rel;
            Console.WriteLine(string.Format("Exp {0} ({1}): Chars={2}, RemapCount={3}, End-PropsEnd={4}",
                exp, e.Name, bf.Chars.Count, bf.Remap.Count, remaining));
            
            // Print first few remap entries
            int count = 0;
            foreach (var kv in bf.Remap)
            {
                if (count < 5 || count > bf.Remap.Count - 5)
                {
                    Console.WriteLine(string.Format("   Key=0x{0:X4} ({0}) -> Idx={1}", kv.Key, kv.Value));
                }
                count++;
            }
        }
    }
}
