// TrueType -> SWF DefineFont3 glyph merge, plus legacy (PUA) Thai shaping.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

class BitWriter
{
    List<byte> buf = new List<byte>();
    int cur, nbits;
    public void UB(int n, long v) { for (int i = n - 1; i >= 0; i--) { cur = (cur << 1) | (int)((v >> i) & 1); if (++nbits == 8) { buf.Add((byte)cur); cur = 0; nbits = 0; } } }
    public void SB(int n, int v) { UB(n, v & ((1L << n) - 1)); }
    public void Align() { if (nbits > 0) UB(8 - nbits, 0); }
    public byte[] ToArray() { Align(); return buf.ToArray(); }
    public static int SBits(int v) { int n = 1; while (v < -(1 << (n - 1)) || v > (1 << (n - 1)) - 1) n++; return n; }
}

class BitReader
{
    byte[] d; int pos; int bit;
    public BitReader(byte[] d, int pos) { this.d = d; this.pos = pos; }
    public int UB(int n) { int v = 0; for (int i = 0; i < n; i++) { v = (v << 1) | ((d[pos] >> (7 - bit)) & 1); if (++bit == 8) { bit = 0; pos++; } } return v; }
    public int SB(int n) { int v = UB(n); if (n > 0 && (v & (1 << (n - 1))) != 0) v -= 1 << n; return v; }
    public int Align() { if (bit > 0) { bit = 0; pos++; } return pos; }
    public int Pos { get { return pos; } }
}

class Ttf
{
    byte[] d;
    Dictionary<string, int> tables = new Dictionary<string, int>();
    public int UnitsPerEm, NumGlyphs, NumHMetrics, LocFormat;
    public Dictionary<int, int> Cmap = new Dictionary<int, int>();

    int U16(int o) { return (d[o] << 8) | d[o + 1]; }
    int S16(int o) { return (short)U16(o); }
    int U32(int o) { return (d[o] << 24) | (d[o + 1] << 16) | (d[o + 2] << 8) | d[o + 3]; }

    public Ttf(string path)
    {
        d = File.ReadAllBytes(path);
        int n = U16(4);
        for (int i = 0; i < n; i++) { int r = 12 + i * 16; tables[Encoding.ASCII.GetString(d, r, 4)] = U32(r + 8); }
        int head = tables["head"]; UnitsPerEm = U16(head + 18); LocFormat = S16(head + 50);
        NumGlyphs = U16(tables["maxp"] + 4);
        NumHMetrics = U16(tables["hhea"] + 34);
        int cmap = tables["cmap"], sub = -1;
        int nsub = U16(cmap + 2);
        for (int i = 0; i < nsub; i++)
        {
            int pid = U16(cmap + 4 + i * 8), eid = U16(cmap + 6 + i * 8), off = U32(cmap + 8 + i * 8);
            if (pid == 3 && eid == 1 && U16(cmap + off) == 4) sub = cmap + off;
        }
        int seg = U16(sub + 6) / 2;
        int ends = sub + 14, starts = ends + seg * 2 + 2, deltas = starts + seg * 2, ranges = deltas + seg * 2;
        for (int s = 0; s < seg; s++)
        {
            int e = U16(ends + s * 2), st = U16(starts + s * 2), dl = S16(deltas + s * 2), ro = U16(ranges + s * 2);
            for (int c = st; c <= e && c != 0xFFFF; c++)
            {
                int g;
                if (ro == 0) g = (c + dl) & 0xFFFF;
                else { int gi = ranges + s * 2 + ro + (c - st) * 2; g = U16(gi); if (g != 0) g = (g + dl) & 0xFFFF; }
                if (g != 0) Cmap[c] = g;
            }
        }
    }

    public int Advance(int g) { int h = tables["hmtx"]; return U16(h + Math.Min(g, NumHMetrics - 1) * 4); }

    int GlyfOffset(int g, out int len)
    {
        int loca = tables["loca"], o1, o2;
        if (LocFormat == 0) { o1 = U16(loca + g * 2) * 2; o2 = U16(loca + g * 2 + 2) * 2; }
        else { o1 = U32(loca + g * 4); o2 = U32(loca + g * 4 + 4); }
        len = o2 - o1;
        return tables["glyf"] + o1;
    }

    public class Pt { public double X, Y; public bool On; }

    // Contours in font units (y up). Handles composite glyphs.
    public List<List<Pt>> Contours(int g)
    {
        var res = new List<List<Pt>>();
        int len; int o = GlyfOffset(g, out len);
        if (len == 0) return res;
        int nc = S16(o);
        if (nc >= 0)
        {
            int p = o + 10;
            var endPts = new int[nc];
            for (int i = 0; i < nc; i++) endPts[i] = U16(p + i * 2);
            p += nc * 2;
            int npts = nc == 0 ? 0 : endPts[nc - 1] + 1;
            p += 2 + U16(p);
            var flags = new byte[npts];
            for (int i = 0; i < npts; )
            {
                byte f = d[p++]; flags[i++] = f;
                if ((f & 8) != 0) { int r = d[p++]; while (r-- > 0 && i < npts) flags[i++] = f; }
            }
            var xs = new int[npts]; var ys = new int[npts];
            int v = 0;
            for (int i = 0; i < npts; i++)
            {
                byte f = flags[i];
                if ((f & 2) != 0) { int dx = d[p++]; v += (f & 0x10) != 0 ? dx : -dx; }
                else if ((f & 0x10) == 0) { v += S16(p); p += 2; }
                xs[i] = v;
            }
            v = 0;
            for (int i = 0; i < npts; i++)
            {
                byte f = flags[i];
                if ((f & 4) != 0) { int dy = d[p++]; v += (f & 0x20) != 0 ? dy : -dy; }
                else if ((f & 0x20) == 0) { v += S16(p); p += 2; }
                ys[i] = v;
            }
            int start = 0;
            for (int c = 0; c < nc; c++)
            {
                var ct = new List<Pt>();
                for (int i = start; i <= endPts[c]; i++) ct.Add(new Pt { X = xs[i], Y = ys[i], On = (flags[i] & 1) != 0 });
                if (ct.Count > 0) res.Add(ct);
                start = endPts[c] + 1;
            }
        }
        else
        {
            int p = o + 10;
            while (true)
            {
                int fl = U16(p), gi = U16(p + 2); p += 4;
                double dx, dy;
                if ((fl & 1) != 0) { dx = S16(p); dy = S16(p + 2); p += 4; }
                else { dx = (sbyte)d[p]; dy = (sbyte)d[p + 1]; p += 2; }
                if ((fl & 2) == 0) { dx = 0; dy = 0; } // point matching not supported
                double a = 1, b = 0, c2 = 0, e = 1;
                if ((fl & 8) != 0) { a = e = S16(p) / 16384.0; p += 2; }
                else if ((fl & 0x40) != 0) { a = S16(p) / 16384.0; e = S16(p + 2) / 16384.0; p += 4; }
                else if ((fl & 0x80) != 0) { a = S16(p) / 16384.0; b = S16(p + 2) / 16384.0; c2 = S16(p + 4) / 16384.0; e = S16(p + 6) / 16384.0; p += 8; }
                foreach (var ct in Contours(gi))
                {
                    var nct = new List<Pt>();
                    foreach (var pt in ct) nct.Add(new Pt { X = pt.X * a + pt.Y * c2 + dx, Y = pt.X * b + pt.Y * e + dy, On = pt.On });
                    res.Add(nct);
                }
                if ((fl & 0x20) == 0) break;
            }
        }
        return res;
    }
}

class DefineFont3
{
    public int Id, Flags, Lang;
    public string Name;
    public List<byte[]> Shapes = new List<byte[]>();
    public List<int> Codes = new List<int>();
    public int Ascent, Descent, Leading;
    public List<int> Advances = new List<int>();
    public List<int[]> Bounds = new List<int[]>();   // xmin xmax ymin ymax
    public List<int[]> Kerning = new List<int[]>();

    public static DefineFont3 Parse(byte[] d, int o)
    {
        var f = new DefineFont3();
        f.Id = BitConverter.ToUInt16(d, o); f.Flags = d[o + 2]; f.Lang = d[o + 3];
        int nl = d[o + 4]; f.Name = Encoding.UTF8.GetString(d, o + 5, nl).TrimEnd('\0');
        int p = o + 5 + nl;
        int ng = BitConverter.ToUInt16(d, p); p += 2;
        bool wideOff = (f.Flags & 0x08) != 0;
        int tbl = p;
        var offs = new int[ng + 1];
        for (int i = 0; i <= ng; i++) { offs[i] = wideOff ? BitConverter.ToInt32(d, p) : BitConverter.ToUInt16(d, p); p += wideOff ? 4 : 2; }
        for (int i = 0; i < ng; i++) { var s = new byte[offs[i + 1] - offs[i]]; Buffer.BlockCopy(d, tbl + offs[i], s, 0, s.Length); f.Shapes.Add(s); }
        p = tbl + offs[ng];
        for (int i = 0; i < ng; i++) { f.Codes.Add(BitConverter.ToUInt16(d, p)); p += 2; }
        if ((f.Flags & 0x80) != 0)
        {
            f.Ascent = BitConverter.ToUInt16(d, p); f.Descent = BitConverter.ToUInt16(d, p + 2); f.Leading = BitConverter.ToInt16(d, p + 4); p += 6;
            for (int i = 0; i < ng; i++) { f.Advances.Add(BitConverter.ToInt16(d, p)); p += 2; }
            for (int i = 0; i < ng; i++)
            {
                var br = new BitReader(d, p); int nb = br.UB(5);
                f.Bounds.Add(new int[] { br.SB(nb), br.SB(nb), br.SB(nb), br.SB(nb) });
                p = br.Align();
            }
            int kc = BitConverter.ToUInt16(d, p); p += 2;
            for (int i = 0; i < kc; i++) { f.Kerning.Add(new int[] { BitConverter.ToUInt16(d, p), BitConverter.ToUInt16(d, p + 2), BitConverter.ToInt16(d, p + 4) }); p += 6; }
        }
        return f;
    }

    public byte[] Serialize()
    {
        // glyphs sorted by code (Flash requires an ascending code table)
        var order = new List<int>();
        for (int i = 0; i < Codes.Count; i++) order.Add(i);
        order.Sort(delegate(int a, int b) { return Codes[a].CompareTo(Codes[b]); });
        var ms = new MemoryStream(); var w = new BinaryWriter(ms);
        w.Write((ushort)Id); w.Write((byte)(Flags | 0x08)); w.Write((byte)Lang);
        var nb = Encoding.UTF8.GetBytes(Name + "\0"); w.Write((byte)nb.Length); w.Write(nb);
        w.Write((ushort)order.Count);
        int off = (order.Count + 1) * 4;
        foreach (int i in order) { w.Write(off); off += Shapes[i].Length; }
        w.Write(off);
        foreach (int i in order) w.Write(Shapes[i]);
        foreach (int i in order) w.Write((ushort)Codes[i]);
        if ((Flags & 0x80) != 0)
        {
            w.Write((ushort)Ascent); w.Write((ushort)Descent); w.Write((short)Leading);
            foreach (int i in order) w.Write((short)Advances[i]);
            foreach (int i in order)
            {
                var b = Bounds[i]; var bw = new BitWriter();
                int n = 1; foreach (int v in b) n = Math.Max(n, BitWriter.SBits(v));
                bw.UB(5, n); foreach (int v in b) bw.SB(n, v);
                w.Write(bw.ToArray());
            }
            w.Write((ushort)Kerning.Count);
            foreach (var k in Kerning) { w.Write((ushort)k[0]); w.Write((ushort)k[1]); w.Write((short)k[2]); }
        }
        return ms.ToArray();
    }

    public int IndexOf(int code) { return Codes.IndexOf(code); }

    // Decode a glyph SHAPE: returns bbox {xmin,xmax,ymin,ymax} and signed area of the first contour (y down)
    public static void Measure(byte[] s, out int[] bbox, out double firstArea)
    {
        var br = new BitReader(s, 0);
        int nf = br.UB(4), nl = br.UB(4);
        int x = 0, y = 0, contour = 0;
        int xmin = int.MaxValue, xmax = int.MinValue, ymin = int.MaxValue, ymax = int.MinValue;
        double area = 0; firstArea = 0;
        while (true)
        {
            int type = br.UB(1);
            if (type == 0)
            {
                int flags = br.UB(5);
                if (flags == 0) break;
                if ((flags & 1) != 0)
                {
                    if (contour == 1) firstArea = area;
                    contour++; area = 0;
                    int mb = br.UB(5); x = br.SB(mb); y = br.SB(mb);
                }
                if ((flags & 2) != 0) br.UB(nf);
                if ((flags & 4) != 0) br.UB(nf);
                if ((flags & 8) != 0) br.UB(nl);
                if ((flags & 16) != 0) throw new Exception("new styles in glyph");
            }
            else
            {
                int nx, ny;
                if (br.UB(1) == 1)
                {
                    int n = br.UB(4) + 2;
                    if (br.UB(1) == 1) { nx = x + br.SB(n); ny = y + br.SB(n); }
                    else if (br.UB(1) == 1) { nx = x; ny = y + br.SB(n); }
                    else { nx = x + br.SB(n); ny = y; }
                }
                else
                {
                    int n = br.UB(4) + 2;
                    int cx = x + br.SB(n), cy = y + br.SB(n);
                    nx = cx + br.SB(n); ny = cy + br.SB(n);
                }
                area += (double)x * ny - (double)nx * y;
                x = nx; y = ny;
            }
            xmin = Math.Min(xmin, x); xmax = Math.Max(xmax, x); ymin = Math.Min(ymin, y); ymax = Math.Max(ymax, y);
        }
        if (contour == 1) firstArea = area;
        bbox = new int[] { xmin, xmax, ymin, ymax };
    }

    // Encode TTF contours as a DefineFont3 glyph SHAPE (EM square = 20480 units, y down)
    public static byte[] EncodeShape(List<List<Ttf.Pt>> contours, double scale, double dx, double dy, out int[] bounds, bool reverse)
    {
        if (reverse)
        {
            var rc = new List<List<Ttf.Pt>>();
            foreach (var c in contours) { var r = new List<Ttf.Pt>(c); r.Reverse(); rc.Add(r); }
            contours = rc;
        }
        var w = new BitWriter();
        w.UB(4, 1); w.UB(4, 0);   // 1 fill bit, 0 line bits
        int cx = 0, cy = 0; bool first = true;
        int xmin = int.MaxValue, xmax = int.MinValue, ymin = int.MaxValue, ymax = int.MinValue;
        Func<double, int> RX = delegate(double x) { return (int)Math.Round(x * scale + dx); };
        Func<double, int> RY = delegate(double y) { return (int)Math.Round(-y * scale + dy); };
        foreach (var ct in contours)
        {
            int n = ct.Count;
            if (n < 2) continue;
            // expand to explicit on/off sequence starting on an on-curve point
            var pts = new List<Ttf.Pt>();
            int s = -1;
            for (int i = 0; i < n; i++) if (ct[i].On) { s = i; break; }
            Ttf.Pt startPt;
            if (s < 0) { startPt = new Ttf.Pt { X = (ct[0].X + ct[n - 1].X) / 2, Y = (ct[0].Y + ct[n - 1].Y) / 2, On = true }; s = 0; for (int i = 0; i < n; i++) pts.Add(ct[i]); }
            else { startPt = ct[s]; for (int i = 1; i <= n; i++) pts.Add(ct[(s + i) % n]); }
            // moveTo
            int mx = RX(startPt.X), my = RY(startPt.Y);
            int mb = Math.Max(BitWriter.SBits(mx), BitWriter.SBits(my));
            // type, newStyles, lineStyle, fillStyle1, fillStyle0, moveTo  (game fonts use FillStyle0)
            w.UB(1, 0); w.UB(1, 0); w.UB(1, 0); w.UB(1, 0); w.UB(1, first ? 1 : 0); w.UB(1, 1);
            w.UB(5, mb); w.SB(mb, mx); w.SB(mb, my);
            if (first) w.UB(1, 1);
            first = false;
            cx = mx; cy = my;
            Upd(ref xmin, ref xmax, ref ymin, ref ymax, cx, cy);
            Ttf.Pt prev = startPt;
            Ttf.Pt pendingCtrl = null;
            foreach (var pt in pts)
            {
                if (pt.On)
                {
                    if (pendingCtrl == null) Line(w, ref cx, ref cy, RX(pt.X), RY(pt.Y));
                    else { Curve(w, ref cx, ref cy, RX(pendingCtrl.X), RY(pendingCtrl.Y), RX(pt.X), RY(pt.Y)); Upd(ref xmin, ref xmax, ref ymin, ref ymax, RX(pendingCtrl.X), RY(pendingCtrl.Y)); pendingCtrl = null; }
                }
                else
                {
                    if (pendingCtrl != null)
                    {
                        var mid = new Ttf.Pt { X = (pendingCtrl.X + pt.X) / 2, Y = (pendingCtrl.Y + pt.Y) / 2, On = true };
                        Curve(w, ref cx, ref cy, RX(pendingCtrl.X), RY(pendingCtrl.Y), RX(mid.X), RY(mid.Y));
                        Upd(ref xmin, ref xmax, ref ymin, ref ymax, RX(pendingCtrl.X), RY(pendingCtrl.Y));
                    }
                    pendingCtrl = pt;
                }
                Upd(ref xmin, ref xmax, ref ymin, ref ymax, cx, cy);
            }
            if (pendingCtrl != null) { Curve(w, ref cx, ref cy, RX(pendingCtrl.X), RY(pendingCtrl.Y), mx, my); Upd(ref xmin, ref xmax, ref ymin, ref ymax, RX(pendingCtrl.X), RY(pendingCtrl.Y)); }
            else if (cx != mx || cy != my) Line(w, ref cx, ref cy, mx, my);
        }
        w.UB(6, 0); // end shape
        if (xmin == int.MaxValue) { xmin = xmax = ymin = ymax = 0; }
        bounds = new int[] { xmin, xmax, ymin, ymax };
        return w.ToArray();
    }

    static void Upd(ref int xmin, ref int xmax, ref int ymin, ref int ymax, int x, int y)
    { xmin = Math.Min(xmin, x); xmax = Math.Max(xmax, x); ymin = Math.Min(ymin, y); ymax = Math.Max(ymax, y); }

    static void Line(BitWriter w, ref int cx, ref int cy, int x, int y)
    {
        int ddx = x - cx, ddy = y - cy;
        if (ddx == 0 && ddy == 0) return;
        int n = Math.Max(2, Math.Max(BitWriter.SBits(ddx), BitWriter.SBits(ddy)));
        w.UB(1, 1); w.UB(1, 1); w.UB(4, n - 2); w.UB(1, 1); w.SB(n, ddx); w.SB(n, ddy);
        cx = x; cy = y;
    }

    static void Curve(BitWriter w, ref int cx, ref int cy, int qx, int qy, int x, int y)
    {
        int c1 = qx - cx, c2 = qy - cy, a1 = x - qx, a2 = y - qy;
        if (c1 == 0 && c2 == 0 && a1 == 0 && a2 == 0) return;
        int n = 2; foreach (int v in new int[] { c1, c2, a1, a2 }) n = Math.Max(n, BitWriter.SBits(v));
        w.UB(1, 1); w.UB(1, 0); w.UB(4, n - 2); w.SB(n, c1); w.SB(n, c2); w.SB(n, a1); w.SB(n, a2);
        cx = x; cy = y;
    }
}

static class GlyphRender
{
    // Add a DefineFont3 glyph to a GraphicsPath at pen position (px, py) with scale s (pixels per font unit)
    public static void AddGlyph(System.Drawing.Drawing2D.GraphicsPath gp, byte[] shape, float px, float py, float s)
    {
        var br = new BitReader(shape, 0);
        int nf = br.UB(4), nl = br.UB(4);
        int x = 0, y = 0;
        bool open = false;
        while (true)
        {
            if (br.UB(1) == 0)
            {
                int flags = br.UB(5);
                if (flags == 0) break;
                if ((flags & 1) != 0) { int mb = br.UB(5); x = br.SB(mb); y = br.SB(mb); if (open) gp.CloseFigure(); gp.StartFigure(); open = true; }
                if ((flags & 2) != 0) br.UB(nf);
                if ((flags & 4) != 0) br.UB(nf);
                if ((flags & 8) != 0) br.UB(nl);
            }
            else if (br.UB(1) == 1)
            {
                int n = br.UB(4) + 2, nx, ny;
                if (br.UB(1) == 1) { nx = x + br.SB(n); ny = y + br.SB(n); }
                else if (br.UB(1) == 1) { nx = x; ny = y + br.SB(n); }
                else { nx = x + br.SB(n); ny = y; }
                gp.AddLine(px + x * s, py + y * s, px + nx * s, py + ny * s);
                x = nx; y = ny;
            }
            else
            {
                int n = br.UB(4) + 2;
                int cx = x + br.SB(n), cy = y + br.SB(n);
                int nx = cx + br.SB(n), ny = cy + br.SB(n);
                float c1x = x + 2f / 3 * (cx - x), c1y = y + 2f / 3 * (cy - y);
                float c2x = nx + 2f / 3 * (cx - nx), c2y = ny + 2f / 3 * (cy - ny);
                gp.AddBezier(px + x * s, py + y * s, px + c1x * s, py + c1y * s, px + c2x * s, py + c2y * s, px + nx * s, py + ny * s);
                x = nx; y = ny;
            }
        }
        if (open) gp.CloseFigure();
    }
}

// Translation table "source<TAB>thai". The source may be plain text or a hash of it, so the released mod
// does not have to ship the game's English text: "#s" + FNV-1a64(text) (exact) or "#i" + FNV-1a64(lower(trim(text))).
class TrTable
{
    readonly Dictionary<string, string> map;
    public TrTable(string path, bool ignoreCase)
    {
        map = new Dictionary<string, string>(ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
        foreach (var line in File.ReadAllLines(path, Encoding.UTF8))
        {
            int tab = line.IndexOf('\t'); if (tab <= 0) continue;
            map[line.Substring(0, tab)] = line.Substring(tab + 1);
        }
    }
    public static string Hash(string s)
    {
        ulong h = 14695981039346656037UL;
        foreach (byte b in Encoding.UTF8.GetBytes(s)) { h ^= b; h *= 1099511628211UL; }
        return h.ToString("x16");
    }
    public static string KeyExact(string s) { return "#s" + Hash(s); }
    public static string KeyLoose(string s) { return "#i" + Hash(s.Trim().ToLowerInvariant()); }
    public bool TryGet(string key, out string th) { return map.TryGetValue(key, out th); }
    public bool TryText(string s, out string th)
    {
        return map.TryGetValue(s, out th) || map.TryGetValue(s.Trim(), out th)
            || map.TryGetValue(KeyExact(s), out th) || map.TryGetValue(KeyLoose(s), out th);
    }
}

// Splits long unbroken Thai runs with spaces at dictionary word boundaries (Windows ICU),
// because the game's text layout only wraps lines at spaces.
static class ThaiWrap
{
    [System.Runtime.InteropServices.DllImport("icu.dll", CallingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl)]
    static extern IntPtr ubrk_open(int type, [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPStr)] string locale, IntPtr text, int textLength, ref int status);
    [System.Runtime.InteropServices.DllImport("icu.dll", CallingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl)]
    static extern int ubrk_first(IntPtr bi);
    [System.Runtime.InteropServices.DllImport("icu.dll", CallingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl)]
    static extern int ubrk_next(IntPtr bi);
    [System.Runtime.InteropServices.DllImport("icu.dll", CallingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl)]
    static extern void ubrk_close(IntPtr bi);

    static bool IsMark(char c) { return c == 'ั' || (c >= 'ิ' && c <= 'ฺ') || (c >= '็' && c <= '๎'); }
    static int Visible(string s, int a, int b) { int n = 0; for (int i = a; i < b; i++) if (!IsMark(s[i])) n++; return n; }

    public static List<int> Boundaries(string s)
    {
        var res = new List<int>();
        char[] buf = s.ToCharArray();
        var h = System.Runtime.InteropServices.GCHandle.Alloc(buf, System.Runtime.InteropServices.GCHandleType.Pinned);
        try
        {
            int status = 0;
            IntPtr bi = ubrk_open(1, "th", h.AddrOfPinnedObject(), buf.Length, ref status);
            if (status > 0 || bi == IntPtr.Zero) return null;
            for (int p = ubrk_first(bi); p >= 0; p = ubrk_next(bi)) res.Add(p);
            ubrk_close(bi);
        }
        finally { h.Free(); }
        return res;
    }

    static List<string> noBreak;
    static List<string> NoBreak()
    {
        if (noBreak != null) return noBreak;
        noBreak = new List<string>();
        string f = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "nobreak.txt");
        if (File.Exists(f))
            foreach (var l in File.ReadAllLines(f, Encoding.UTF8)) { var w = l.Trim(); if (w.Length > 1 && !w.StartsWith("#")) noBreak.Add(w); }
        return noBreak;
    }

    // maxRun: longest allowed run of visible characters without a space
    public static string Apply(string s, int maxRun)
    {
        var locked = new bool[s.Length + 1];
        foreach (var w in NoBreak())
            for (int p = s.IndexOf(w, StringComparison.Ordinal); p >= 0; p = s.IndexOf(w, p + 1, StringComparison.Ordinal))
                for (int k = p + 1; k < p + w.Length; k++) locked[k] = true;
        bool longRun = false;
        foreach (var w in s.Split(' ')) if (Visible(w, 0, w.Length) > maxRun) { longRun = true; break; }
        if (!longRun) return s;
        List<int> bounds;
        try { bounds = Boundaries(s); } catch { return s; }
        if (bounds == null) return s;
        var cut = new HashSet<int>();
        int runStart = 0;        // index after last space or inserted cut
        int lastGood = -1;       // last word boundary inside the current run
        int bi = 0;
        for (int i = 0; i <= s.Length; i++)
        {
            if (i == s.Length || s[i] == ' ') { runStart = i + 1; lastGood = -1; continue; }
            while (bi < bounds.Count && bounds[bi] < i) bi++;
            bool isBound = bi < bounds.Count && bounds[bi] == i && i > runStart && !locked[i]
                && s[i] >= '฀' && s[i] <= '๿' && !IsMark(s[i]) && s[i] != 'ๆ' && s[i] != 'ฯ'
                && s[i - 1] != '(' && s[i - 1] != '"' && s[i - 1] != '\''
                && !(i + 1 < s.Length && s[i + 1] == '์') && !(i + 2 < s.Length && s[i + 2] == '์');
            if (isBound) lastGood = i;
            if (Visible(s, runStart, i + 1) > maxRun && lastGood > runStart)
            {
                cut.Add(lastGood);
                runStart = lastGood;
                lastGood = -1;
            }
        }
        if (cut.Count == 0) return s;
        var sb = new StringBuilder();
        for (int i = 0; i < s.Length; i++) { if (cut.Contains(i)) sb.Append(' '); sb.Append(s[i]); }
        return sb.ToString();
    }
}

// Arkham Asylum passes text through the system ANSI code page (WideChar -> ANSI -> WideChar) before it is
// drawn, so any character outside that code page becomes '?'. The text is therefore stored in characters that
// survive the round trip, and the font builders draw the Thai glyphs at those code points.
// ASYLUM_CP selects the target code page (the installer reads the user's ACP):
//   1252 - Thai and the shaper's PUA forms move into cp1252's upper half (Latin-1 glyphs are replaced)
//   874  - Thai survives as is; only the PUA forms move, into cp874's undefined bytes (C1 range, F8C1-F8C8)
static class Cp1252Thai
{
    public static readonly bool IsUnicode = Environment.GetEnvironmentVariable("ASYLUM_CP") == "UNICODE";
    public static readonly int CodePage = IsUnicode ? 65001 : (Environment.GetEnvironmentVariable("ASYLUM_CP") == "874" ? 874 : 1252);

    // cp1252: undefined bytes, nbsp / soft hyphen, and punctuation that English text keeps using
    static readonly int[] Reserved1252 = { 0x81, 0x8D, 0x8F, 0x90, 0x9D, 0xA0, 0xAD, 0x85, 0x91, 0x92, 0x93, 0x94, 0x96, 0x97, 0x99, 0xA9, 0xAE, 0xB0 };
    // cp874 bytes with no character: they round-trip through Windows as U+0081.. and U+F8C1-F8C8
    static readonly int[] Free874 = { 0xDB, 0xDC, 0xDD, 0xDE, 0xFC, 0xFD, 0xFE, 0xFF, 0x81, 0x82, 0x83, 0x84, 0x86, 0x87, 0x88, 0x89, 0x8A, 0x8B, 0x8C, 0x8E, 0x98, 0x99, 0x9A, 0x9B, 0x9C, 0x9E, 0x9F, 0x8D, 0x8F, 0x90, 0x9D };

    static Dictionary<char, char> toSlot;
    // character -> stored character. In 874 mode Thai maps to itself, so font builders also add those glyphs.
    public static Dictionary<char, char> Map
    {
        get
        {
            if (toSlot != null) return toSlot;
            var thai = new List<char>();
            for (int c = 0x0E01; c <= 0x0E4D; c++)
            {
                // obsolete or never used by the translation: ฃ ฅ, unassigned 0E3B-0E3E
                if (c == 0x0E03 || c == 0x0E05 || (c >= 0x0E3B && c <= 0x0E3E)) continue;
                thai.Add((char)c);
            }
            var pua = new List<char>();
            for (int c = 0xF700; c <= 0xF71A; c++) pua.Add((char)c);
            toSlot = new Dictionary<char, char>();
            if (IsUnicode)
            {
                foreach (char c in thai) toSlot[c] = c;
                foreach (char c in pua) toSlot[c] = c;
                return toSlot;
            }
            var enc = Encoding.GetEncoding(CodePage);
            var chars = new List<char>();
            var slots = new List<char>();
            if (CodePage == 1252)
            {
                chars.AddRange(thai); chars.AddRange(pua);
                for (int b = 0x80; b <= 0xFF; b++)
                    if (Array.IndexOf(Reserved1252, b) < 0) slots.Add(enc.GetString(new byte[] { (byte)b })[0]);
            }
            else
            {
                foreach (char c in thai) toSlot[c] = c;
                chars.AddRange(pua);
                foreach (int b in Free874) slots.Add(enc.GetString(new byte[] { (byte)b })[0]);
            }
            if (chars.Count > slots.Count) throw new Exception("cp" + CodePage + " slots: need " + chars.Count + ", have " + slots.Count);
            for (int i = 0; i < chars.Count; i++) toSlot[chars[i]] = slots[i];
            return toSlot;
        }
    }

    public static string Encode(string s)
    {
        if (IsUnicode) return s;
        var enc = Encoding.GetEncoding(CodePage);
        var sb = new StringBuilder(s.Length);
        foreach (char c in s)
        {
            char m;
            if (c >= 0x0E50 && c <= 0x0E59) sb.Append((char)('0' + c - 0x0E50));  // Thai digits -> ASCII
            else if (Map.TryGetValue(c, out m)) sb.Append(m);
            else if (c > 0x7F && enc.GetString(enc.GetBytes(c.ToString())) != c.ToString())
                throw new Exception("not representable in cp" + CodePage + ": U+" + ((int)c).ToString("X4") + " in \"" + s + "\"");
            else sb.Append(c);
        }
        return sb.ToString();
    }
}

// Legacy Thai shaping onto the Windows/Apple PUA glyphs (U+F700-F71A), following HarfBuzz's thai fallback.
static class ThaiShaper
{
    const int NOP = 0, SD = 1, SL = 2, SDL = 3, RD = 4;
    static int ConsType(char c)
    {
        switch (c)
        {
            case 'ป': case 'ฝ': case 'ฟ': case 'ฬ': return 1; // AC ascender
            case 'ญ': case 'ฐ': return 2;                               // RC removable descender
            case 'ฎ': case 'ฏ': return 3;                               // DC strict descender
        }
        return (c >= 'ก' && c <= 'ฮ') ? 0 : 4;                          // NC / not consonant
    }
    static int MarkType(char c)
    {
        switch (c)
        {
            case 'ั': case 'ิ': case 'ี': case 'ึ': case 'ื': case '็': case 'ํ': return 0; // AV
            case 'ุ': case 'ู': case 'ฺ': return 1; // BV
            case '่': case '้': case '๊': case '๋': case '์': return 2; // T
        }
        return 3;
    }
    // [state][mark] -> action, next   (above: T0..T3, below: B0..B2)
    static readonly int[,,] Above = {
        { {NOP,3}, {NOP,0}, {SD,3}, {NOP,3} },
        { {SL,2},  {NOP,1}, {SDL,2},{NOP,3} },
        { {NOP,3}, {NOP,2}, {SL,3}, {NOP,3} },
        { {NOP,3}, {NOP,3}, {NOP,3},{NOP,3} } };
    static readonly int[] AboveStart = { 0, 1, 0, 0, 3 };
    static readonly int[,,] Below = {
        { {NOP,0}, {NOP,2}, {NOP,0}, {NOP,2} },
        { {NOP,1}, {RD,2},  {NOP,1}, {NOP,2} },
        { {NOP,2}, {SD,2},  {NOP,2}, {NOP,2} } };
    // RC (ญ ฐ) is treated like DC: no descenderless glyphs are available, so the vowel is lowered instead
    static readonly int[] BelowStart = { 0, 0, 2, 2, 2 };

    static char Map(char c, int action)
    {
        switch (action)
        {
            case SD:
                switch (c) { case '่': return ''; case '้': return ''; case '๊': return ''; case '๋': return ''; case '์': return '';
                             case 'ุ': return ''; case 'ู': return ''; case 'ฺ': return ''; }
                break;
            case SDL:
                switch (c) { case '่': return ''; case '้': return ''; case '๊': return ''; case '๋': return ''; case '์': return ''; }
                break;
            case SL:
                switch (c) { case '่': return ''; case '้': return ''; case '๊': return ''; case '๋': return ''; case '์': return '';
                             case 'ั': return ''; case 'ิ': return ''; case 'ี': return ''; case 'ึ': return ''; case 'ื': return '';
                             case '็': return ''; case 'ํ': return ''; }
                break;
            case RD:
                switch (c) { case 'ญ': return ''; case 'ฐ': return ''; }
                break;
        }
        return c;
    }

    public static string Shape(string s)
    {
        var a = s.ToCharArray();
        int above = 3, below = 2, baseIdx = -1;
        for (int i = 0; i < a.Length; i++)
        {
            char c = a[i];
            int mt = MarkType(c);
            if (mt == 3)
            {
                int ct = ConsType(c);
                above = AboveStart[ct]; below = BelowStart[ct]; baseIdx = ct == 4 ? -1 : i;
                // SARA AM after a tone mark: keep the tone high (nikhahit takes the low slot)
                continue;
            }
            int act = Above[above, mt, 0]; above = Above[above, mt, 1];
            if (act != NOP && !(c >= '่' && c <= '์' && i + 1 < a.Length && a[i + 1] == 'ำ')) a[i] = Map(c, act);
            act = Below[below, mt, 0]; below = Below[below, mt, 1];
            if (act == RD && baseIdx >= 0) a[baseIdx] = Map(a[baseIdx], RD);
            else if (act == SD) a[i] = Map(c, SD);
        }
        return new string(a);
    }
}
