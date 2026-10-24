using System;

class CheckCoordsFinal
{
    static void Main()
    {
        var p = Package.Load(@"C:\Users\Supakiat\AppData\Roaming\yuzu\load\0100E870163CA000\MODTHAI\romfs\BmGame\CookedSwitch\Startup.xxx");
        var bf = BmFontData.Load(p, 6);
        Console.WriteLine("Kerning: " + bf.Kerning + ", Chars: " + bf.Chars.Count + ", Textures: " + bf.Textures.Count);
        foreach (int code in new int[] { '!', ' ', 0x0E16, 0x0E2D, 0x0E22, 0x0E44, 0x0E1B, 0xF70B, 0x0E42, 0x0E07, 0xF70A })
        {
            int idx;
            if (bf.Remap.TryGetValue(code, out idx))
            {
                var c = bf.Chars[idx];
                Console.WriteLine(string.Format("Code 0x{0:X4} -> cell {1}: Tex={2}, U={3}, V={4}, W={5}, H={6}, VOff={7}",
                    code, idx, c.Tex, c.U, c.V, c.W, c.H, c.VOff));
            }
            else
            {
                Console.WriteLine("Code 0x" + code.ToString("X4") + " NOT IN REMAP!");
            }
        }
    }
}
