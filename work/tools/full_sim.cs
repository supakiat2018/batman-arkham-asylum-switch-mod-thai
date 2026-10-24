using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

class FullSim
{
    static void Main()
    {
        var ttf = new Ttf(@"C:\Users\Supakiat\Downloads\Google_Sans\static\GoogleSans-Bold.ttf");
        var p = Package.Load(@"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\romfs\BmGame\CookedSwitch\Startup.xxx");
        var bf = BmFontData.Load(p, 6);
        var hc = bf.Chars[bf.Remap['H']];
        double thaiPx = hc.H - 2;
        double maxH = 0;
        foreach (var ct in ttf.Contours(ttf.Cmap[0x0E01])) foreach (var q in ct) maxH = Math.Max(maxH, q.Y);
        double s = thaiPx / maxH;
        var plan = ThaiRaster.Plan(ttf);
        double left = 0;
        foreach (var pl in plan) left = Math.Max(left, ThaiRaster.LeftInk(ttf, ttf.Cmap[pl[1]], s, pl[2]));
        int pad = (int)Math.Ceiling(left) + 2;
        int kerning = -pad;
        int baseline = hc.VOff + hc.H - 1;

        // Render dictionary
        var cellMap = new Dictionary<int, Cell>();
        foreach (var pl in plan)
        {
            int code = pl[0];
            int srcCode = pl[1];
            if (!ttf.Cmap.ContainsKey(srcCode)) continue;
            int clipped;
            var cell = ThaiRaster.Render(ttf, ttf.Cmap[srcCode], s, pl[2], pl[3], pad, baseline, out clipped);
            cellMap[code] = cell;
        }

        // Add ASCII from bf
        var asciiCells = new Dictionary<char, Cell>();
        foreach (var kv in bf.Remap)
        {
            if (kv.Key < 128)
            {
                var orig = bf.Chars[kv.Value];
                // In fixed baker: EVERY character gets cpad = pad
                var cell = new Cell { W = orig.W + pad, H = Math.Max(1, orig.H), VOff = orig.VOff };
                asciiCells[(char)kv.Key] = cell;
            }
        }

        string text = "ถอยไป ไอ้โง่! ฉันมีระเบิด อ้อ จริงสิ";

        // Test FIXED simulation
        int penX = 10;
        int minPenX = penX;
        foreach (char ch in text)
        {
            int code = (int)ch;
            Cell c;
            if (cellMap.TryGetValue(code, out c))
            {
                int net = c.W + kerning;
                penX += net;
            }
            else if (asciiCells.TryGetValue(ch, out c))
            {
                int net = c.W + kerning;
                penX += net;
            }
            minPenX = Math.Min(minPenX, penX);
        }
        Console.WriteLine(string.Format("FIXED: Final PenX = {0}, Min PenX = {1}", penX, minPenX));

        // Test BUGGY simulation (space has W = orig.W, so net = orig.W - pad)
        int penXBug = 10;
        int minPenXBug = penXBug;
        foreach (char ch in text)
        {
            int code = (int)ch;
            Cell c;
            if (cellMap.TryGetValue(code, out c))
            {
                int net = c.W + kerning;
                penXBug += net;
            }
            else if (ch == ' ')
            {
                int origW = bf.Chars[bf.Remap[' ']].W;
                int net = origW + kerning; // NEGATIVE!
                penXBug += net;
            }
            else if (asciiCells.TryGetValue(ch, out c))
            {
                int net = c.W + kerning;
                penXBug += net;
            }
            minPenXBug = Math.Min(minPenXBug, penXBug);
        }
        Console.WriteLine(string.Format("BUGGY: Final PenX = {0}, Min PenX = {1}", penXBug, minPenXBug));
    }
}
