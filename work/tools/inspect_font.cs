using System;
using System.IO;

class FontInspector
{
    static void Main(string[] args)
    {
        var p = Package.Load(args[0]);
        int exp = int.Parse(args[1]);
        var bf = BmFontData.Load(p, exp);
        Console.WriteLine("Font " + Path.GetFileName(args[0]) + " Exp " + exp + ": chars=" + bf.Chars.Count + ", remap=" + bf.Remap.Count);
        int thaiCount = 0, puaCount = 0, cp1252Count = 0;
        foreach (var k in bf.Remap.Keys)
        {
            if (k >= 0x0E00 && k <= 0x0E7F) thaiCount++;
            else if (k >= 0xF700 && k <= 0xF71A) puaCount++;
            else if (k >= 0x80 && k <= 0xFF) cp1252Count++;
        }
        Console.WriteLine("  Thai (0E00-0E7F): " + thaiCount + ", PUA (F700-F71A): " + puaCount + ", CP1252 (80-FF): " + cp1252Count);
    }
}
