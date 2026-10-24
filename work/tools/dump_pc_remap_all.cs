using System;
using System.IO;

class RemapDumperAll
{
    static void Main(string[] args)
    {
        var p = Package.Load(args[0]);
        var bf = BmFontData.Load(p, int.Parse(args[1]));
        Console.WriteLine("Total remap: " + bf.Remap.Count);
        foreach (var kv in bf.Remap)
        {
            if (kv.Key >= 0x0E00 && kv.Key <= 0x0E7F)
            {
                Console.WriteLine(string.Format("Thai Key U+{0:X4} ({(char)kv.Key}): idx={1}", kv.Key, kv.Value));
            }
        }
    }
}
