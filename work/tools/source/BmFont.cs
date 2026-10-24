// UE3 bitmap fonts (UFont + Texture2D pages) for Arkham Asylum's canvas-drawn subtitles.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

static class Dxt5
{
    static uint C565(int c)
    {
        int r = (c >> 11) & 31, g = (c >> 5) & 63, b = c & 31;
        return (uint)(((r << 3) | (r >> 2)) << 16 | ((g << 2) | (g >> 4)) << 8 | ((b << 3) | (b >> 2)));
    }

    // -> ARGB pixels
    public static uint[] Decode(byte[] d, int off, int w, int h)
    {
        var px = new uint[w * h];
        int p = off;
        for (int by = 0; by < h; by += 4)
            for (int bx = 0; bx < w; bx += 4)
            {
                int a0 = d[p], a1 = d[p + 1];
                ulong abits = 0; for (int i = 0; i < 6; i++) abits |= (ulong)d[p + 2 + i] << (8 * i);
                var al = new int[8]; al[0] = a0; al[1] = a1;
                if (a0 > a1) for (int i = 1; i < 7; i++) al[i + 1] = ((7 - i) * a0 + i * a1) / 7;
                else { for (int i = 1; i < 5; i++) al[i + 1] = ((5 - i) * a0 + i * a1) / 5; al[6] = 0; al[7] = 255; }
                int c0 = BitConverter.ToUInt16(d, p + 8), c1 = BitConverter.ToUInt16(d, p + 10);
                uint cb = BitConverter.ToUInt32(d, p + 12);
                var col = new uint[4]; col[0] = C565(c0); col[1] = C565(c1);
                col[2] = Mix(col[0], col[1], 2, 1); col[3] = Mix(col[0], col[1], 1, 2);
                for (int y = 0; y < 4; y++)
                    for (int x = 0; x < 4; x++)
                    {
                        int i = y * 4 + x;
                        int a = al[(int)((abits >> (3 * i)) & 7)];
                        uint c = col[(cb >> (2 * i)) & 3];
                        px[(by + y) * w + bx + x] = ((uint)a << 24) | c;
                    }
                p += 16;
            }
        return px;
    }

    static uint Mix(uint a, uint b, int wa, int wb)
    {
        uint r = 0;
        for (int s = 0; s < 24; s += 8)
            r |= (uint)((((a >> s) & 255) * wa + ((b >> s) & 255) * wb) / (wa + wb)) << s;
        return r;
    }

    // Font pages are grey-scale with alpha: colour endpoints are the darkest and brightest grey in each block.
    public static byte[] Encode(uint[] px, int w, int h)
    {
        var o = new byte[w * h];
        int p = 0;
        for (int by = 0; by < h; by += 4)
            for (int bx = 0; bx < w; bx += 4)
            {
                var a = new int[16]; var l = new int[16];
                int amin = 255, amax = 0, lmin = 255, lmax = 0;
                for (int i = 0; i < 16; i++)
                {
                    uint c = px[(by + i / 4) * w + bx + i % 4];
                    a[i] = (int)(c >> 24);
                    l[i] = (int)(((c >> 16 & 255) * 3 + (c >> 8 & 255) * 6 + (c & 255)) / 10);
                    amin = Math.Min(amin, a[i]); amax = Math.Max(amax, a[i]);
                    if (a[i] > 0) { lmin = Math.Min(lmin, l[i]); lmax = Math.Max(lmax, l[i]); }
                }
                if (lmin > lmax) { lmin = lmax = 255; }
                // alpha: 8-level mode (a0 > a1); a0 == a1 is fine (all levels equal)
                o[p] = (byte)amax; o[p + 1] = (byte)amin;
                var al = new int[8]; al[0] = amax; al[1] = amin;
                for (int i = 1; i < 7; i++) al[i + 1] = ((7 - i) * amax + i * amin) / 7;
                ulong abits = 0;
                for (int i = 0; i < 16; i++)
                {
                    int best = 0, bd = 999;
                    for (int k = 0; k < 8; k++) { int dd = Math.Abs(al[k] - a[i]); if (dd < bd) { bd = dd; best = k; } }
                    abits |= (ulong)best << (3 * i);
                }
                for (int i = 0; i < 6; i++) o[p + 2 + i] = (byte)(abits >> (8 * i));
                // colour: 4-colour mode needs c0 > c1
                int c0 = Grey565(lmax), c1 = Grey565(lmin);
                if (c0 == c1) { if (c1 > 0) c1 = Grey565(Math.Max(0, lmin - 8)); else c0 = Grey565(Math.Min(255, lmax + 8)); }
                if (c0 < c1) { int t = c0; c0 = c1; c1 = t; }
                var cl = new int[4]; cl[0] = Lum(C565(c0)); cl[1] = Lum(C565(c1)); cl[2] = (2 * cl[0] + cl[1]) / 3; cl[3] = (cl[0] + 2 * cl[1]) / 3;
                uint cbits = 0;
                for (int i = 0; i < 16; i++)
                {
                    int best = 0, bd = 999;
                    for (int k = 0; k < 4; k++) { int dd = Math.Abs(cl[k] - l[i]); if (dd < bd) { bd = dd; best = k; } }
                    cbits |= (uint)best << (2 * i);
                }
                BitConverter.GetBytes((ushort)c0).CopyTo(o, p + 8);
                BitConverter.GetBytes((ushort)c1).CopyTo(o, p + 10);
                BitConverter.GetBytes(cbits).CopyTo(o, p + 12);
                p += 16;
            }
        return o;
    }

    static int Grey565(int l) { return ((l >> 3) << 11) | ((l >> 2) << 5) | (l >> 3); }
    static int Lum(uint c) { return (int)(((c >> 16 & 255) * 3 + (c >> 8 & 255) * 6 + (c & 255)) / 10); }
}

class FontChar { public int U, V, W, H, Tex, VOff; }

// UFont export: Characters (21-byte immutable structs), Textures, tagged props, then native CharRemap (count, WORD key, WORD value)
class BmFontData
{
    public Package P; public Exp E; public int ExportIndex;
    public List<FontChar> Chars = new List<FontChar>();
    public List<int> Textures = new List<int>();
    public Dictionary<int, int> Remap = new Dictionary<int, int>();
    public int Kerning; public bool HasKerning;
    public int IsRemapped; public bool HasIsRemapped;
    public int CharsValuePos, PropsEnd;

    public static BmFontData Load(Package p, int exportIndex)
    {
        var f = new BmFontData { P = p, ExportIndex = exportIndex, E = p.Exports[exportIndex - 1] };
        var d = p.Data;
        int end = f.E.SerialOffset + f.E.SerialSize;
        f.PropsEnd = Subs.Walk(p, f.E.SerialOffset + 4, end, delegate(int t, string n, int sp, int v, int s)
        {
            if (n == "Characters")
            {
                f.CharsValuePos = v;
                int cnt = BitConverter.ToInt32(d, v);
                if (cnt * 21 + 4 != s) throw new Exception("unexpected FontCharacter size");
                for (int i = 0; i < cnt; i++)
                {
                    int q = v + 4 + i * 21;
                    f.Chars.Add(new FontChar { U = BitConverter.ToInt32(d, q), V = BitConverter.ToInt32(d, q + 4), W = BitConverter.ToInt32(d, q + 8), H = BitConverter.ToInt32(d, q + 12), Tex = d[q + 16], VOff = BitConverter.ToInt32(d, q + 17) });
                }
            }
            else if (n == "Textures")
            {
                int cnt = BitConverter.ToInt32(d, v);
                for (int i = 0; i < cnt; i++) f.Textures.Add(BitConverter.ToInt32(d, v + 4 + i * 4));
            }
            else if (n == "Kerning") { f.Kerning = BitConverter.ToInt32(d, v); f.HasKerning = true; }
            else if (n == "IsRemapped") { f.IsRemapped = BitConverter.ToInt32(d, v); f.HasIsRemapped = true; }
        });
        int r = f.PropsEnd;
        if (r + 4 <= end)
        {
            int rc = BitConverter.ToInt32(d, r);
            for (int i = 0; i < rc && r + 4 + (i + 1) * 4 <= end; i++) f.Remap[BitConverter.ToUInt16(d, r + 4 + i * 4)] = BitConverter.ToUInt16(d, r + 6 + i * 4);
            if (r + 4 + rc * 4 != end) Console.Error.WriteLine("warning: " + (end - r - 4 - rc * 4) + " bytes after CharRemap");
        }
        if (f.Remap.Count == 0)
        {
            for (int i = 0; i < f.Chars.Count; i++) f.Remap[i] = i;
        }
        return f;
    }
}

// One glyph image for the rebuilt atlas. Every cell carries Pad transparent columns on the left and the font
// gets Kerning = -Pad, so the pen advance is W - Pad. A Thai mark has W == Pad (zero advance): its cell lies
// over the right edge of the previous glyph, which is where Thai marks sit. Canvas text cannot move a glyph
// left, so this is how zero-width marks are made without touching the exe.
class Cell { public int W, H, VOff; public uint[] Px; public int X, Y; }

static class ThaiRaster
{
    // y down, origin at (ox, oy) in pixels, scale s px/unit
    static void AddContour(System.Drawing.Drawing2D.GraphicsPath gp, List<Ttf.Pt> ct, double s, double ox, double oy)
    {
        Func<double, double, System.Drawing.PointF> P = (x, y) => new System.Drawing.PointF((float)(ox + x * s), (float)(oy - y * s));
        int n = ct.Count; if (n < 2) return;
        // start at an on-curve point (or the midpoint of two off-curve points)
        int st = ct.FindIndex(q => q.On);
        double sx, sy;
        if (st < 0) { sx = (ct[0].X + ct[1].X) / 2; sy = (ct[0].Y + ct[1].Y) / 2; st = 0; }
        else { sx = ct[st].X; sy = ct[st].Y; st++; }
        double cx = sx, cy = sy;
        gp.StartFigure();
        double? qx = null, qy = null;
        for (int k = 0; k <= n; k++)
        {
            var pt = ct[(st + k) % n];
            double px = pt.X, py = pt.Y; bool on = pt.On;
            if (k == n) { px = sx; py = sy; on = true; }
            if (on)
            {
                if (qx == null) gp.AddLine(P(cx, cy), P(px, py));
                else Quad(gp, P, cx, cy, qx.Value, qy.Value, px, py);
                cx = px; cy = py; qx = null;
            }
            else
            {
                if (qx != null)
                {
                    double mx = (qx.Value + px) / 2, my = (qy.Value + py) / 2;
                    Quad(gp, P, cx, cy, qx.Value, qy.Value, mx, my);
                    cx = mx; cy = my;
                }
                qx = px; qy = py;
            }
        }
        gp.CloseFigure();
    }

    static void Quad(System.Drawing.Drawing2D.GraphicsPath gp, Func<double, double, System.Drawing.PointF> P, double x0, double y0, double qx, double qy, double x1, double y1)
    {
        gp.AddBezier(P(x0, y0), P(x0 + 2.0 / 3 * (qx - x0), y0 + 2.0 / 3 * (qy - y0)), P(x1 + 2.0 / 3 * (qx - x1), y1 + 2.0 / 3 * (qy - y1)), P(x1, y1));
    }

    // White fill with a 1px black outline, like the game's bitmap fonts. Cell width = pad + advance.
    public static Cell Render(Ttf ttf, int glyph, double s, double dx, double dy, int pad, int baseline, out int clipped)
    {
        const int SS = 4, H = 72, B = 48;           // work canvas: baseline at row B
        int adv = (int)Math.Round(ttf.Advance(glyph) * s);
        int W = pad + adv, WW = W + 16;
        var fill = new double[WW * H];
        using (var bmp = new System.Drawing.Bitmap(WW * SS, H * SS))
        using (var g = System.Drawing.Graphics.FromImage(bmp))
        using (var gp = new System.Drawing.Drawing2D.GraphicsPath(System.Drawing.Drawing2D.FillMode.Winding))
        {
            g.Clear(System.Drawing.Color.Black);
            foreach (var ct in ttf.Contours(glyph))
            {
                var moved = ct.ConvertAll(q => new Ttf.Pt { X = q.X + dx, Y = q.Y + dy, On = q.On });
                AddContour(gp, moved, s * SS, pad * SS, B * SS);
            }
            g.FillPath(System.Drawing.Brushes.White, gp);
            var bd = bmp.LockBits(new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            var raw = new byte[bd.Stride * bmp.Height];
            System.Runtime.InteropServices.Marshal.Copy(bd.Scan0, raw, 0, raw.Length);
            bmp.UnlockBits(bd);
            for (int y = 0; y < H; y++)
                for (int x = 0; x < WW; x++)
                {
                    int sum = 0;
                    for (int yy = 0; yy < SS; yy++) for (int xx = 0; xx < SS; xx++) sum += raw[(y * SS + yy) * bd.Stride + (x * SS + xx) * 4 + 1];
                    fill[y * WW + x] = sum / (255.0 * SS * SS);
                }
        }
        // outline = 3x3 dilation of the fill
        var px = new uint[WW * H];
        clipped = 0;
        for (int y = 0; y < H; y++)
            for (int x = 0; x < WW; x++)
            {
                double f = fill[y * WW + x], o = 0;
                for (int yy = Math.Max(0, y - 1); yy <= Math.Min(H - 1, y + 1); yy++)
                    for (int xx = Math.Max(0, x - 1); xx <= Math.Min(WW - 1, x + 1); xx++) o = Math.Max(o, fill[yy * WW + xx]);
                double a = Math.Max(f, Math.Min(1, o * 1.2));
                if (a <= 0.02) continue;
                int grey = (int)Math.Round(255 * Math.Min(1, f / a));
                px[y * WW + x] = ((uint)Math.Round(a * 255) << 24) | ((uint)grey << 16) | ((uint)grey << 8) | (uint)grey;
                if (x >= W && f > 0.3) clipped++;
            }
        int top = H, bot = -1;
        for (int y = 0; y < H; y++) for (int x = 0; x < W; x++) if (px[y * WW + x] != 0) { top = Math.Min(top, y); bot = Math.Max(bot, y); }
        var c = new Cell { W = W };
        if (bot < 0) { c.H = 1; c.VOff = baseline; c.Px = new uint[W]; return c; }
        c.H = bot - top + 1; c.VOff = baseline - (B - top);
        c.Px = new uint[W * c.H];
        for (int y = 0; y < c.H; y++) for (int x = 0; x < W; x++) c.Px[y * W + x] = px[(top + y) * WW + x];
        return c;
    }

    // Ink extent left of the origin, in pixels (marks and the left-shifted PUA forms)
    public static double LeftInk(Ttf ttf, int glyph, double s, double dx)
    {
        double m = 0;
        foreach (var ct in ttf.Contours(glyph)) foreach (var q in ct) m = Math.Min(m, (q.X + dx) * s);
        return -m;
    }

    // (code, source code, shift x, shift y) in font units; same placement rules as the Scaleform fontbuild
    public static List<int[]> Plan(Ttf ttf)
    {
        Func<int, double[]> box = delegate(int code)
        {
            double[] r = { 1e9, -1e9, 1e9, -1e9 };
            foreach (var ct in ttf.Contours(ttf.Cmap[code])) foreach (var pt in ct)
            { r[0] = Math.Min(r[0], pt.X); r[1] = Math.Max(r[1], pt.X); r[2] = Math.Min(r[2], pt.Y); r[3] = Math.Max(r[3], pt.Y); }
            return r;
        };
        double em = ttf.UnitsPerEm;
        int UP = (int)Math.Round(box(0x0E37)[3] + 0.035 * em - box(0x0E48)[2]);
        int LEFT = (int)Math.Round(ttf.Advance(ttf.Cmap[0x0E1B]) * 230.0 / 1290.0);
        int DOWN = (int)Math.Round(box(0x0E38)[3] - box(0x0E0E)[2] + 0.033 * em);
        var plan = new List<int[]>();
        for (int c = 0x0E01; c <= 0x0E5B; c++) plan.Add(new int[] { c, c, 0, (c >= 0x0E48 && c <= 0x0E4C) ? UP : 0 });
        for (int k = 0; k < 5; k++)
        {
            plan.Add(new int[] { 0xF70A + k, 0x0E48 + k, 0, 0 });
            plan.Add(new int[] { 0xF705 + k, 0x0E48 + k, -LEFT, 0 });
            plan.Add(new int[] { 0xF713 + k, 0x0E48 + k, -LEFT, UP });
        }
        int[] avSrc = { 0x0E34, 0x0E35, 0x0E36, 0x0E37 };
        for (int k = 0; k < 4; k++) plan.Add(new int[] { 0xF701 + k, avSrc[k], -LEFT, 0 });
        plan.Add(new int[] { 0xF710, 0x0E31, -LEFT, 0 });
        plan.Add(new int[] { 0xF711, 0x0E4D, -LEFT, 0 });
        plan.Add(new int[] { 0xF712, 0x0E47, -LEFT, 0 });
        plan.Add(new int[] { 0xF718, 0x0E38, 0, -DOWN });
        plan.Add(new int[] { 0xF719, 0x0E39, 0, -DOWN });
        plan.Add(new int[] { 0xF71A, 0x0E3A, 0, -DOWN });
        plan.Add(new int[] { 0xF700, 0x0E10, 0, 0 });
        plan.Add(new int[] { 0xF70F, 0x0E0D, 0, 0 });
        plan.RemoveAll(pl => !ttf.Cmap.ContainsKey(pl[1]));
        return plan;
    }
}

static class BmBuild
{
    static byte[] Tag(Package p, string name, string type, int size, byte[] value)
    {
        var ms = new MemoryStream(); var w = new BinaryWriter(ms);
        w.Write(p.Names.IndexOf(name)); w.Write(0); w.Write(p.Names.IndexOf(type)); w.Write(0); w.Write(size); w.Write(0);
        if (p.Version == 579) w.Write((short)0);
        w.Write(value);
        return ms.ToArray();
    }

    // bmbuild <pkg> <out> <ttf> <fontExport> [thaiPx]
    public static int Run(string[] args)
    {
        var p = Package.Load(args[1]);
        var ttf = new Ttf(args[3]);
        int fontExport = int.Parse(args[4]);
        var bf = BmFontData.Load(p, fontExport);
        // Thai consonant height defaults to the cap height of H without its outline
        double thaiPx = args.Length > 5 ? double.Parse(args[5], System.Globalization.CultureInfo.InvariantCulture) : bf.Chars[bf.Remap['H']].H - 2;
        // every page is merged into one atlas written over page 0; Textures then lists page 0 only
        var pagesPx = new List<uint[]>(); var pagesW = new List<int>();
        foreach (int ti in bf.Textures)
        {
            var pt = BmTexture.Load(p, ti);
            if (pt.Format != "PF_DXT5") throw new Exception("texture format " + pt.Format);
            pagesPx.Add(Dxt5.Decode(p.Data, pt.MipDataPos, pt.SizeX, pt.SizeY)); pagesW.Add(pt.SizeX);
        }
        var tx = BmTexture.Load(p, bf.Textures[0]);

        var hc = bf.Chars[bf.Remap['H']];
        int baseline = hc.VOff + hc.H - 1;                 // last row of the outline under 'H'
        double s = thaiPx / ttf.Contours(ttf.Cmap[0x0E01]).ConvertAll(ct => ct.ConvertAll(q => q.Y).Max()).Max();
        var plan = ThaiRaster.Plan(ttf);
        double left = 0;
        foreach (var pl in plan) left = Math.Max(left, ThaiRaster.LeftInk(ttf, ttf.Cmap[pl[1]], s, pl[2]));
        int pad = (int)Math.Ceiling(left) + 2;
        Console.WriteLine("baseline " + baseline + ", scale " + s.ToString("0.0000") + " px/unit, pad " + pad);

        // existing glyphs, shifted right by pad
        // the canvas skips Kerning when it steps over a space, so the space cell keeps its original width
        int spaceIdx; if (!bf.Remap.TryGetValue(' ', out spaceIdx)) spaceIdx = -1;
        var cells = new List<Cell>();
        foreach (var c in bf.Chars)
        {
            int cpad = cells.Count == spaceIdx ? 0 : pad;
            var cell = new Cell { W = c.W + cpad, H = Math.Max(1, c.H), VOff = c.VOff };
            cell.Px = new uint[cell.W * cell.H];
            for (int y = 0; y < c.H; y++) for (int x = 0; x < c.W; x++)
                cell.Px[y * cell.W + cpad + x] = pagesPx[c.Tex][(c.V + y) * pagesW[c.Tex] + c.U + x];
            cells.Add(cell);
        }
        // Thai glyphs at their cp1252 slots
        var remap = new Dictionary<int, int>(bf.Remap);
        var order = new List<int>(bf.Remap.Keys);
        int replaced = 0, added = 0, clippedTotal = 0;
        foreach (var kv in Cp1252Thai.Map)
        {
            var pl = plan.Find(q => q[0] == kv.Key);
            if (pl == null) continue;
            int clipped;
            var cell = ThaiRaster.Render(ttf, ttf.Cmap[pl[1]], s, pl[2], pl[3], pad, baseline, out clipped);
            clippedTotal += clipped;
            int idx;
            if (remap.TryGetValue(kv.Value, out idx) && idx > 0) { cells[idx] = cell; replaced++; }
            else { cells.Add(cell); remap[kv.Value] = cells.Count - 1; if (!order.Contains(kv.Value)) order.Add(kv.Value); added++; }
        }
        Console.WriteLine("thai glyphs: replaced " + replaced + ", added " + added + ", clipped ink px " + clippedTotal);

        // shelf packing, tallest first
        int AW = 256;
        var byH = new List<Cell>(cells); byH.Sort((a, b) => b.H.CompareTo(a.H));
        int cx = 0, cy = 0, rowH = 0;
        foreach (var c in byH)
        {
            if (cx + c.W > AW) { cx = 0; cy += rowH + 1; rowH = 0; }
            c.X = cx; c.Y = cy; cx += c.W + 1; rowH = Math.Max(rowH, c.H);
        }
        int AH = 64; while (AH < cy + rowH) AH *= 2;
        if (AH > AW * 2) { AW *= 2; }  // keep the page roughly square
        if (AW != 256)
        {
            cx = 0; cy = 0; rowH = 0;
            foreach (var c in byH) { if (cx + c.W > AW) { cx = 0; cy += rowH + 1; rowH = 0; } c.X = cx; c.Y = cy; cx += c.W + 1; rowH = Math.Max(rowH, c.H); }
            AH = 64; while (AH < cy + rowH) AH *= 2;
        }
        var atlas = new uint[AW * AH];
        foreach (var c in cells) for (int y = 0; y < c.H; y++) for (int x = 0; x < c.W; x++) atlas[(c.Y + y) * AW + c.X + x] = c.Px[y * c.W + x];
        // transparent texels keep white colour so filtering at glyph edges does not darken
        for (int i = 0; i < atlas.Length; i++) if (atlas[i] == 0) atlas[i] = 0x00FFFFFF;
        var dxt = Dxt5.Encode(atlas, AW, AH);
        Console.WriteLine("atlas " + AW + "x" + AH + ", " + cells.Count + " glyphs");
        if (Environment.GetEnvironmentVariable("BM_ATLAS_PNG") != null)
            using (var bmp = new System.Drawing.Bitmap(AW, AH))
            {
                var dec = Dxt5.Decode(dxt, 0, AW, AH);
                for (int y = 0; y < AH; y++) for (int x = 0; x < AW; x++) { uint v = dec[y * AW + x]; int a = (int)(v >> 24), l = (int)(v & 255) * a / 255; bmp.SetPixel(x, y, System.Drawing.Color.FromArgb(255, l, l, 40 + l * 215 / 255)); }
                bmp.Save(Environment.GetEnvironmentVariable("BM_ATLAS_PNG"), System.Drawing.Imaging.ImageFormat.Png);
            }

        // names
        var newNames = new List<string>();
        foreach (var n in new[] { "Kerning", "IntProperty" }) if (!p.Names.Contains(n)) { newNames.Add(n); p.Names.Add(n); }

        var outMs = new MemoryStream();
        outMs.Write(p.Data, 0, p.Data.Length);

        // texture export
        var d = p.Data;
        int texOff = (int)outMs.Length;
        var te = new MemoryStream();
        te.Write(d, tx.E.SerialOffset, tx.MipDataPos - tx.E.SerialOffset);
        var tb = te.ToArray();
        Func<int, int> trel = abs => abs - tx.E.SerialOffset;
        BitConverter.GetBytes(AW).CopyTo(tb, trel(tx.SizeXPos));
        BitConverter.GetBytes(AH).CopyTo(tb, trel(tx.SizeYPos));
        if (tx.MipTailPos > 0) { int l2 = 0; while ((1 << l2) < Math.Max(AW, AH)) l2++; BitConverter.GetBytes(l2).CopyTo(tb, trel(tx.MipTailPos)); }
        BitConverter.GetBytes(texOff + trel(tx.NativeStart) + 16).CopyTo(tb, trel(tx.NativeStart) + 12);   // empty SourceArt
        int hdr = trel(tx.MipDataPos) - 16;
        BitConverter.GetBytes(dxt.Length).CopyTo(tb, hdr + 4);
        BitConverter.GetBytes(dxt.Length).CopyTo(tb, hdr + 8);
        BitConverter.GetBytes(texOff + hdr + 16).CopyTo(tb, hdr + 12);
        int tailStart = tx.MipDataPos + tx.MipDataLen, tailLen = tx.E.SerialOffset + tx.E.SerialSize - tailStart;
        var tail = new byte[tailLen]; Buffer.BlockCopy(d, tailStart, tail, 0, tailLen);
        BitConverter.GetBytes(AW).CopyTo(tail, 0); BitConverter.GetBytes(AH).CopyTo(tail, 4);
        outMs.Write(tb, 0, tb.Length); outMs.Write(dxt, 0, dxt.Length); outMs.Write(tail, 0, tail.Length);
        int texSize = (int)outMs.Length - texOff;

        // font export: same tags with new Characters, plus Kerning = -pad; native CharRemap
        int fontOff = (int)outMs.Length;
        var fe = new MemoryStream(); var fw = new BinaryWriter(fe);
        fe.Write(d, bf.E.SerialOffset, 4);
        var chars = new MemoryStream(); var cw = new BinaryWriter(chars);
        cw.Write(cells.Count);
        foreach (var c in cells) { cw.Write(c.X); cw.Write(c.Y); cw.Write(c.W); cw.Write(c.H); cw.Write((byte)0); cw.Write(c.VOff); }
        Subs.Walk(p, bf.E.SerialOffset + 4, bf.E.SerialOffset + bf.E.SerialSize, delegate(int t, string n, int sp, int v, int sz)
        {
            int start = sp - 16;
            if (n == "Characters")
            {
                fe.Write(d, start, 16); fw.Write((int)chars.Length); fw.Write(0);
                if (p.Version == 579) fw.Write((short)0);
                chars.WriteTo(fe);
            }
            else if (n == "Kerning") { fe.Write(d, start, v - start); fw.Write(-pad); }
            else if (n == "Textures")
            {
                fe.Write(d, start, 16); fw.Write(8); fw.Write(0);
                if (p.Version == 579) fw.Write((short)0);
                fw.Write(1); fw.Write(bf.Textures[0]);
            }
            else fe.Write(d, start, v + sz - start);
        });
        if (!bf.HasKerning) { var kt = Tag(p, "Kerning", "IntProperty", 4, BitConverter.GetBytes(-pad)); fe.Write(kt, 0, kt.Length); }
        fe.Write(d, bf.PropsEnd - 8, 8);   // None
        fw.Write(order.Count);
        foreach (int k in order) { fw.Write((ushort)k); fw.Write((ushort)remap[k]); }
        var fb = fe.ToArray();
        outMs.Write(fb, 0, fb.Length);

        // relocated name table (only when names were added)
        int nameOff = p.NameOffset;
        if (newNames.Count > 0)
        {
            nameOff = (int)outMs.Length;
            int oldEnd = p.NameOffset;
            var nr = new BinaryReader(new MemoryStream(d)); nr.BaseStream.Position = p.NameOffset;
            ulong nameFlags = 0;
            for (int i = 0; i < p.NameCount; i++) { int len = nr.ReadInt32(); nr.BaseStream.Position += len >= 0 ? len : -len * 2; ulong fl = nr.ReadUInt64(); if (i == 0) nameFlags = fl; }
            outMs.Write(d, p.NameOffset, (int)nr.BaseStream.Position - p.NameOffset);
            var nw = new BinaryWriter(outMs);
            foreach (var n in newNames) { var b = Encoding.ASCII.GetBytes(n + "\0"); nw.Write(b.Length); nw.Write(b); nw.Write(nameFlags); }
        }
        var all = outMs.ToArray();
        BitConverter.GetBytes(texSize).CopyTo(all, tx.E.SizeFieldPos); BitConverter.GetBytes(texOff).CopyTo(all, tx.E.SizeFieldPos + 4);
        BitConverter.GetBytes(fb.Length).CopyTo(all, bf.E.SizeFieldPos); BitConverter.GetBytes(fontOff).CopyTo(all, bf.E.SizeFieldPos + 4);
        if (newNames.Count > 0)
        {
            BitConverter.GetBytes(p.Names.Count).CopyTo(all, p.NameCountPos);
            BitConverter.GetBytes(nameOff).CopyTo(all, p.NameCountPos + 4);
            BitConverter.GetBytes(p.Names.Count).CopyTo(all, p.GenerationsPos + (p.GenerationCount - 1) * 12 + 4);
        }
        File.WriteAllBytes(args[2], all);
        Console.WriteLine("wrote " + args[2] + " (" + all.Length + " bytes), names +" + newNames.Count);
        return 0;
    }

    // bmrender <pkg> <fontExport> <out.png> <lines.txt> : draw lines the way UCanvas does (advance = USize + Kerning)
    public static int Render(string[] args)
    {
        var p = Package.Load(args[1]);
        var bf = BmFontData.Load(p, int.Parse(args[2]));
        var tx = BmTexture.Load(p, bf.Textures[0]);
        var px = Dxt5.Decode(p.Data, tx.MipDataPos, tx.SizeX, tx.SizeY);
        var lines = File.ReadAllLines(args[4], Encoding.UTF8);
        int Z = 3, LH = 30, W = 900;
        using (var bmp = new System.Drawing.Bitmap(W * Z, (lines.Length * LH + 10) * Z))
        {
            using (var g = System.Drawing.Graphics.FromImage(bmp)) g.Clear(System.Drawing.Color.FromArgb(70, 80, 95));
            for (int li = 0; li < lines.Length; li++)
            {
                string t = Cp1252Thai.Encode(ThaiShaper.Shape(lines[li]));
                int pen = 10, top = 5 + li * LH;
                foreach (char ch in t)
                {
                    int idx; if (!bf.Remap.TryGetValue(ch, out idx)) idx = 0;
                    var c = bf.Chars[idx];
                    for (int y = 0; y < c.H; y++) for (int x = 0; x < c.W; x++)
                    {
                        uint v = px[(c.V + y) * tx.SizeX + c.U + x]; int a = (int)(v >> 24); if (a == 0) continue;
                        int X = pen + x, Y = top + c.VOff + y; if (X < 0 || X >= W) continue;
                        for (int zy = 0; zy < Z; zy++) for (int zx = 0; zx < Z; zx++)
                        {
                            var o = bmp.GetPixel(X * Z + zx, Y * Z + zy);
                            int l = (int)(v & 255);
                            bmp.SetPixel(X * Z + zx, Y * Z + zy, System.Drawing.Color.FromArgb((o.R * (255 - a) + l * a) / 255, (o.G * (255 - a) + l * a) / 255, (o.B * (255 - a) + l * a) / 255));
                        }
                    }
                    pen += c.W + (ch == ' ' ? 0 : bf.Kerning);   // the game skips Kerning on spaces
                }
            }
            bmp.Save(args[3], System.Drawing.Imaging.ImageFormat.Png);
        }
        Console.WriteLine("saved " + args[3]);
        return 0;
    }
}

// Texture2D export with one inline mip: props (SizeX, SizeY, Format, MipTailBaseIdx ...), native:
// SourceArt bulk (16 bytes header, empty), mip count, then per mip: bulk header (flags, count, size, offset) + data + SizeX + SizeY
class BmTexture
{
    public Package P; public Exp E;
    public int SizeX, SizeY, SizeXPos, SizeYPos, MipTailPos; public string Format;
    public int NativeStart, MipDataPos, MipDataLen;

    public static BmTexture Load(Package p, int exportIndex)
    {
        var t = new BmTexture { P = p, E = p.Exports[exportIndex - 1] };
        var d = p.Data;
        t.NativeStart = Subs.Walk(p, t.E.SerialOffset + 4, t.E.SerialOffset + t.E.SerialSize, delegate(int ty, string n, int sp, int v, int s)
        {
            if (n == "SizeX") { t.SizeX = BitConverter.ToInt32(d, v); t.SizeXPos = v; }
            if (n == "SizeY") { t.SizeY = BitConverter.ToInt32(d, v); t.SizeYPos = v; }
            if (n == "MipTailBaseIdx") t.MipTailPos = v;
            if (n == "Format") t.Format = Program.NameAt(p, d, v);
        });
        int q = t.NativeStart + 16;   // empty SourceArt bulk
        int mips = BitConverter.ToInt32(d, q); q += 4;
        if (mips != 1) throw new Exception("texture has " + mips + " mips");
        t.MipDataLen = BitConverter.ToInt32(d, q + 8);
        t.MipDataPos = q + 16;
        return t;
    }
}
