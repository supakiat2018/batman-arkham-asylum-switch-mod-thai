using System;
using System.IO;

class RemapDumperAllKeys
{
    static void Main(string[] args)
    {
        var p = Package.Load(args[0]);
        var bf = BmFontData.Load(p, int.Parse(args[1]));
        Console.WriteLine("Total remap: " + bf.Remap.Count);
        foreach (var kv in bf.Remap)
        {
            if (kv.Key >= 256)
            {
                Console.WriteLine(string.Format("Key U+{0:X4} ({0}): idx={1}", kv.Key, kv.Value));
            }
        }
    }
}
