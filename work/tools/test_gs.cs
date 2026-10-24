using System;
using System.IO;
using System.Linq;

class TestGoogleSans
{
    static void Main()
    {
        var ttf = new Ttf(@"C:\Users\Supakiat\Downloads\Google_Sans\static\GoogleSans-Bold.ttf");
        var p = Package.Load(@"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\romfs\BmGame\CookedSwitch\Startup.xxx");
        var bf = BmFontData.Load(p, 6);
        var hc = bf.Chars[bf.Remap['H']];
        double thaiPx = hc.H - 2;
        double s = thaiPx / ttf.Contours(ttf.Cmap[0x0E01]).ConvertAll(ct => ct.ConvertAll(q => q.Y).Max()).Max();
        var plan = ThaiRaster.Plan(ttf);
        double left = 0;
        foreach (var pl in plan) left = Math.Max(left, ThaiRaster.LeftInk(ttf, ttf.Cmap[pl[1]], s, pl[2]));
        int pad = (int)Math.Ceiling(left) + 2;
        Console.WriteLine("thaiPx=" + thaiPx + ", s=" + s + ", pad=" + pad);

        string test = "ถอยไป ไอ้โง่! ฉันมีระเบิด อ้อ จริงสิ เฮะ เฮะ...!!!";
        Console.WriteLine("\nTesting string: " + test);
        // Let's calculate pen position simulation
        int pen = 0;
        foreach (char ch in test)
        {
            int code = (int)ch;
            int g;
            if (ttf.Cmap.TryGetValue(code, out g))
            {
                int adv = (int)Math.Round(ttf.Advance(g) * s);
                int W = pad + adv;
                int netAdv = W - pad; // since Kerning = -pad
                Console.WriteLine(string.Format("Char '{0}' (0x{1:X4}): adv={2}, W={3}, netAdv={4}, PenX: {5} -> {6}",
                    ch, code, adv, W, netAdv, pen, pen + netAdv));
                pen += netAdv;
            }
            else if (code == 32)
            {
                // Space
                int spaceW = bf.Chars[bf.Remap[' ']].W;
                // If space gets pad:
                int W_fixed = spaceW + pad;
                int netAdv_fixed = W_fixed - pad; // = spaceW
                // Old bug:
                int W_bug = spaceW;
                int netAdv_bug = W_bug - pad;
                Console.WriteLine(string.Format("SPACE: original={0}, BUG netAdv={1}, FIXED netAdv={2}, PenX: {3} -> {4}",
                    spaceW, netAdv_bug, netAdv_fixed, pen, pen + netAdv_fixed));
                pen += netAdv_fixed;
            }
            else
            {
                Console.WriteLine(string.Format("Char '{0}' (0x{1:X4}) NOT IN TTF", ch, code));
            }
        }
    }
}
