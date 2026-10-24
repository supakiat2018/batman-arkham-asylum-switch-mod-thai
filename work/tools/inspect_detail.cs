using System;
using System.IO;

class DetailInspector
{
    static void Main(string[] args)
    {
        var p = Package.Load(args[0]);
        var bf = BmFontData.Load(p, int.Parse(args[1]));
        Console.WriteLine("Chars: " + bf.Chars.Count + ", Remap: " + bf.Remap.Count);
        char[] testChars = { '!', 'X', 'P', 'เ', 'ฮ', 'ช', 'า', 'ร', 'ป', (char)0xF702, (char)0xF70B, (char)0xF70E, (char)0xF714 };
        foreach (char c in testChars)
        {
            int idx = -1;
            bool found = bf.Remap.TryGetValue(c, out idx);
            if (found)
            {
                var fc = bf.Chars[idx];
                Console.WriteLine(string.Format("Char U+{0:X4}: idx={1}, U={2}, V={3}, W={4}, H={5}, Tex={6}, VOff={7}", (int)c, idx, fc.U, fc.V, fc.W, fc.H, fc.Tex, fc.VOff));
            }
            else
            {
                Console.WriteLine(string.Format("Char U+{0:X4}: NOT FOUND", (int)c));
            }
        }
    }
}
