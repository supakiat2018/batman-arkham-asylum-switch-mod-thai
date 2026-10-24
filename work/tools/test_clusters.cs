using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

class TestClusters
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

        var cellMap = new Dictionary<int, Cell>();
        foreach (var pl in plan)
        {
            int code = pl[0];
            int srcCode = pl[1];
            if (!ttf.Cmap.ContainsKey(srcCode)) continue;
            int clipped;
            cellMap[code] = ThaiRaster.Render(ttf, ttf.Cmap[srcCode], s, pl[2], pl[3], pad, baseline, out clipped);
        }

        // Test sequences from Cutscene.int
        // "ผู้" -> ผ (0x0E1C), ู (0x0E39), ้ (0xF70B)
        // "กิ้" -> ก (0x0E01), ิ (0x0E34), ้ (0x0E49)
        // "ไม่" -> ไ (0x0E44), ม (0x0E21), ่ (0xF70A)
        // "ด้วย" -> ด (0x0E14), ้ (0xF70B), ว (0x0E27), ย (0x0E22)
        string[] words = { "ผู้", "กิ้", "ไม่", "ด้วย" };
        int canvasW = 300, canvasH = 60;
        var canvas = new Bitmap(canvasW, canvasH);
        using (var g = Graphics.FromImage(canvas)) g.Clear(Color.Black);

        int penX = 10;
        int penY = 20;

        // In Cutscene.int:
        // "ผู้" is encoded as: 0x0E1C, 0x0E39, 0xF70B
        int[][] seqs = {
            new int[] { 0x0E1C, 0x0E39, 0xF70B }, // ผู้
            new int[] { 0x0E01, 0x0E34, 0x0E49 }, // กิ้
            new int[] { 0x0E44, 0x0E21, 0xF70A }, // ไม่
            new int[] { 0x0E14, 0xF70B, 0x0E27, 0x0E22 } // ด้วย
        };

        for (int w = 0; w < seqs.Length; w++)
        {
            foreach (int code in seqs[w])
            {
                Cell c = cellMap[code];
                DrawCell(canvas, c, penX, penY + c.VOff);
                penX += c.W + kerning;
            }
            penX += 15; // space
        }

        canvas.Save(@"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\clusters_test.png");
        Console.WriteLine("Saved clusters_test.png, final penX=" + penX);
    }

    static void DrawCell(Bitmap bmp, Cell c, int destX, int destY)
    {
        for (int y = 0; y < c.H; y++)
            for (int x = 0; x < c.W; x++)
            {
                uint px = c.Px[y * c.W + x];
                byte a = (byte)(px >> 24);
                byte r = (byte)(px >> 16);
                byte g = (byte)(px >> 8);
                byte b = (byte)px;
                if (a > 0)
                {
                    int bx = destX + x;
                    int by = destY + y;
                    if (bx >= 0 && bx < bmp.Width && by >= 0 && by < bmp.Height)
                    {
                        var cur = bmp.GetPixel(bx, by);
                        int outR = Math.Min(255, cur.R + r);
                        int outG = Math.Min(255, cur.G + g);
                        int outB = Math.Min(255, cur.B + b);
                        bmp.SetPixel(bx, by, Color.FromArgb(255, outR, outG, outB));
                    }
                }
            }
    }
}
