using System;
using System.Drawing;
using System.IO;

class VisualSim
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

        int baseline = hc.VOff + hc.H - 1;

        // Render 'อ' (0x0E2D)
        int clipped;
        var cellO = ThaiRaster.Render(ttf, ttf.Cmap[0x0E2D], s, 0, 0, pad, baseline, out clipped);
        // Render '้' (0xF70B or 0x0E49)
        var plTho = plan.Find(pl => pl[0] == 0xF70B);
        var cellTho = ThaiRaster.Render(ttf, ttf.Cmap[plTho[1]], s, plTho[2], plTho[3], pad, baseline, out clipped);

        Console.WriteLine(string.Format("Cell 'อ': W={0}, H={1}, VOff={2}", cellO.W, cellO.H, cellO.VOff));
        Console.WriteLine(string.Format("Cell '้': W={0}, H={1}, VOff={2}", cellTho.W, cellTho.H, cellTho.VOff));

        // Simulate UE3 Canvas rendering of "อ" followed by "้"
        // Kerning = -pad
        int kerning = -pad;
        int canvasW = 100, canvasH = 60;
        var canvas = new Bitmap(canvasW, canvasH);
        using (var g = Graphics.FromImage(canvas)) g.Clear(Color.Black);

        int penX = 10;
        int penY = 20;

        // 1. Draw 'อ'
        DrawCell(canvas, cellO, penX, penY + cellO.VOff);
        penX += cellO.W + kerning;
        Console.WriteLine("After 'อ', PenX = " + penX);

        // 2. Draw '้'
        DrawCell(canvas, cellTho, penX, penY + cellTho.VOff);
        penX += cellTho.W + kerning;
        Console.WriteLine("After '้', PenX = " + penX);

        canvas.Save(@"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\sim_o_tho.png");
        Console.WriteLine("Saved sim_o_tho.png");
    }

    static void DrawCell(Bitmap bmp, Cell c, int destX, int destY)
    {
        for (int y = 0; y < c.H; y++)
        {
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
                        // simple alpha blend
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
}
