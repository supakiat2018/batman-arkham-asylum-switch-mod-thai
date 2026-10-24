using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

class PortPcFont
{
    // PUA F7xx -> CP874 slot table
    static readonly Dictionary<int, int> PuaToSlot = new Dictionary<int, int>
    {
        { 0xF700, 0xF8C1 }, { 0xF701, 0xF8C2 }, { 0xF702, 0xF8C3 }, { 0xF703, 0xF8C4 },
        { 0xF704, 0xF8C5 }, { 0xF705, 0xF8C6 }, { 0xF706, 0xF8C7 }, { 0xF707, 0xF8C8 },
        { 0xF708, 0x0081 }, { 0xF709, 0x0082 }, { 0xF70A, 0x0083 }, { 0xF70B, 0x0084 },
        { 0xF70C, 0x0086 }, { 0xF70D, 0x0087 }, { 0xF70E, 0x0088 }, { 0xF70F, 0x0089 },
        { 0xF710, 0x008A }, { 0xF711, 0x008B }, { 0xF712, 0x008C }, { 0xF713, 0x008E },
        { 0xF714, 0x0098 }, { 0xF715, 0x0099 }, { 0xF716, 0x009A }, { 0xF717, 0x009B },
        { 0xF718, 0x009C }, { 0xF719, 0x009E }, { 0xF71A, 0x009F }
    };

    static byte[] Tag(Package p, string name, string type, int size, byte[] value)
    {
        var ms = new MemoryStream(); var w = new BinaryWriter(ms);
        w.Write(p.Names.IndexOf(name)); w.Write(0); w.Write(p.Names.IndexOf(type)); w.Write(0); w.Write(size); w.Write(0);
        if (p.Version == 579) w.Write((short)0);
        w.Write(value);
        return ms.ToArray();
    }

    public static void PortFont(string pcPkgPath, int pcFontExp, string swInPkgPath, string swOutPkgPath, int swFontExp)
    {
        // 1. Load PC font data and texture
        var pcPkg = Package.Load(pcPkgPath);
        var pcBf = BmFontData.Load(pcPkg, pcFontExp);
        var pcTx = BmTexture.Load(pcPkg, pcBf.Textures[0]);
        byte[] pcDxt = new byte[pcTx.MipDataLen];
        Buffer.BlockCopy(pcPkg.Data, pcTx.MipDataPos, pcDxt, 0, pcTx.MipDataLen);

        int AW = pcTx.SizeX;
        int AH = pcTx.SizeY;

        // Build combined Remap with PUA aliases
        var remap = new Dictionary<int, int>(pcBf.Remap);
        var order = new List<int>(pcBf.Remap.Keys);
        foreach (var kv in PuaToSlot)
        {
            int puaChar = kv.Key;
            int slotChar = kv.Value;
            int glyphIdx;
            if (remap.TryGetValue(slotChar, out glyphIdx))
            {
                remap[puaChar] = glyphIdx;
                if (!order.Contains(puaChar)) order.Add(puaChar);
            }
        }

        // 2. Load Switch package
        var p = Package.Load(swInPkgPath);
        var bf = BmFontData.Load(p, swFontExp);
        var tx = BmTexture.Load(p, bf.Textures[0]);

        // names
        var newNames = new List<string>();
        foreach (var n in new[] { "Kerning", "IntProperty" })
            if (!p.Names.Contains(n)) { newNames.Add(n); p.Names.Add(n); }

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
        if (tx.MipTailPos > 0)
        {
            int l2 = 0; while ((1 << l2) < Math.Max(AW, AH)) l2++;
            BitConverter.GetBytes(l2).CopyTo(tb, trel(tx.MipTailPos));
        }
        BitConverter.GetBytes(texOff + trel(tx.NativeStart) + 16).CopyTo(tb, trel(tx.NativeStart) + 12);
        int hdr = trel(tx.MipDataPos) - 16;
        BitConverter.GetBytes(pcDxt.Length).CopyTo(tb, hdr + 4);
        BitConverter.GetBytes(pcDxt.Length).CopyTo(tb, hdr + 8);
        BitConverter.GetBytes(texOff + hdr + 16).CopyTo(tb, hdr + 12);
        int tailStart = tx.MipDataPos + tx.MipDataLen, tailLen = tx.E.SerialOffset + tx.E.SerialSize - tailStart;
        var tail = new byte[tailLen]; Buffer.BlockCopy(d, tailStart, tail, 0, tailLen);
        BitConverter.GetBytes(AW).CopyTo(tail, 0); BitConverter.GetBytes(AH).CopyTo(tail, 4);
        outMs.Write(tb, 0, tb.Length); outMs.Write(pcDxt, 0, pcDxt.Length); outMs.Write(tail, 0, tail.Length);
        int texSize = (int)outMs.Length - texOff;

        // font export
        int fontOff = (int)outMs.Length;
        var fe = new MemoryStream(); var fw = new BinaryWriter(fe);
        fe.Write(d, bf.E.SerialOffset, 4);
        var chars = new MemoryStream(); var cw = new BinaryWriter(chars);
        cw.Write(pcBf.Chars.Count);
        foreach (var c in pcBf.Chars)
        {
            cw.Write(c.U); cw.Write(c.V); cw.Write(c.W); cw.Write(c.H); cw.Write((byte)0); cw.Write(c.VOff);
        }
        Subs.Walk(p, bf.E.SerialOffset + 4, bf.E.SerialOffset + bf.E.SerialSize, delegate(int t, string n, int sp, int v, int sz)
        {
            int start = sp - 16;
            if (n == "Characters")
            {
                fe.Write(d, start, 16); fw.Write((int)chars.Length); fw.Write(0);
                if (p.Version == 579) fw.Write((short)0);
                chars.WriteTo(fe);
            }
            else if (n == "Kerning") { fe.Write(d, start, v - start); fw.Write(pcBf.Kerning); }
            else if (n == "Textures")
            {
                fe.Write(d, start, 16); fw.Write(8); fw.Write(0);
                if (p.Version == 579) fw.Write((short)0);
                fw.Write(1); fw.Write(bf.Textures[0]);
            }
            else fe.Write(d, start, v + sz - start);
        });
        if (!bf.HasKerning)
        {
            var kt = Tag(p, "Kerning", "IntProperty", 4, BitConverter.GetBytes(pcBf.Kerning));
            fe.Write(kt, 0, kt.Length);
        }
        fe.Write(d, bf.PropsEnd - 8, 8); // None
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
            for (int i = 0; i < p.NameCount; i++)
            {
                int len = nr.ReadInt32(); nr.BaseStream.Position += len >= 0 ? len : -len * 2;
                ulong fl = nr.ReadUInt64(); if (i == 0) nameFlags = fl;
            }
            outMs.Write(d, p.NameOffset, (int)nr.BaseStream.Position - p.NameOffset);
            var nw = new BinaryWriter(outMs);
            foreach (var n in newNames)
            {
                var b = Encoding.ASCII.GetBytes(n + "\0");
                nw.Write(b.Length); nw.Write(b); nw.Write(nameFlags);
            }
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
        File.WriteAllBytes(swOutPkgPath, all);
        Console.WriteLine(string.Format("Ported PC exp {0} -> SW exp {1}: {2} chars, {3} remap, tex {4}x{5}", pcFontExp, swFontExp, pcBf.Chars.Count, order.Count, AW, AH));
    }

    static int Main(string[] args)
    {
        if (args.Length < 5)
        {
            Console.WriteLine("Usage: PortPcFont <pcPkg> <pcFontExp> <swInPkg> <swOutPkg> <swFontExp>");
            return 1;
        }
        PortFont(args[0], int.Parse(args[1]), args[2], args[3], int.Parse(args[4]));
        return 0;
    }
}
