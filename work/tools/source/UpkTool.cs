// UpkTool - Batman: Arkham Origins (UE3 v807 / licensee 138) package helper.
// Build: csc /nologo /optimize /out:UpkTool.exe UpkTool.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;

static class Lzo
{
    // Port of lzo1x_decompress_safe.
    public static int Decompress(byte[] src, int ip, int srcLen, byte[] dst, int op)
    {
        int ipEnd = ip + srcLen;
        int opStart = op;
        int t, m;

        t = src[ip];
        if (t > 17)
        {
            ip++;
            t -= 17;
            if (t < 4) goto match_next;
            while (t-- > 0) dst[op++] = src[ip++];
            goto first_literal_run;
        }

    loop:
        t = src[ip++];
        if (t >= 16) goto match;
        if (t == 0)
        {
            while (src[ip] == 0) { t += 255; ip++; }
            t += 15 + src[ip++];
        }
        t += 3;
        while (t-- > 0) dst[op++] = src[ip++];

    first_literal_run:
        t = src[ip++];
        if (t >= 16) goto match;
        m = op - (1 + 0x0800);
        m -= t >> 2;
        m -= src[ip++] << 2;
        dst[op++] = dst[m++]; dst[op++] = dst[m++]; dst[op++] = dst[m];
        goto match_done;

    match:
        if (t >= 64)
        {
            m = op - 1;
            m -= (t >> 2) & 7;
            m -= src[ip++] << 3;
            t = (t >> 5) - 1;
            goto copy_match;
        }
        else if (t >= 32)
        {
            t &= 31;
            if (t == 0)
            {
                while (src[ip] == 0) { t += 255; ip++; }
                t += 31 + src[ip++];
            }
            m = op - 1;
            m -= (src[ip] >> 2) + (src[ip + 1] << 6);
            ip += 2;
        }
        else if (t >= 16)
        {
            m = op;
            m -= (t & 8) << 11;
            t &= 7;
            if (t == 0)
            {
                while (src[ip] == 0) { t += 255; ip++; }
                t += 7 + src[ip++];
            }
            m -= (src[ip] >> 2) + (src[ip + 1] << 6);
            ip += 2;
            if (m == op) goto eof_found;
            m -= 0x4000;
        }
        else
        {
            m = op - 1;
            m -= t >> 2;
            m -= src[ip++] << 2;
            dst[op++] = dst[m++]; dst[op++] = dst[m];
            goto match_done;
        }

    copy_match:
        t += 2;
        while (t-- > 0) dst[op++] = dst[m++];

    match_done:
        t = src[ip - 2] & 3;
        if (t == 0) goto loop;

    match_next:
        while (t-- > 0) dst[op++] = src[ip++];
        t = src[ip++];
        goto match;

    eof_found:
        if (ip != ipEnd) throw new Exception("LZO: input not fully consumed");
        return op - opStart;
    }
}

class Chunk { public int UOff, USize, COff, CSize; }
class Imp { public string ClassPackage, ClassName, Name; public int Outer; }
class Exp { public int Class, Super, Outer, Archetype, SerialSize, SerialOffset; public string Name; public ulong Flags; public long TableOffset; public int SizeFieldPos; }

class Package
{
    public byte[] Data;          // fully decompressed
    public int Version, Licensee, HeaderSize, NameCount, NameOffset, ExportCount, ExportOffset, ImportCount, ImportOffset;
    public uint PackageFlags;
    public List<string> Names = new List<string>();
    public List<Imp> Imports = new List<Imp>();
    public List<Exp> Exports = new List<Exp>();
    public int CompressionFlagsPos;  // offset of compression flags in the decompressed header
    public int NameCountPos, GenerationsPos, GenerationCount;

    [DllImport("oo2core_9_win64.dll", CallingConvention = CallingConvention.Cdecl)]
    static extern long OodleLZ_Decompress(byte[] compBuf, long compLen, byte[] rawBuf, long rawLen, int fuzzSafe, int checkCRC, int verbosity, IntPtr decBufBase, long decBufSize, IntPtr fpCallback, IntPtr callbackUserData, IntPtr decoderMemory, long decoderMemorySize, int threadPhase);

    public static Package Load(string path)
    {
        byte[] raw = File.ReadAllBytes(path);
        var p = new Package();
        var r = new BinaryReader(new MemoryStream(raw));
        if (r.ReadUInt32() != 0x9E2A83C1) throw new Exception("not a UE3 package");
        p.Version = r.ReadUInt16(); p.Licensee = r.ReadUInt16();
        p.HeaderSize = r.ReadInt32();
        ReadFString(r);
        long flagsPos = r.BaseStream.Position;
        p.NameCountPos = (int)flagsPos + 4;
        p.PackageFlags = r.ReadUInt32();
        p.NameCount = r.ReadInt32(); p.NameOffset = r.ReadInt32();
        p.ExportCount = r.ReadInt32(); p.ExportOffset = r.ReadInt32();
        p.ImportCount = r.ReadInt32(); p.ImportOffset = r.ReadInt32();
        r.ReadInt32(); // depends
        if (p.Version >= 584) r.ReadInt32();                                    // thumbnail table
        if (p.Version >= 623) { r.ReadInt32(); r.ReadInt32(); r.ReadInt32(); }  // import/export guids
        r.ReadBytes(16); // guid
        int gens = r.ReadInt32();
        p.GenerationsPos = (int)r.BaseStream.Position; p.GenerationCount = gens;
        r.ReadBytes(gens * 12);
        r.ReadInt32(); r.ReadInt32(); // engine, cooker
        p.CompressionFlagsPos = (int)r.BaseStream.Position;
        int compFlags = r.ReadInt32();
        int chunkCount = r.ReadInt32();
        var chunks = new List<Chunk>();
        for (int i = 0; i < chunkCount; i++)
            chunks.Add(new Chunk { UOff = r.ReadInt32(), USize = r.ReadInt32(), COff = r.ReadInt32(), CSize = r.ReadInt32() });
        int tailStart = (int)r.BaseStream.Position;

        if (chunkCount == 0) { p.Data = raw; }
        else
        {
            if (compFlags != 2 && compFlags != 8) throw new Exception("unsupported compression " + compFlags);
            int total = 0;
            foreach (var c in chunks) total = Math.Max(total, c.UOff + c.USize);
            byte[] d = new byte[total];
            // header: everything up to the chunk table, then count=0, then the tail up to first chunk data
            int firstC = int.MaxValue;
            foreach (var c in chunks) firstC = Math.Min(firstC, c.COff);
            int hp = 0;
            Buffer.BlockCopy(raw, 0, d, 0, p.CompressionFlagsPos); hp = p.CompressionFlagsPos;
            BitConverter.GetBytes(0).CopyTo(d, hp); hp += 4;     // compression flags
            BitConverter.GetBytes(0).CopyTo(d, hp); hp += 4;     // chunk count
            Buffer.BlockCopy(raw, tailStart, d, hp, firstC - tailStart); hp += firstC - tailStart;
            if (hp != chunks[0].UOff) Console.Error.WriteLine("warning: header length " + hp + " != first chunk uoff " + chunks[0].UOff);
            // clear the "store compressed" flag
            uint pf = BitConverter.ToUInt32(d, (int)flagsPos) & ~0x02000000u;
            BitConverter.GetBytes(pf).CopyTo(d, (int)flagsPos);
            foreach (var c in chunks) DecompressChunk(raw, c, d, compFlags);
            p.Data = d;
        }
        try { p.Parse(); } catch (Exception ex) { Console.Error.WriteLine("parse error: " + ex.Message); }
        return p;
    }

    static void DecompressChunk(byte[] raw, Chunk c, byte[] d, int compFlags)
    {
        int pos = c.COff;
        uint tag = BitConverter.ToUInt32(raw, pos);
        if (tag != 0x9E2A83C1) throw new Exception("bad chunk tag");
        int blockSize = BitConverter.ToInt32(raw, pos + 4);
        int usize = BitConverter.ToInt32(raw, pos + 12);
        int n = (usize + blockSize - 1) / blockSize;
        int bp = pos + 16;
        int dataPos = bp + n * 8;
        int outPos = c.UOff;
        for (int i = 0; i < n; i++)
        {
            int cs = BitConverter.ToInt32(raw, bp + i * 8);
            int us = BitConverter.ToInt32(raw, bp + i * 8 + 4);
            if (compFlags == 8)
            {
                byte[] compBlock = new byte[cs];
                Buffer.BlockCopy(raw, dataPos, compBlock, 0, cs);
                byte[] rawBlock = new byte[us];
                long ret = OodleLZ_Decompress(compBlock, cs, rawBlock, us, 0, 0, 0, IntPtr.Zero, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0, 3);
                if (ret != us) throw new Exception("oodle decompress failed: " + ret + " != " + us);
                Buffer.BlockCopy(rawBlock, 0, d, outPos, us);
            }
            else
            {
                int got = Lzo.Decompress(raw, dataPos, cs, d, outPos);
                if (got != us) throw new Exception("block size mismatch");
            }
            dataPos += cs; outPos += us;
        }
    }

    public static string ReadFString(BinaryReader r)
    {
        int len = r.ReadInt32();
        if (len == 0) return "";
        if (len > 0) { var b = r.ReadBytes(len); return Encoding.GetEncoding(1252).GetString(b, 0, len - 1); }
        var u = r.ReadBytes(-len * 2); return Encoding.Unicode.GetString(u, 0, (-len - 1) * 2);
    }

    void Parse()
    {
        var r = new BinaryReader(new MemoryStream(Data));
        r.BaseStream.Position = NameOffset;
        for (int i = 0; i < NameCount; i++) { Names.Add(ReadFString(r)); r.ReadUInt64(); }
        r.BaseStream.Position = ImportOffset;
        for (int i = 0; i < ImportCount; i++)
        {
            var im = new Imp();
            im.ClassPackage = Name(r); im.ClassName = Name(r); im.Outer = r.ReadInt32(); im.Name = Name(r);
            Imports.Add(im);
        }
        r.BaseStream.Position = ExportOffset;
        for (int i = 0; i < ExportCount; i++)
        {
            var e = new Exp();
            e.TableOffset = r.BaseStream.Position;
            e.Class = r.ReadInt32(); e.Super = r.ReadInt32(); e.Outer = r.ReadInt32(); e.Name = Name(r);
            e.Archetype = r.ReadInt32();
            if (Licensee == 138) r.ReadInt32(); // Arkham Origins extra field
            e.Flags = r.ReadUInt64();
            e.SizeFieldPos = (int)r.BaseStream.Position;
            e.SerialSize = r.ReadInt32(); e.SerialOffset = r.ReadInt32();
            r.ReadUInt32(); // export flags
            int net = r.ReadInt32(); r.ReadBytes(net * 4);
            r.ReadBytes(16); r.ReadUInt32();
            Exports.Add(e);
        }
    }

    string Name(BinaryReader r)
    {
        int idx = r.ReadInt32(); int num = r.ReadInt32();
        string s = (idx >= 0 && idx < Names.Count) ? Names[idx] : ("<bad " + idx + ">");
        return num > 0 ? s + "_" + (num - 1) : s;
    }

    public string ObjName(int index)
    {
        if (index > 0) return Exports[index - 1].Name;
        if (index < 0) return Imports[-index - 1].Name;
        return "Class";
    }

    public string FullName(int index)
    {
        var parts = new List<string>();
        int guard = 0;
        while (index != 0 && guard++ < 64)
        {
            parts.Insert(0, ObjName(index));
            index = index > 0 ? Exports[index - 1].Outer : Imports[-index - 1].Outer;
        }
        return string.Join(".", parts.ToArray());
    }
}

// One subtitle Text property inside an RDialogueEvent's LocalizedSubtitles[LANG_x].Subtitles[i]
class SubText
{
    public string Lang;
    public int Index;          // line index within Subtitles array
    public string Text;
    public int TextTagSizePos; // absolute offsets of the size fields that enclose this text
    public int SubsTagSizePos;
    public int LocTagSizePos;
    public int ValuePos, ValueLen; // FString (len + chars) absolute range
}

static class Subs
{
    // Walk a tagged property list. Callback gets (type, name, sizePos, valuePos, size). Returns end (after None) or -1.
    public delegate void PropCb(int type, string name, int sizePos, int valuePos, int size);

    // Standard UE3 v576 tag: FName name, FName type, int32 size, int32 arrayIndex, [FName struct], value
    // (bool: size 0, value is a trailing int32). Type names map to the codes the rest of the tool uses.
    public static int TypeCode(string t)
    {
        switch (t)
        {
            case "ByteProperty": return 1;
            case "IntProperty": return 2;
            case "BoolProperty": return 3;
            case "FloatProperty": return 4;
            case "ObjectProperty": case "ClassProperty": case "ComponentProperty": case "InterfaceProperty": return 5;
            case "NameProperty": return 6;
            case "DelegateProperty": return 7;
            case "ArrayProperty": return 9;
            case "StructProperty": return 10;
            case "StrProperty": return 13;
            default: return 99;
        }
    }

    public static int Walk(Package p, int o, int end, PropCb cb)
    {
        var d = p.Data;
        int tagHdr = p.Version == 579 ? 26 : 24;
        while (o + 8 <= end)
        {
            string name = Program.NameAt(p, d, o);
            if (name == "None") return o + 8;
            if (o + tagHdr > end) return -1;
            int type = TypeCode(Program.NameAt(p, d, o + 8));
            if (type == 99) return -1;
            int size = BitConverter.ToInt32(d, o + 16);
            int v = o + tagHdr;
            if (type == 10) v += 8;
            int real = type == 3 ? 4 : size;
            if (real < 0 || v + real > end) return -1;
            cb(type, name, o + 16, v, real);
            o = v + real;
        }
        return -1;
    }

    // true when an array value looks like a list of tagged structs
    public static bool IsStructArray(Package p, int v, int size)
    {
        if (size < 4 + 8) return false;
        int n = BitConverter.ToInt32(p.Data, v);
        if (n <= 0 || n > 100000) return false;
        int i0 = BitConverter.ToInt32(p.Data, v + 4), i1 = BitConverter.ToInt32(p.Data, v + 8);
        if (i0 < 0 || i0 >= p.Names.Count || i1 != 0) return false;
        if (p.Names[i0] == "None") return true;
        if (size < 4 + 24) return false;
        int t = BitConverter.ToInt32(p.Data, v + 12);
        return t >= 0 && t < p.Names.Count && p.Names[t].EndsWith("Property");
    }

    public static string ReadStr(byte[] d, int v)
    {
        int sl = BitConverter.ToInt32(d, v);
        if (sl > 0) return Encoding.GetEncoding(1252).GetString(d, v + 4, sl - 1);
        if (sl < 0) return Encoding.Unicode.GetString(d, v + 4, (-sl - 1) * 2);
        return "";
    }

    public static List<SubText> Find(Package p, Exp e)
    {
        var res = new List<SubText>();
        int start = e.SerialOffset + 4, end = e.SerialOffset + e.SerialSize;
        Walk(p, start, end, delegate(int type, string name, int sizePos, int v, int size)
        {
            if (type != 9 || name != "LocalizedSubtitles") return;
            int n = BitConverter.ToInt32(p.Data, v);
            int q = v + 4;
            for (int k = 0; k < n && q > 0; k++)
            {
                string lang = null;
                var texts = new List<SubText>();
                q = Walk(p, q, v + size, delegate(int t2, string n2, int sp2, int v2, int s2)
                {
                    if (t2 == 1 && n2 == "Language") lang = Program.NameAt(p, p.Data, v2);
                    if (t2 == 9 && n2 == "Subtitles")
                    {
                        int m = BitConverter.ToInt32(p.Data, v2);
                        int r = v2 + 4;
                        for (int j = 0; j < m && r > 0; j++)
                        {
                            int jj = j;
                            r = Walk(p, r, v2 + s2, delegate(int t3, string n3, int sp3, int v3, int s3)
                            {
                                if (t3 == 13 && n3 == "Text")
                                    texts.Add(new SubText { Index = jj, Text = ReadStr(p.Data, v3), TextTagSizePos = sp3, SubsTagSizePos = sp2, LocTagSizePos = sizePos, ValuePos = v3, ValueLen = s3 });
                            });
                        }
                    }
                });
                foreach (var t in texts) { t.Lang = lang; res.Add(t); }
            }
        });
        return res;
    }

    // Every StrProperty inside an export's tagged properties (recursing into struct arrays and structs),
    // with the size fields that enclose it so they can be patched when the text length changes.
    public static List<StrRef> Strings(Package p, Exp e)
    {
        var res = new List<StrRef>();
        Collect(p, e.SerialOffset + 4, e.SerialOffset + e.SerialSize, "", new List<int>(), res);
        return res;
    }

    static int Collect(Package p, int o, int end, string path, List<int> sizes, List<StrRef> res)
    {
        return Walk(p, o, end, delegate(int type, string name, int sp, int v, int size)
        {
            var inner = new List<int>(sizes); inner.Add(sp);
            if (type == 13)
                res.Add(new StrRef { Path = path + name, Text = ReadStr(p.Data, v), SizePos = inner, ValuePos = v, ValueLen = size });
            else if (type == 9 && IsStructArray(p, v, size))
            {
                int n = BitConverter.ToInt32(p.Data, v), q = v + 4;
                for (int k = 0; k < n && q > 0; k++) q = Collect(p, q, v + size, path + name + "[" + k + "].", inner, res);
            }
            else if (type == 10 && size >= 8)
            {
                int ni = BitConverter.ToInt32(p.Data, v);
                if (ni >= 0 && ni < p.Names.Count && BitConverter.ToInt32(p.Data, v + 4) == 0)
                {
                    var tmp = new List<StrRef>();
                    if (Collect(p, v, v + size, path + name + ".", inner, tmp) == v + size) res.AddRange(tmp);
                }
            }
        });
    }

    public static string Escape(string s) { return s.Replace("\\", "\\\\").Replace("\t", "\\t").Replace("\r", "\\r").Replace("\n", "\\n"); }
    public static string Unescape(string s)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < s.Length; i++)
        {
            if (s[i] == '\\' && i + 1 < s.Length)
            {
                char c = s[++i];
                sb.Append(c == 't' ? '\t' : c == 'r' ? '\r' : c == 'n' ? '\n' : c);
            }
            else sb.Append(s[i]);
        }
        return sb.ToString();
    }
}

class StrRef { public string Path, Text; public List<int> SizePos; public int ValuePos, ValueLen; }

class SwfTag { public int Code; public int Offset; public int HeaderLen; public int Length; }

static class Swf
{
    // Unpack CFX/CWS (zlib) or GFX/FWS movie -> uncompressed bytes with an F/G signature
    public static byte[] Unpack(byte[] raw)
    {
        char s0 = (char)raw[0];
        if (s0 == 'F' || s0 == 'G') return raw;
        int ulen = BitConverter.ToInt32(raw, 4);
        var res = new byte[ulen];
        Buffer.BlockCopy(raw, 0, res, 0, 8);
        res[0] = (byte)(s0 == 'C' && raw[1] == 'F' ? 'G' : 'F');
        using (var ds = new System.IO.Compression.DeflateStream(new MemoryStream(raw, 10, raw.Length - 10), System.IO.Compression.CompressionMode.Decompress))
        {
            int pos = 8;
            while (pos < ulen) { int n = ds.Read(res, pos, ulen - pos); if (n <= 0) break; pos += n; }
        }
        return res;
    }

    public static int HeaderEnd(byte[] d)
    {
        int nbits = d[8] >> 3;
        int rectBytes = (5 + nbits * 4 + 7) / 8;
        return 8 + rectBytes + 4; // rect + frame rate + frame count
    }

    public static List<SwfTag> Tags(byte[] d)
    {
        var list = new List<SwfTag>();
        int o = HeaderEnd(d);
        while (o + 2 <= d.Length)
        {
            int cl = BitConverter.ToUInt16(d, o);
            int code = cl >> 6, len = cl & 0x3F, hl = 2;
            if (len == 0x3F) { len = BitConverter.ToInt32(d, o + 2); hl = 6; }
            list.Add(new SwfTag { Code = code, Offset = o, HeaderLen = hl, Length = len });
            o += hl + len;
            if (code == 0) break;
        }
        return list;
    }
}

static class Program
{
    public static string NameAt(Package p, byte[] d, int o)
    {
        int idx = BitConverter.ToInt32(d, o), num = BitConverter.ToInt32(d, o + 4);
        string s = (idx >= 0 && idx < p.Names.Count) ? p.Names[idx] : ("<" + idx + ">");
        return num > 0 ? s + "_" + (num - 1) : s;
    }

    public static string Hex(byte[] d, int o, int n)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < n && o + i < d.Length; i++) sb.Append(d[o + i].ToString("X2")).Append(' ');
        return sb.ToString();
    }

    // returns position after the None terminator, or -1 on failure
    public static int DumpProps(Package p, int o, int end, string ind)
    {
        var d = p.Data;
        int tagHdr = p.Version == 579 ? 26 : 24;
        while (o + 8 <= end)
        {
            string name = NameAt(p, d, o);
            if (name == "None") { Console.WriteLine(ind + "None @" + o + (o + 8 < end ? " (+" + (end - o - 8) + " bytes native: " + Hex(d, o + 8, Math.Min(48, end - o - 8)) + ")" : "")); return o + 8; }
            if (o + tagHdr > end) { Console.WriteLine(ind + "?? truncated"); return -1; }
            string tname = NameAt(p, d, o + 8);
            int type = Subs.TypeCode(tname);
            int size = BitConverter.ToInt32(d, o + 16);
            int arr = BitConverter.ToInt32(d, o + 20);
            int v = o + tagHdr;
            if (type == 10) { tname += " " + NameAt(p, d, v); v += 8; }
            if (type == 3) size = 4; // BoolProperty: size field 0, value is a trailing int32
            if (type == 99 || size < 0 || v + size > end) { Console.WriteLine(ind + "?? bad tag " + tname + " " + name + " size " + size); return -1; }
            string val;
            if (size == 4 && (type == 2)) val = BitConverter.ToInt32(d, v).ToString();
            else if (size == 4 && type == 5) val = "obj " + BitConverter.ToInt32(d, v) + " " + p.FullName(BitConverter.ToInt32(d, v));
            else if (size == 8 && (type == 1 || type == 6)) val = NameAt(p, d, v);
            else if (size == 4 && type == 4) val = BitConverter.ToSingle(d, v).ToString();
            else
            {
                // try FString
                int sl = size >= 4 ? BitConverter.ToInt32(d, v) : 0;
                if (sl > 0 && sl + 4 == size) val = "\"" + Encoding.GetEncoding(1252).GetString(d, v + 4, sl - 1) + "\"";
                else if (sl < 0 && -sl * 2 + 4 == size) val = "U\"" + Encoding.Unicode.GetString(d, v + 4, (-sl - 1) * 2) + "\"";
                else val = Hex(d, v, Math.Min(size, 64)) + (size > 64 ? "..." : "");
            }
            Console.WriteLine(ind + tname + " " + name + (arr != 0 ? "[" + arr + "]" : "") + " (" + size + ") = " + val);
            if (type == 9 && Subs.IsStructArray(p, v, size))
            {
                int n = BitConverter.ToInt32(d, v);
                int q = v + 4;
                for (int k = 0; k < n && q > 0; k++) { Console.WriteLine(ind + "  [" + k + "]"); q = DumpProps(p, q, v + size, ind + "    "); }
            }
            o = v + size;
        }
        return -1;
    }

    // The game round-trips strings through the ANSI code page before Scaleform draws them (see Cp1252Thai).
    public static string Enc(string s) { return Cp1252Thai.Encode(s); }

    static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        if (args.Length < 2) { Console.WriteLine("UpkTool decompress|info|exports|strings <pkg> [out|filter] | scan <dir> <regex>"); return 1; }
        if (args[0].StartsWith("usm")) return UsmTool.Run(args);
        if (args[0] == "bmbuild") return BmBuild.Run(args);
        if (args[0] == "bmrender") return BmBuild.Render(args);
        if (args[0] == "hashtsv")
        {
            // hashtsv <in.tsv english\tthai> <out.tsv> <exact|loose> : replace English keys by hashes for release
            // (rows starting with '[' or '#' are kept; rows whose Thai equals the English are dropped)
            bool loose = args[3] == "loose";
            var outl = new List<string>();
            foreach (var line in File.ReadAllLines(args[1], Encoding.UTF8))
            {
                int tab = line.IndexOf('\t'); if (tab <= 0) continue;
                string k = line.Substring(0, tab), v = line.Substring(tab + 1);
                if (k[0] == '[' || k[0] == '#') { outl.Add(line); continue; }
                if (string.Equals(k.Trim(), v.Trim(), loose ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)) continue;
                outl.Add((loose ? TrTable.KeyLoose(k) : TrTable.KeyExact(k)) + "\t" + v);
            }
            File.WriteAllLines(args[2], outl.ToArray(), new UTF8Encoding(false));
            Console.WriteLine("hashed " + outl.Count + " rows");
            return 0;
        }
        if (args[0] == "intapply")
        {
            // intapply <src.int> <dst.int> <tsv english\tthai> [maxRun] : replace Key=Value values (UTF-16 LE + BOM kept)
            // case-insensitive: the worklist was deduped ignoring case, so "UNKNOWN" and "Unknown" share one row
            var tr = new TrTable(args[3], true);
            int maxRun = args.Length > 4 ? int.Parse(args[4]) : 24;
            string text = File.ReadAllText(args[1], Encoding.Unicode);
            var parts = System.Text.RegularExpressions.Regex.Split(text, "(\r?\n)");
            int hit = 0;
            string sec = "";
            for (int i = 0; i < parts.Length; i += 2)
            {
                string l = parts[i];
                if (l.Length > 0 && l[0] == '[') { sec = l.Trim().Trim('[', ']'); continue; }
                if (l.Length == 0 || l.TrimStart().StartsWith(";")) continue;
                int eq = l.IndexOf('='); if (eq < 1) continue;
                string v = l.Substring(eq + 1), th;
                // "[Section]Key" rows override by position, for values that must differ from same-text entries elsewhere
                if (!tr.TryGet("[" + sec + "]" + l.Substring(0, eq), out th) && !tr.TryText(v, out th)) continue;
                if (string.Equals(th, v.Trim(), StringComparison.OrdinalIgnoreCase)) continue;
                // wrap each \n-separated segment separately, then shape for the PUA glyphs
                var segs = th.Split(new[] { "\\n" }, StringSplitOptions.None);
                for (int s = 0; s < segs.Length; s++) segs[s] = ThaiShaper.Shape(ThaiWrap.Apply(segs[s], maxRun));
                parts[i] = l.Substring(0, eq + 1) + Enc(string.Join("\\n", segs));
                hit++;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[2])));
            File.WriteAllText(args[2], string.Concat(parts), new UnicodeEncoding(false, true));
            Console.WriteLine(Path.GetFileName(args[1]) + "\treplaced " + hit);
            return 0;
        }
        if (args[0] == "survey")
        {
            // survey <dir> <out.tsv> : every StrProperty in every package (recursive) -> file \t class \t object \t path \t text
            int nf = 0;
            using (var w = new StreamWriter(args[2], false, new UTF8Encoding(false)))
                foreach (var f in Directory.GetFiles(args[1], args.Length > 3 ? args[3] : "*.upk", SearchOption.AllDirectories))
                {
                    Package q;
                    try { q = Package.Load(f); } catch (Exception ex) { Console.Error.WriteLine(Path.GetFileName(f) + ": " + ex.Message); continue; }
                    if (q.Exports.Count == 0) { Console.Error.WriteLine(Path.GetFileName(f) + ": no exports"); continue; }
                    string rel = f.Substring(args[1].TrimEnd('\\').Length + 1);
                    for (int i = 0; i < q.Exports.Count; i++)
                    {
                        var e = q.Exports[i];
                        if (e.SerialSize < 12) continue;
                        List<StrRef> st;
                        try { st = Subs.Strings(q, e); } catch { continue; }
                        foreach (var s in st)
                            if (s.Text.Length > 0) w.WriteLine(rel + "\t" + q.ObjName(e.Class) + "\t" + q.FullName(i + 1) + "\t" + s.Path + "\t" + Subs.Escape(s.Text));
                    }
                    if (++nf % 100 == 0) Console.Error.WriteLine(nf + " packages");
                }
            Console.WriteLine("scanned " + nf + " packages");
            return 0;
        }
        if (args[0] == "inject")
        {
            // inject <in.upk> <out.upk> <translations.tsv> [wrap] : rows are "object|path<TAB>text".
            // A row for "X|Subtitles[k].Text" also covers the copy at "X|LocalizedSubtitles[n].Subtitles[k].Text".
            // Modified exports are appended at the end of an uncompressed copy, so no other offsets move;
            // bulk data headers inside a moved export (absolute offset of their payload) are re-pointed.
            var tr = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var line in File.ReadAllLines(args[3], Encoding.UTF8))
            {
                int tab = line.IndexOf('\t');
                if (tab <= 0) continue;
                string val = Subs.Unescape(line.Substring(tab + 1));
                if (val.Length > 0) tr[line.Substring(0, tab)] = val;
            }
            int wrapAt = args.Length > 4 ? int.Parse(args[4]) : 40;
            var locRx = new System.Text.RegularExpressions.Regex(@"^LocalizedSubtitles\[\d+\]\.");
            var q = Package.Load(args[1]);
            var outData = new MemoryStream();
            outData.Write(q.Data, 0, q.Data.Length);
            int changedExports = 0, changedLines = 0, bulkFixed = 0;
            for (int i = 0; i < q.Exports.Count; i++)
            {
                var e = q.Exports[i];
                if (e.SerialSize < 12) continue;
                List<StrRef> st;
                try { st = Subs.Strings(q, e); } catch { continue; }
                string full = q.FullName(i + 1);
                var mine = new List<StrRef>(); var texts = new Dictionary<StrRef, string>();
                foreach (var s in st)
                {
                    string th;
                    if (tr.TryGetValue(full + "|" + s.Path, out th) || tr.TryGetValue(full + "|" + locRx.Replace(s.Path, ""), out th))
                    { mine.Add(s); texts[s] = th; }
                }
                if (mine.Count == 0) continue;
                mine.Sort(delegate(StrRef a, StrRef b) { return b.ValuePos.CompareTo(a.ValuePos); });
                byte[] ex = new byte[e.SerialSize];
                Buffer.BlockCopy(q.Data, e.SerialOffset, ex, 0, e.SerialSize);
                int total = 0, lastEnd = 0;
                foreach (var s in mine)
                {
                    string text = Enc(ThaiShaper.Shape(ThaiWrap.Apply(texts[s], wrapAt)));
                    byte[] chars = Encoding.Unicode.GetBytes(text + "\0");
                    byte[] fs = new byte[4 + chars.Length];
                    BitConverter.GetBytes(-(text.Length + 1)).CopyTo(fs, 0);
                    chars.CopyTo(fs, 4);
                    int rel = s.ValuePos - e.SerialOffset;
                    lastEnd = Math.Max(lastEnd, rel + s.ValueLen);
                    int delta = fs.Length - s.ValueLen;
                    byte[] nx = new byte[ex.Length + delta];
                    Buffer.BlockCopy(ex, 0, nx, 0, rel);
                    Buffer.BlockCopy(fs, 0, nx, rel, fs.Length);
                    Buffer.BlockCopy(ex, rel + s.ValueLen, nx, rel + fs.Length, ex.Length - rel - s.ValueLen);
                    foreach (int sp in s.SizePos)
                    {
                        int r = sp - e.SerialOffset;
                        BitConverter.GetBytes(BitConverter.ToInt32(nx, r) + delta).CopyTo(nx, r);
                    }
                    ex = nx;
                    total += delta;
                    changedLines++;
                }
                int newOffset = (int)outData.Length;
                // bulk data: flags, count, sizeOnDisk, offsetInFile (= absolute position of the payload right after it)
                for (int r = lastEnd + total + 12; r + 4 <= ex.Length; r++)
                {
                    int stored = BitConverter.ToInt32(ex, r);
                    int oldRel = r + 4 - total;
                    if (stored != e.SerialOffset + oldRel) continue;
                    int sz = BitConverter.ToInt32(ex, r - 4);
                    if (sz < 0 || r + 4 + sz > ex.Length) continue;
                    BitConverter.GetBytes(newOffset + r + 4).CopyTo(ex, r);
                    bulkFixed++;
                    r += 3 + sz;
                }
                outData.Write(ex, 0, ex.Length);
                byte[] all = outData.GetBuffer();
                BitConverter.GetBytes(ex.Length).CopyTo(all, e.SizeFieldPos);
                BitConverter.GetBytes(newOffset).CopyTo(all, e.SizeFieldPos + 4);
                changedExports++;
            }
            if (changedExports == 0) { Console.WriteLine("no changes"); return 2; }
            byte[] result = new byte[outData.Length];
            Buffer.BlockCopy(outData.GetBuffer(), 0, result, 0, result.Length);
            File.WriteAllBytes(args[2], result);
            Console.WriteLine("exports " + changedExports + ", lines " + changedLines + ", bulk " + bulkFixed + ", size " + result.Length);
            return 0;
        }
        if (args[0] == "wrap")
        {
            // wrap <tsv> [max] : preview automatic Thai line-break spaces
            int mx = args.Length > 2 ? int.Parse(args[2]) : 40;
            foreach (var line in File.ReadAllLines(args[1], Encoding.UTF8))
            {
                int tab = line.IndexOf('\t'); if (tab < 0) continue;
                string a = line.Substring(tab + 1), b = ThaiWrap.Apply(a, mx);
                if (a != b) Console.WriteLine(line.Substring(0, tab) + "\t" + b);
            }
            return 0;
        }
        if (args[0] == "ttf")
        {
            // ttf <font.ttf> <hex codes...> : glyph id, advance, bbox (font units, y up)
            var tt = new Ttf(args[1]);
            Console.WriteLine("upem " + tt.UnitsPerEm + " glyphs " + tt.NumGlyphs + " cmap " + tt.Cmap.Count + " PUA F700: " + tt.Cmap.ContainsKey(0xF700));
            for (int i = 2; i < args.Length; i++)
            {
                int c = Convert.ToInt32(args[i], 16), g;
                if (!tt.Cmap.TryGetValue(c, out g)) { Console.WriteLine(args[i] + " missing"); continue; }
                double x0 = 1e9, x1 = -1e9, y0 = 1e9, y1 = -1e9;
                foreach (var ct in tt.Contours(g)) foreach (var pt in ct) { x0 = Math.Min(x0, pt.X); x1 = Math.Max(x1, pt.X); y0 = Math.Min(y0, pt.Y); y1 = Math.Max(y1, pt.Y); }
                Console.WriteLine("U+" + c.ToString("X4") + " " + char.ConvertFromUtf32(c) + " g" + g + " adv " + tt.Advance(g) + " x " + x0 + ".." + x1 + " y " + y0 + ".." + y1);
            }
            return 0;
        }
        if (args[0] == "movies")
        {
            // movies <dir> : list SwfMovie exports and the font names each one references or embeds
            foreach (var f in Directory.GetFiles(args[1], "*.upk"))
            {
                Package q;
                try { q = Package.Load(f); } catch { continue; }
                if (!q.Names.Contains("SwfMovie") && !q.Names.Contains("GFxMovieInfo")) continue;
                for (int i = 0; i < q.Exports.Count; i++)
                {
                    var e = q.Exports[i];
                    if (q.ObjName(e.Class) != "SwfMovie" && q.ObjName(e.Class) != "GFxMovieInfo") continue;
                    byte[] raw = null;
                    Subs.Walk(q, e.SerialOffset + 4, e.SerialOffset + e.SerialSize, delegate(int t, string n, int sp, int v, int s)
                    { if (n == "RawData") { raw = new byte[BitConverter.ToInt32(q.Data, v)]; Buffer.BlockCopy(q.Data, v + 4, raw, 0, raw.Length); } });
                    if (raw == null || raw.Length < 16) continue;
                    var info = new List<string>();
                    try
                    {
                        var swf = Swf.Unpack(raw);
                        foreach (var t in Swf.Tags(swf))
                        {
                            int b = t.Offset + t.HeaderLen;
                            if (t.Code == 75 || t.Code == 48) { int nl = swf[b + 4]; info.Add("embed:" + Encoding.UTF8.GetString(swf, b + 5, nl).TrimEnd('\0') + "/" + BitConverter.ToUInt16(swf, b + 5 + nl)); }
                            if (t.Code == 13 || t.Code == 62) info.Add("fontinfo");
                            if (t.Code == 57 || t.Code == 71) info.Add("import");
                        }
                        string all = Encoding.ASCII.GetString(swf);
                        foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(all, @"\$(Game|GameChain|GameLine|OCR|Title)\b|Subtitle\w*")) if (!info.Contains(m.Value)) info.Add(m.Value);
                    }
                    catch (Exception ex) { info.Add("err " + ex.Message); }
                    Console.WriteLine(Path.GetFileName(f) + "\t" + q.FullName(i + 1) + "\t" + raw.Length + "\t" + string.Join(" ", info.ToArray()));
                }
            }
            return 0;
        }
        if (args[0] == "scan")
        {
            // list packages whose name table matches a regex, with the matching names
            var rx = new System.Text.RegularExpressions.Regex(args[2], System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            foreach (var f in Directory.GetFiles(args[1], args.Length > 3 ? args[3] : "*.upk", SearchOption.AllDirectories))
            {
                Package q;
                try { q = Package.Load(f); } catch (Exception ex) { Console.Error.WriteLine(Path.GetFileName(f) + ": " + ex.Message); continue; }
                var hits = new List<string>();
                foreach (var n in q.Names) if (rx.IsMatch(n)) hits.Add(n);
                if (hits.Count > 0) Console.WriteLine(Path.GetFileName(f) + "\t" + new FileInfo(f).Length + "\t" + string.Join(",", hits.ToArray()));
            }
            return 0;
        }
        var p = Package.Load(args[1]);
        switch (args[0])
        {
            case "props":
                // props <pkg> <exportIndex|classFilter> : dump tagged properties (Arkham compact tag format)
                {
                    int one;
                    var list = new List<int>();
                    if (int.TryParse(args[2], out one)) list.Add(one);
                    else for (int i = 0; i < p.Exports.Count; i++) if (p.ObjName(p.Exports[i].Class) == args[2]) list.Add(i + 1);
                    int limit = args.Length > 3 ? int.Parse(args[3]) : 20;
                    foreach (int ei in list)
                    {
                        if (limit-- <= 0) break;
                        var e = p.Exports[ei - 1];
                        Console.WriteLine("== " + ei + " " + p.ObjName(e.Class) + " " + p.FullName(ei) + " size " + e.SerialSize);
                        DumpProps(p, e.SerialOffset + 4, e.SerialOffset + e.SerialSize, "  ");
                    }
                }
                break;
            case "swf":
                // swf <pkg> <export> [dumpPath] : list SWF tags of an SwfMovie RawData
                {
                    var e = p.Exports[int.Parse(args[2]) - 1];
                    byte[] raw = null;
                    Subs.Walk(p, e.SerialOffset + 4, e.SerialOffset + e.SerialSize, delegate(int t, string n, int sp, int v, int s)
                    {
                        if (n == "RawData") { int cnt = BitConverter.ToInt32(p.Data, v); raw = new byte[cnt]; Buffer.BlockCopy(p.Data, v + 4, raw, 0, cnt); }
                    });
                    var swf = Swf.Unpack(raw);
                    Console.WriteLine("sig " + (char)swf[0] + (char)swf[1] + (char)swf[2] + " ver " + swf[3] + " len " + swf.Length);
                    if (args.Length > 3) File.WriteAllBytes(args[3], swf);
                    foreach (var t in Swf.Tags(swf))
                    {
                        string extra = "";
                        int b = t.Offset + t.HeaderLen;
                        if (t.Code == 75 || t.Code == 48)
                        {
                            int id = BitConverter.ToUInt16(swf, b); int flags = swf[b + 2]; int nl = swf[b + 4];
                            string fname = Encoding.UTF8.GetString(swf, b + 5, nl).TrimEnd('\0');
                            int ng = BitConverter.ToUInt16(swf, b + 5 + nl);
                            extra = "id " + id + " flags 0x" + flags.ToString("X2") + " name '" + fname + "' glyphs " + ng;
                        }
                        else if (t.Code == 56 || t.Code == 57)
                        {
                            int cnt = BitConverter.ToUInt16(swf, b); int q2 = b + 2; var names = new List<string>();
                            for (int k = 0; k < cnt; k++) { int id = BitConverter.ToUInt16(swf, q2); q2 += 2; int z = q2; while (swf[z] != 0) z++; names.Add(id + "=" + Encoding.UTF8.GetString(swf, q2, z - q2)); q2 = z + 1; }
                            extra = string.Join(", ", names.ToArray());
                        }
                        else if (t.Code == 88 || t.Code == 73)
                            extra = "font " + BitConverter.ToUInt16(swf, b);
                        Console.WriteLine("tag " + t.Code + " @" + t.Offset + " len " + t.Length + " " + extra);
                    }
                }
                break;
            case "fontinfo":
            case "fontbuild":
                // fontinfo <pkg> | fontbuild <pkg> <out.upk> <font.ttf> [scaleMul]
                {
                    int ei = -1;
                    for (int i = 0; i < p.Exports.Count; i++) if (p.ObjName(p.Exports[i].Class) == "SwfMovie" || p.ObjName(p.Exports[i].Class) == "GFxMovieInfo") ei = i;
                    // FONT_EXPORT picks the movie in packages holding several (BmGame.u: GameHUD.HUD embeds its own fonts)
                    string fe = Environment.GetEnvironmentVariable("FONT_EXPORT");
                    if (!string.IsNullOrEmpty(fe)) ei = int.Parse(fe) - 1;
                    var e = p.Exports[ei];
                    int rawSizePos = 0, rawPos = 0, rawLen = 0;
                    Subs.Walk(p, e.SerialOffset + 4, e.SerialOffset + e.SerialSize, delegate(int t, string n, int sp, int v, int s)
                    { if (n == "RawData") { rawSizePos = sp; rawPos = v; rawLen = s; } });
                    byte[] raw = new byte[BitConverter.ToInt32(p.Data, rawPos)];
                    Buffer.BlockCopy(p.Data, rawPos + 4, raw, 0, raw.Length);
                    var swf = Swf.Unpack(raw);
                    // fontbuild <pkg> <out> <regular.ttf> [bold.ttf for 'BAC Game'] [scaleMul]
                    Ttf ttfRegular = args[0] == "fontbuild" ? new Ttf(args[3]) : null;
                    Ttf ttfBold = args.Length > 4 ? new Ttf(args[4]) : ttfRegular;
                    double mul = args.Length > 5 ? double.Parse(args[5], System.Globalization.CultureInfo.InvariantCulture) : 1.0;
                    var outTags = new MemoryStream();
                    int hdr = Swf.HeaderEnd(swf);
                    outTags.Write(swf, 0, hdr);
                    foreach (var t in Swf.Tags(swf))
                    {
                        if (t.Code != 75) { outTags.Write(swf, t.Offset, t.HeaderLen + t.Length); continue; }
                        var f = DefineFont3.Parse(swf, t.Offset + t.HeaderLen);
                        // caps-only fonts (Bou Collegiate) map lowercase to the capitals or lack it entirely
                        int ix = f.IndexOf('x') >= 0 ? f.IndexOf('x') : f.IndexOf('X'), iA = f.IndexOf('A');
                        int io = f.IndexOf('o') >= 0 ? f.IndexOf('o') : f.IndexOf('O');
                        int[] xbb; double xarea; DefineFont3.Measure(f.Shapes[ix], out xbb, out xarea);
                        int[] obb; double oarea; DefineFont3.Measure(f.Shapes[io], out obb, out oarea);
                        int[] hbb; double harea; DefineFont3.Measure(f.Shapes[f.IndexOf('H')], out hbb, out harea);
                        int capH = -hbb[2];
                        Console.WriteLine("  [" + f.Name + "] x bbox " + string.Join(",", Array.ConvertAll(xbb, z => z.ToString())) + "  H height " + capH + "  o first contour area " + oarea);
                        Ttf ttf = ttfRegular == null ? null : (f.Name == "BAC Game" ? ttfBold : ttfRegular);
                        if (ttf == null)
                        {
                            var br = new BitReader(f.Shapes[iA], 0);
                            int nf = br.UB(4), nlb = br.UB(4);
                            Console.WriteLine("font '" + f.Name + "' id " + f.Id + " flags 0x" + f.Flags.ToString("X2") + " glyphs " + f.Codes.Count + " asc " + f.Ascent + " desc " + f.Descent + " lead " + f.Leading + " kern " + f.Kerning.Count);
                            Console.WriteLine("  x: adv " + f.Advances[ix] + " bounds " + string.Join(",", Array.ConvertAll(f.Bounds[ix], z => z.ToString())) + "  A: adv " + f.Advances[iA] + " bounds " + string.Join(",", Array.ConvertAll(f.Bounds[iA], z => z.ToString())));
                            Console.WriteLine("  A shape: fillbits " + nf + " linebits " + nlb + " first rec type " + br.UB(1) + " newstyles " + br.UB(1) + " line " + br.UB(1) + " fill1 " + br.UB(1) + " fill0 " + br.UB(1) + " move " + br.UB(1));
                            var cs = new List<string>(); foreach (int c in f.Codes) if (c > 0x7E) cs.Add("U+" + c.ToString("X4"));
                            Console.WriteLine("  non-ascii: " + string.Join(" ", cs.ToArray()));
                            outTags.Write(swf, t.Offset, t.HeaderLen + t.Length);
                            continue;
                        }
                        // scale Thai so its consonant height is 0.772 x this font's cap height (Tahoma's ratio; Chainprinter is
                        // caps-only, so x-height is unusable). Measured on 'ก' so Thai-only fonts (no Latin) work too.
                        Func<int, double[]> box = delegate(int code)
                        {
                            double[] r = { 1e9, -1e9, 1e9, -1e9 };   // x0 x1 y0 y1
                            foreach (var ct in ttf.Contours(ttf.Cmap[code])) foreach (var pt in ct)
                            { r[0] = Math.Min(r[0], pt.X); r[1] = Math.Max(r[1], pt.X); r[2] = Math.Min(r[2], pt.Y); r[3] = Math.Max(r[3], pt.Y); }
                            return r;
                        };
                        double kTop = box(0x0E01)[3];
                        double scale = capH * 0.772 / kTop * mul;
                        int[] tb; double tarea;
                        int refCode = ttf.Cmap.ContainsKey('o') ? 'o' : 0x0E01;
                        DefineFont3.Measure(DefineFont3.EncodeShape(ttf.Contours(ttf.Cmap[refCode]), scale, 0, 0, out tb, false), out tb, out tarea);
                        bool reverse = Math.Sign(tarea) != Math.Sign(oarea);
                        Console.WriteLine("  [" + f.Name + "] ttf ref area " + tarea + " -> reverse " + reverse);
                        int added = 0;
                        // (code, source code, shift x, shift y) in TTF units, y up.
                        // Base tone marks are raised to the "high" slot; the PUA forms used by ThaiShaper hold the other positions.
                        // Offsets come from the font's own geometry (Tahoma gives ~470/230/420 at 2048 upem):
                        //   UP   - lift a tone mark from its base slot to just above the tallest upper vowel (ื)
                        //   LEFT - move upper marks clear of the ascender of ป ฝ ฟ
                        //   DOWN - drop lower vowels below the descender of ฎ ฏ
                        double em = ttf.UnitsPerEm;
                        int UP = (int)Math.Round(box(0x0E37)[3] + 0.035 * em - box(0x0E48)[2]);
                        int LEFT = (int)Math.Round(ttf.Advance(ttf.Cmap[0x0E1B]) * 230.0 / 1290.0);
                        int DOWN = (int)Math.Round(box(0x0E38)[3] - box(0x0E0E)[2] + 0.033 * em);
                        Console.WriteLine("  [" + f.Name + "] mark offsets up " + UP + " left " + LEFT + " down " + DOWN + " (upem " + em + ")");
                        var plan = new List<int[]>();
                        for (int c = 0x0E01; c <= 0x0E5B; c++) plan.Add(new int[] { c, c, 0, (c >= 0x0E48 && c <= 0x0E4C) ? UP : 0 });
                        for (int k = 0; k < 5; k++)
                        {
                            plan.Add(new int[] { 0xF70A + k, 0x0E48 + k, 0, 0 });        // low
                            plan.Add(new int[] { 0xF705 + k, 0x0E48 + k, -LEFT, 0 });    // low-left
                            plan.Add(new int[] { 0xF713 + k, 0x0E48 + k, -LEFT, UP });   // high-left
                        }
                        int[] avSrc = { 0x0E34, 0x0E35, 0x0E36, 0x0E37 };
                        for (int k = 0; k < 4; k++) plan.Add(new int[] { 0xF701 + k, avSrc[k], -LEFT, 0 });
                        plan.Add(new int[] { 0xF710, 0x0E31, -LEFT, 0 });
                        plan.Add(new int[] { 0xF711, 0x0E4D, -LEFT, 0 });
                        plan.Add(new int[] { 0xF712, 0x0E47, -LEFT, 0 });
                        plan.Add(new int[] { 0xF718, 0x0E38, 0, -DOWN });
                        plan.Add(new int[] { 0xF719, 0x0E39, 0, -DOWN });
                        plan.Add(new int[] { 0xF71A, 0x0E3A, 0, -DOWN });
                        foreach (var pl in plan)
                        {
                            int c = pl[0], g;
                            if (!ttf.Cmap.TryGetValue(pl[1], out g) || f.IndexOf(c) >= 0) continue;
                            int[] bounds;
                            var shape = DefineFont3.EncodeShape(ttf.Contours(g), scale, pl[2] * scale, -pl[3] * scale, out bounds, reverse);
                            f.Shapes.Add(shape); f.Codes.Add(c); f.Advances.Add((int)Math.Round(ttf.Advance(g) * scale)); f.Bounds.Add(bounds);
                            added++;
                        }
                        // copy every Thai glyph onto its cp1252 slot (replacing the Latin glyph there)
                        var slotCodes = new HashSet<int>();
                        foreach (var kv in Cp1252Thai.Map)
                        {
                            int src = f.IndexOf(kv.Key);
                            if (src < 0) continue;
                            int dst = f.IndexOf(kv.Value);
                            if (dst < 0) { f.Shapes.Add(f.Shapes[src]); f.Codes.Add(kv.Value); f.Advances.Add(f.Advances[src]); f.Bounds.Add(f.Bounds[src]); }
                            else { f.Shapes[dst] = f.Shapes[src]; f.Advances[dst] = f.Advances[src]; f.Bounds[dst] = f.Bounds[src]; }
                            slotCodes.Add(kv.Value);
                        }
                        int kernBefore = f.Kerning.Count;
                        f.Kerning.RemoveAll(k => slotCodes.Contains(k[0]) || slotCodes.Contains(k[1]));
                        Console.WriteLine("  [" + f.Name + "] cp1252 slots " + slotCodes.Count + ", kerning pairs removed " + (kernBefore - f.Kerning.Count));
                        var body = f.Serialize();
                        var bw2 = new BinaryWriter(outTags);
                        bw2.Write((ushort)((75 << 6) | 0x3F)); bw2.Write(body.Length); bw2.Write(body);
                        Console.WriteLine("font '" + f.Name + "': +" + added + " glyphs, scale " + scale.ToString("0.###") + ", size " + t.Length + " -> " + body.Length);
                    }
                    if (ttfRegular == null) break;
                    byte[] nswf = outTags.ToArray();
                    nswf[0] = (byte)'G'; nswf[1] = (byte)'F'; nswf[2] = (byte)'X';
                    BitConverter.GetBytes(nswf.Length).CopyTo(nswf, 4);
                    // new export: [before RawData value][count + swf][after]
                    int relSize = rawSizePos - e.SerialOffset, relVal = rawPos - e.SerialOffset;
                    var ex = new MemoryStream();
                    ex.Write(p.Data, e.SerialOffset, relVal);
                    ex.Write(BitConverter.GetBytes(nswf.Length), 0, 4);
                    ex.Write(nswf, 0, nswf.Length);
                    ex.Write(p.Data, rawPos + rawLen, e.SerialOffset + e.SerialSize - rawPos - rawLen);
                    byte[] exb = ex.ToArray();
                    BitConverter.GetBytes(nswf.Length + 4).CopyTo(exb, relSize);
                    var pkg = new MemoryStream();
                    pkg.Write(p.Data, 0, p.Data.Length);
                    int newOff = (int)pkg.Length;
                    pkg.Write(exb, 0, exb.Length);
                    byte[] all = pkg.ToArray();
                    BitConverter.GetBytes(exb.Length).CopyTo(all, e.SizeFieldPos);
                    BitConverter.GetBytes(newOff).CopyTo(all, e.SizeFieldPos + 4);
                    File.WriteAllBytes(args[2], all);
                    Console.WriteLine("wrote " + args[2] + " (" + all.Length + " bytes)");
                }
                break;
            case "render":
                // render <fontpkg> <out.png> <lines.txt> : draw each line with every font in the library (after Thai shaping)
                {
                    int ei = -1;
                    for (int i = 0; i < p.Exports.Count; i++) if (p.ObjName(p.Exports[i].Class) == "SwfMovie" || p.ObjName(p.Exports[i].Class) == "GFxMovieInfo") ei = i;
                    var e = p.Exports[ei];
                    byte[] raw = null;
                    Subs.Walk(p, e.SerialOffset + 4, e.SerialOffset + e.SerialSize, delegate(int t, string n, int sp, int v, int s)
                    { if (n == "RawData") { raw = new byte[BitConverter.ToInt32(p.Data, v)]; Buffer.BlockCopy(p.Data, v + 4, raw, 0, raw.Length); } });
                    var swf = Swf.Unpack(raw);
                    var fonts = new List<DefineFont3>();
                    foreach (var t in Swf.Tags(swf)) if (t.Code == 75) fonts.Add(DefineFont3.Parse(swf, t.Offset + t.HeaderLen));
                    var lines = File.ReadAllLines(args[3], Encoding.UTF8);
                    float sc = 48f / 20480f; // 48px em
                    int lh = 80, W = 1800, H = 20 + lh * lines.Length * fonts.Count + 20 * fonts.Count;
                    using (var bmp = new System.Drawing.Bitmap(W, H))
                    using (var g = System.Drawing.Graphics.FromImage(bmp))
                    {
                        g.Clear(System.Drawing.Color.FromArgb(30, 30, 34));
                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                        float yy = 20;
                        foreach (var f in fonts)
                        {
                            g.DrawString(f.Name, new System.Drawing.Font("Segoe UI", 10), System.Drawing.Brushes.Gray, 10, yy - 16);
                            foreach (var line in lines)
                            {
                                string shaped = Enc(ThaiShaper.Shape(line));
                                var gp = new System.Drawing.Drawing2D.GraphicsPath(System.Drawing.Drawing2D.FillMode.Alternate);
                                float px = 20, base_ = yy + 56;
                                foreach (char ch in shaped)
                                {
                                    int gi = f.IndexOf(ch);
                                    if (gi < 0) { g.DrawRectangle(System.Drawing.Pens.Red, px, base_ - 30, 16, 30); px += 20; continue; }
                                    GlyphRender.AddGlyph(gp, f.Shapes[gi], px, base_, sc);
                                    px += f.Advances[gi] * sc;
                                }
                                g.FillPath(System.Drawing.Brushes.White, gp);
                                g.DrawLine(new System.Drawing.Pen(System.Drawing.Color.FromArgb(60, 80, 160, 255)), 0, base_, W, base_);
                                yy += lh;
                            }
                            yy += 20;
                        }
                        bmp.Save(args[2], System.Drawing.Imaging.ImageFormat.Png);
                    }
                    Console.WriteLine("saved " + args[2]);
                }
                break;
            case "bmglyphs":
                // bmglyphs <pkg> <fontExport> <out.png> <text> : the font's cells for these characters, 6x, on grey
                {
                    var bf = BmFontData.Load(p, int.Parse(args[2]));
                    var pages = new List<uint[]>(); var sizes = new List<int[]>();
                    foreach (int ti in bf.Textures) { var tx = BmTexture.Load(p, ti); pages.Add(Dxt5.Decode(p.Data, tx.MipDataPos, tx.SizeX, tx.SizeY)); sizes.Add(new[] { tx.SizeX, tx.SizeY }); }
                    string text = args[4]; int Z = 6;
                    using (var bmp = new System.Drawing.Bitmap(text.Length * 32 * Z, 32 * Z))
                    {
                        using (var g = System.Drawing.Graphics.FromImage(bmp)) g.Clear(System.Drawing.Color.FromArgb(90, 100, 110));
                        int pen = 2;
                        foreach (char ch in text)
                        {
                            int idx; if (!bf.Remap.TryGetValue(ch, out idx)) { if (bf.Remap.Count == 0) idx = ch; else continue; }
                            var c = bf.Chars[idx]; var pg = pages[c.Tex]; int sw = sizes[c.Tex][0];
                            Console.WriteLine("'" + ch + "' " + c.W + "x" + c.H + " voff " + c.VOff);
                            for (int y = 0; y < c.H; y++) for (int x = 0; x < c.W; x++)
                            {
                                uint v = pg[(c.V + y) * sw + c.U + x]; int a = (int)(v >> 24), l = (int)(v & 255);
                                var col = System.Drawing.Color.FromArgb((90 * (255 - a) + l * a) / 255, (100 * (255 - a) + l * a) / 255, (110 * (255 - a) + l * a) / 255);
                                for (int zy = 0; zy < Z; zy++) for (int zx = 0; zx < Z; zx++) bmp.SetPixel((pen + x) * Z + zx, (c.VOff + y + 2) * Z + zy, col);
                            }
                            pen += c.W + 2;
                        }
                        bmp.Save(args[3], System.Drawing.Imaging.ImageFormat.Png);
                    }
                }
                break;
            case "bmdump":
                // bmdump <pkg> <fontExport> <out.png> : bitmap font table + texture page
                {
                    var bf = BmFontData.Load(p, int.Parse(args[2]));
                    Console.WriteLine("chars " + bf.Chars.Count + " remap " + bf.Remap.Count + " textures " + string.Join(",", bf.Textures.ConvertAll(x => x.ToString()).ToArray()) + " kerning " + (bf.HasKerning ? bf.Kerning.ToString() : "default"));
                    foreach (var kv in bf.Remap)
                    {
                        if (kv.Key < 0x20 || (kv.Key > 0x7E && kv.Key < 0xC0) || kv.Key > 0xFF) continue;
                        var c = bf.Chars[kv.Value];
                        if ("HxgAa ".IndexOf((char)kv.Key) < 0 && kv.Key != 0xE0) continue;
                        Console.WriteLine("  '" + (char)kv.Key + "' #" + kv.Value + " uv " + c.U + "," + c.V + " size " + c.W + "x" + c.H + " tex " + c.Tex + " voff " + c.VOff);
                    }
                    var tx = BmTexture.Load(p, bf.Textures[0]);
                    Console.WriteLine("texture " + tx.SizeX + "x" + tx.SizeY + " " + tx.Format + " mip bytes " + tx.MipDataLen);
                    var px = Dxt5.Decode(p.Data, tx.MipDataPos, tx.SizeX, tx.SizeY);
                    using (var bmp = new System.Drawing.Bitmap(tx.SizeX * 2, tx.SizeY))
                    {
                        for (int y = 0; y < tx.SizeY; y++)
                            for (int x = 0; x < tx.SizeX; x++)
                            {
                                uint c = px[y * tx.SizeX + x];
                                bmp.SetPixel(x, y, System.Drawing.Color.FromArgb((int)(c | 0xFF000000)));                 // colour
                                int a = (int)(c >> 24); bmp.SetPixel(tx.SizeX + x, y, System.Drawing.Color.FromArgb(255, a, a, a)); // alpha
                            }
                        bmp.Save(args[3], System.Drawing.Imaging.ImageFormat.Png);
                    }
                }
                break;
            case "name":
                for (int i = 2; i < args.Length; i++) { int ni = Convert.ToInt32(args[i], 16); Console.WriteLine(args[i] + " = " + (ni < p.Names.Count ? p.Names[ni] : "?")); }
                break;
            case "decompress":
                File.WriteAllBytes(args[2], p.Data);
                Console.WriteLine("ok " + p.Data.Length);
                break;
            case "info":
                Console.WriteLine("ver " + p.Version + "/" + p.Licensee + " names " + p.NameCount + " imports " + p.ImportCount + " exports " + p.ExportCount + " size " + p.Data.Length);
                var counts = new SortedDictionary<string, int>();
                foreach (var e in p.Exports) { string c = p.ObjName(e.Class); int v; counts.TryGetValue(c, out v); counts[c] = v + 1; }
                foreach (var kv in counts) Console.WriteLine("  " + kv.Value.ToString().PadLeft(5) + "  " + kv.Key);
                break;
            case "exports":
                string filter = args.Length > 2 ? args[2] : null;
                for (int i = 0; i < p.Exports.Count; i++)
                {
                    var e = p.Exports[i];
                    string cls = p.ObjName(e.Class);
                    if (filter != null && cls.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0) continue;
                    Console.WriteLine((i + 1) + "\t" + cls + "\t" + p.FullName(i + 1) + "\t@" + e.SerialOffset + "\t" + e.SerialSize);
                }
                break;
            case "strings":
                // FStrings embedded in export data: int32 length then text (ANSI or UTF-16)
                var d = p.Data;
                int minLen = args.Length > 2 ? int.Parse(args[2]) : 12;
                for (int i = 0; i < p.Exports.Count; i++)
                {
                    var e = p.Exports[i];
                    int end = e.SerialOffset + e.SerialSize;
                    for (int o = e.SerialOffset; o + 4 < end; o++)
                    {
                        int len = BitConverter.ToInt32(d, o);
                        if (len >= minLen && len < 4000 && o + 4 + len <= end && d[o + 4 + len - 1] == 0)
                        {
                            bool ok = true;
                            for (int k = 0; k < len - 1; k++) { byte b = d[o + 4 + k]; if (b < 9 || (b > 13 && b < 32)) { ok = false; break; } }
                            if (ok) { Console.WriteLine((i + 1) + "\t" + p.ObjName(e.Class) + "\t" + e.Name + "\t+" + (o - e.SerialOffset) + "\tA\t" + Encoding.GetEncoding(1252).GetString(d, o + 4, len - 1)); o += 3 + len; }
                        }
                        else if (len <= -minLen && len > -4000 && o + 4 - len * 2 <= end)
                        {
                            int n = -len;
                            if (d[o + 4 + (n - 1) * 2] == 0 && d[o + 5 + (n - 1) * 2] == 0)
                            {
                                string s = Encoding.Unicode.GetString(d, o + 4, (n - 1) * 2);
                                bool ok = true;
                                foreach (char ch in s) if (ch < 9 || (ch > 13 && ch < 32)) { ok = false; break; }
                                if (ok) { Console.WriteLine((i + 1) + "\t" + p.ObjName(e.Class) + "\t" + e.Name + "\t+" + (o - e.SerialOffset) + "\tU\t" + s); o += 3 + n * 2; }
                            }
                        }
                    }
                }
                break;
        }
        return 0;
    }
}
