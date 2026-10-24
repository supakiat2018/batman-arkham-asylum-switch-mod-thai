using System;
using System.IO;

class RemapDumper
{
    static void Main(string[] args)
    {
        var p = Package.Load(args[0]);
        var bf = BmFontData.Load(p, int.Parse(args[1]));
        foreach (var kv in bf.Remap)
        {
            if (kv.Key > 127 && (kv.Key < 0x0E00 || kv.Key > 0x0E7F))
            {
                Console.WriteLine(string.Format("Key U+{0:X4}: idx={1}", kv.Key, kv.Value));
            }
        }
    }
}
