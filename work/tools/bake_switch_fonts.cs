using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

class SwitchFontBaker
{
    static byte[] Tag(Package p, string name, string type, int size, byte[] value)
    {
        var ms = new MemoryStream(); var w = new BinaryWriter(ms);
        w.Write(p.Names.IndexOf(name)); w.Write(0); w.Write(p.Names.IndexOf(type)); w.Write(0); w.Write(size); w.Write(0);
        if (p.Version == 579) w.Write((short)0);
        w.Write(value);
        return ms.ToArray();
    }

    public static int Bake(string inPkg, string outPkg, string ttfPath, int fontExport, double customThaiPx = -1)
    {
        var p = Package.Load(inPkg);
        var ttf = new Ttf(ttfPath);
        var bf = BmFontData.Load(p, fontExport);

        double thaiPx = customThaiPx > 0 ? customThaiPx : bf.Chars[bf.Remap['H']].H - 2;

        var pagesPx = new List<uint[]>();
        var pagesW = new List<int>();
        foreach (int ti in bf.Textures)
        {
            var pt = BmTexture.Load(p, ti);
            if (pt.Format != "PF_DXT5") throw new Exception("texture format " + pt.Format);
            byte[] rawMip = new byte[pt.MipDataLen];
            Buffer.BlockCopy(p.Data, pt.MipDataPos, rawMip, 0, pt.MipDataLen);
            byte[] linearMip = Tegra.DeswizzleDxt5(rawMip, pt.SizeX, pt.SizeY);
            pagesPx.Add(Dxt5.Decode(linearMip, 0, pt.SizeX, pt.SizeY));
            pagesW.Add(pt.SizeX);
        }
        var tx = BmTexture.Load(p, bf.Textures[0]);

        var hc = bf.Chars[bf.Remap['H']];
        int baseline = hc.VOff + hc.H - 1;
        double maxH = 0;
        foreach (var ct in ttf.Contours(ttf.Cmap[0x0E01])) foreach (var q in ct) maxH = Math.Max(maxH, q.Y);
        double s = thaiPx / maxH;
        var plan = ThaiRaster.Plan(ttf);
        double left = 0;
        foreach (var pl in plan) left = Math.Max(left, ThaiRaster.LeftInk(ttf, ttf.Cmap[pl[1]], s, pl[2]));
        int pad = (int)Math.Ceiling(left) + 2;

        // existing glyphs
        var cells = new List<Cell>();
        foreach (var c in bf.Chars)
        {
            int cpad = pad;
            var cell = new Cell { W = c.W + cpad, H = Math.Max(1, c.H), VOff = c.VOff };
            cell.Px = new uint[cell.W * cell.H];
            for (int y = 0; y < c.H; y++) for (int x = 0; x < c.W; x++)
                cell.Px[y * cell.W + cpad + x] = pagesPx[c.Tex][(c.V + y) * pagesW[c.Tex] + c.U + x];
            cells.Add(cell);
        }

        var remap = new Dictionary<int, int>(bf.Remap);
        var order = new List<int>(bf.Remap.Keys);
        int added = 0;

        // CP874 slot mapping dictionary
        var slotMap874 = new Dictionary<int, int>();
        // PUA F700-F71A -> Free874 slots
        int[] free874 = { 0xF8C1, 0xF8C2, 0xF8C3, 0xF8C4, 0xF8C5, 0xF8C6, 0xF8C7, 0xF8C8,
                          0x0081, 0x0082, 0x0083, 0x0084, 0x0086, 0x0087, 0x0088, 0x0089,
                          0x008A, 0x008B, 0x008C, 0x008E, 0x0098, 0x0099, 0x009A, 0x009B,
                          0x009C, 0x009E, 0x009F };
        for (int i = 0; i < 27; i++) slotMap874[0xF700 + i] = free874[i];

        foreach (var pl in plan)
        {
            int code = pl[0];
            int srcCode = pl[1];
            if (!ttf.Cmap.ContainsKey(srcCode)) continue;

            int clipped;
            var cell = ThaiRaster.Render(ttf, ttf.Cmap[srcCode], s, pl[2], pl[3], pad, baseline, out clipped);
            cells.Add(cell);
            int newIdx = cells.Count - 1;
            added++;

            // 1. Map raw Unicode / PUA
            remap[code] = newIdx;
            if (!order.Contains(code)) order.Add(code);

            // 2. Map CP874 / CP1252 slot alias if exists
            int slotChar;
            if (slotMap874.TryGetValue(code, out slotChar))
            {
                remap[slotChar] = newIdx;
                if (!order.Contains(slotChar)) order.Add(slotChar);
            }
        }

        // Shelf packing
        int AW = 256;
        var byH = new List<Cell>(cells); byH.Sort((a, b) => b.H.CompareTo(a.H));
        int cx = 0, cy = 0, rowH = 0;
        foreach (var c in byH)
        {
            if (cx + c.W > AW) { cx = 0; cy += rowH + 1; rowH = 0; }
            c.X = cx; c.Y = cy; cx += c.W + 1; rowH = Math.Max(rowH, c.H);
        }
        int AH = 64; while (AH < cy + rowH) AH *= 2;
        if (AH > AW * 2) AW *= 2;
        if (AW != 256)
        {
            cx = 0; cy = 0; rowH = 0;
            foreach (var c in byH) { if (cx + c.W > AW) { cx = 0; cy += rowH + 1; rowH = 0; } c.X = cx; c.Y = cy; cx += c.W + 1; rowH = Math.Max(rowH, c.H); }
            AH = 64; while (AH < cy + rowH) AH *= 2;
        }
        var atlas = new uint[AW * AH];
        foreach (var c in cells) for (int y = 0; y < c.H; y++) for (int x = 0; x < c.W; x++) atlas[(c.Y + y) * AW + c.X + x] = c.Px[y * c.W + x];
        for (int i = 0; i < atlas.Length; i++) if (atlas[i] == 0) atlas[i] = 0x00FFFFFF;
        var linearDxt = Dxt5.Encode(atlas, AW, AH);
        var dxt = Tegra.SwizzleDxt5(linearDxt, AW, AH);

        // names
        var newNames = new List<string>();
        foreach (var n in new[] { "Kerning", "IntProperty", "IsRemapped" }) if (!p.Names.Contains(n)) { newNames.Add(n); p.Names.Add(n); }

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
        BitConverter.GetBytes(texOff + trel(tx.NativeStart) + 16).CopyTo(tb, trel(tx.NativeStart) + 12);
        int hdr = trel(tx.MipDataPos) - 16;
        BitConverter.GetBytes(dxt.Length).CopyTo(tb, hdr + 4);
        BitConverter.GetBytes(dxt.Length).CopyTo(tb, hdr + 8);
        BitConverter.GetBytes(texOff + hdr + 16).CopyTo(tb, hdr + 12);
        int tailStart = tx.MipDataPos + tx.MipDataLen, tailLen = tx.E.SerialOffset + tx.E.SerialSize - tailStart;
        var tail = new byte[tailLen]; Buffer.BlockCopy(d, tailStart, tail, 0, tailLen);
        BitConverter.GetBytes(AW).CopyTo(tail, 0); BitConverter.GetBytes(AH).CopyTo(tail, 4);
        outMs.Write(tb, 0, tb.Length); outMs.Write(dxt, 0, dxt.Length); outMs.Write(tail, 0, tail.Length);
        int texSize = (int)outMs.Length - texOff;

        // font export
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
            else if (n == "IsRemapped") { fe.Write(d, start, v - start); fw.Write(1); }
            else if (n == "Textures")
            {
                fe.Write(d, start, 16); fw.Write(8); fw.Write(0);
                if (p.Version == 579) fw.Write((short)0);
                fw.Write(1); fw.Write(bf.Textures[0]);
            }
            else fe.Write(d, start, v + sz - start);
        });
        if (!bf.HasKerning) { var kt = Tag(p, "Kerning", "IntProperty", 4, BitConverter.GetBytes(-pad)); fe.Write(kt, 0, kt.Length); }
        if (!bf.HasIsRemapped) { var rt = Tag(p, "IsRemapped", "IntProperty", 4, BitConverter.GetBytes(1)); fe.Write(rt, 0, rt.Length); }
        fe.Write(d, bf.PropsEnd - 8, 8);
        order.Sort();
        fw.Write(order.Count);
        foreach (int k in order) { fw.Write((ushort)k); fw.Write((ushort)remap[k]); }
        var fb = fe.ToArray();
        outMs.Write(fb, 0, fb.Length);

        // name table relocation
        int nameOff = p.NameOffset;
        if (newNames.Count > 0)
        {
            nameOff = (int)outMs.Length;
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
        File.WriteAllBytes(outPkg, all);
        Console.WriteLine(string.Format("Baked Switch font exp {0}: {1} cells, {2} remap, tex {3}x{4}, pad={5}", fontExport, cells.Count, order.Count, AW, AH, pad));
        return 0;
    }

    static int Main(string[] args)
    {
        if (args.Length < 4)
        {
            Console.WriteLine("Usage: SwitchFontBaker <inPkg> <outPkg> <ttf> <fontExp> [customThaiPx]");
            return 1;
        }
        double thaiPx = args.Length > 4 ? double.Parse(args[4], System.Globalization.CultureInfo.InvariantCulture) : -1;
        return Bake(args[0], args[1], args[2], int.Parse(args[3]), thaiPx);
    }
}
