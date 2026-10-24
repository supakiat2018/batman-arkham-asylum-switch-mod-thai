using System;

class CheckThaiCoords
{
    static void Main()
    {
        var p = Package.Load(@"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\output\test_swizzled_startup_f6.xxx");
        var bf = BmFontData.Load(p, 6);
        foreach (int code in new int[] { 'A', 'H', 0x0E01, 0x0E16, 0x0E2D, 0x0E22 })
        {
            int idx = bf.Remap[code];
            var c = bf.Chars[idx];
            Console.WriteLine(string.Format("Code 0x{0:X4}: U={1}, V={2}, W={3}, H={4}, VOff={5}",
                code, c.U, c.V, c.W, c.H, c.VOff));
        }
    }
}
