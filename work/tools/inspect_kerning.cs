using System;
using System.IO;

class KerningInspector
{
    static void Main(string[] args)
    {
        var p = Package.Load(args[0]);
        var bf = BmFontData.Load(p, int.Parse(args[1]));
        Console.WriteLine(Path.GetFileName(args[0]) + " exp " + args[1] + ": Kerning=" + bf.Kerning + ", HasKerning=" + bf.HasKerning);
        for (int i = 0; i < 5; i++)
        {
            var c = bf.Chars[i];
            Console.WriteLine(string.Format("  char {0}: U={1}, V={2}, W={3}, H={4}, VOff={5}", i, c.U, c.V, c.W, c.H, c.VOff));
        }
    }
}
