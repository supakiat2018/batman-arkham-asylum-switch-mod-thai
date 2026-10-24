using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

// CRI USM (Sofdec2) subtitle stream (@SBT) reader/writer for the pre-rendered cutscenes.
// Chunk: sig[4] size(BE32, bytes after this field) | +8 unk | +9 headerSize | +10 padSize(BE16) | +15 type | ...
// SBT data payload (LE): lang, timescale, start, duration, textLen, text ("<sec>|<line>\0..").
// Every subtitle event is stored once per language as a contiguous run of @SBT chunks, so a run can be
// rewritten in place with the same total byte length - no seek tables or stream sizes need to change.
static class UsmTool
{
    class Sbt { public int Off, Size, Hdr, Pad, Lang, Start, Dur; public byte[] Raw; public string Text; }

    static int BE32(byte[] b, int o) { return (b[o] << 24) | (b[o + 1] << 16) | (b[o + 2] << 8) | b[o + 3]; }
    static void PutBE32(byte[] b, int o, int v) { b[o] = (byte)(v >> 24); b[o + 1] = (byte)(v >> 16); b[o + 2] = (byte)(v >> 8); b[o + 3] = (byte)v; }

    static bool IsSbt(byte[] b, int o) { return b[o] == 0x40 && b[o + 1] == 0x53 && b[o + 2] == 0x42 && b[o + 3] == 0x54; }

    // contiguous runs of SBT data chunks
    static List<List<Sbt>> Runs(byte[] b)
    {
        var runs = new List<List<Sbt>>();
        List<Sbt> cur = null;
        int o = 0;
        while (o + 16 <= b.Length)
        {
            int size = BE32(b, o + 4);
            if (size <= 0 || o + 8 + size > b.Length) break;
            if (IsSbt(b, o) && b[o + 15] == 0)
            {
                var s = new Sbt { Off = o, Size = size, Hdr = b[o + 9], Pad = (b[o + 10] << 8) | b[o + 11] };
                int p = o + 8 + s.Hdr;
                s.Lang = BitConverter.ToInt32(b, p); s.Start = BitConverter.ToInt32(b, p + 8); s.Dur = BitConverter.ToInt32(b, p + 12);
                int len = BitConverter.ToInt32(b, p + 16);
                s.Text = Encoding.UTF8.GetString(b, p + 20, len).TrimEnd('\0');
                s.Raw = new byte[8 + size]; Buffer.BlockCopy(b, o, s.Raw, 0, s.Raw.Length);
                if (cur == null) { cur = new List<Sbt>(); runs.Add(cur); }
                cur.Add(s);
            }
            else cur = null;
            o += 8 + size;
        }
        return runs;
    }

    static string Body(string t) { int bar = t.IndexOf('|'); return bar >= 0 ? t.Substring(bar + 1) : t; }
    static string Prefix(Sbt s) { return s.Text.Substring(0, s.Text.Length - Body(s.Text).Length); }
    static string Key(Sbt s) { return Body(s.Text).Replace("\t", " ").Replace("\n", "\\n"); }
    static int Need(Sbt s, string text) { return 8 + s.Hdr + 20 + ((Encoding.UTF8.GetByteCount(text) + 4) & ~3); }

    // build one SBT chunk from a template chunk, new lang/text, and an exact total length (pad absorbs slack)
    static byte[] Build(Sbt tpl, int lang, string text, int total)
    {
        byte[] tb = Encoding.UTF8.GetBytes(text);
        int tlen = (tb.Length + 1 + 3) & ~3;
        int need = 8 + tpl.Hdr + 20 + tlen;
        if (need > total) return null;
        var c = new byte[total];
        Buffer.BlockCopy(tpl.Raw, 0, c, 0, 8 + tpl.Hdr);
        PutBE32(c, 4, total - 8);
        int pad = total - need;
        if (pad > 0xFFFF) return null;
        c[10] = (byte)(pad >> 8); c[11] = (byte)pad;
        int p = 8 + tpl.Hdr;
        Buffer.BlockCopy(tpl.Raw, p, c, p, 20);
        BitConverter.GetBytes(lang).CopyTo(c, p);
        BitConverter.GetBytes(tlen).CopyTo(c, p + 16);
        Buffer.BlockCopy(tb, 0, c, p + 20, tb.Length);
        return c;
    }

    // Write Thai lang-0 chunks then other chunks (kept while they fit) into exactly `total` bytes at `start`;
    // the last part's padding absorbs the slack.
    static bool WriteRun(byte[] b, int start, int total, List<KeyValuePair<Sbt, string>> thai, List<Sbt> others)
    {
        int used = 0; foreach (var kv in thai) used += Need(kv.Key, kv.Value);
        if (used > total) return false;
        var keep = new List<Sbt>();
        foreach (var s in others) if (used + 8 + s.Size <= total) { keep.Add(s); used += 8 + s.Size; }
        if (thai.Count + keep.Count == 0) return false;
        var parts = new List<byte[]>();
        for (int i = 0; i < thai.Count; i++)
        {
            int len = Need(thai[i].Key, thai[i].Value);
            if (keep.Count == 0 && i == thai.Count - 1) len += total - used;
            parts.Add(Build(thai[i].Key, 0, thai[i].Value, len));
        }
        for (int i = 0; i < keep.Count; i++)
        {
            int len = 8 + keep[i].Size;
            if (i == keep.Count - 1) len += total - used;
            parts.Add(Build(keep[i], keep[i].Lang, keep[i].Text, len));
        }
        int o = start;
        foreach (var c in parts) { if (c == null) return false; Buffer.BlockCopy(c, 0, b, o, c.Length); o += c.Length; }
        return o == start + total;
    }

    static int BE16(byte[] b, int o) { return (b[o] << 8) | b[o + 1]; }
    static string CStr(byte[] b, int o) { int e = o; while (e < b.Length && b[e] != 0) e++; return Encoding.ASCII.GetString(b, o, e - o); }

    // Absolute positions of every per-row "ofs_byte" value (BE u32/u64) in all @UTF tables of non-media chunks.
    class OfsField { public int Pos, Width; }
    static List<OfsField> OfsFields(byte[] b, bool dump)
    {
        var res = new List<OfsField>();
        int o = 0;
        while (o + 16 <= b.Length)
        {
            int size = BE32(b, o + 4);
            if (size <= 0 || o + 8 + size > b.Length) break;
            string sig = Encoding.ASCII.GetString(b, o, 4);
            int type = b[o + 15];
            bool media = (sig == "@SFV" || sig == "@SFA" || sig == "@ALP") && type == 0;
            if (!media && sig != "@SBT")
            {
                int end = o + 8 + size;
                for (int u = o + 8; u + 32 < end; u++)
                {
                    if (b[u] != 0x40 || b[u + 1] != 0x55 || b[u + 2] != 0x54 || b[u + 3] != 0x46) continue;
                    int t = u + 8;                                   // offsets below are relative to t
                    int rowsOff = BE16(b, t + 2), strOff = BE32(b, t + 4);
                    int nameOff = BE32(b, t + 12), ncol = BE16(b, t + 16), rowLen = BE16(b, t + 18), nrow = BE32(b, t + 20);
                    string table = CStr(b, t + strOff + nameOff);
                    int c = t + 24, rowPos = 0;
                    var cols = new List<string>();
                    for (int k = 0; k < ncol; k++)
                    {
                        int flags = b[c]; string name = CStr(b, t + strOff + BE32(b, c + 1)); c += 5;
                        int st = flags & 0xF0, ty = flags & 0x0F;
                        int w = ty <= 1 ? 1 : ty <= 3 ? 2 : ty <= 5 ? 4 : ty <= 7 ? 8 : ty == 8 ? 4 : 8;
                        if (st == 0x30) c += w;
                        if (st == 0x50)
                        {
                            if (name == "ofs_byte") for (int r = 0; r < nrow; r++) res.Add(new OfsField { Pos = t + rowsOff + r * rowLen + rowPos, Width = w });
                            rowPos += w;
                        }
                        cols.Add(name + ":" + st.ToString("x") + "/" + ty);
                    }
                    if (dump) Console.WriteLine("  @" + (o) + " " + sig + "/" + type + " table " + table + " rows " + nrow + " cols " + string.Join(" ", cols.ToArray()));
                    u = t + BE32(b, u + 4) - 8;
                }
            }
            o += 8 + size;
        }
        return res;
    }
    static long ReadBE(byte[] b, OfsField f) { long v = 0; for (int i = 0; i < f.Width; i++) v = (v << 8) | b[f.Pos + i]; return v; }
    static void WriteBE(byte[] b, OfsField f, long v) { for (int i = f.Width - 1; i >= 0; i--) { b[f.Pos + i] = (byte)v; v >>= 8; } }

    public static int Run(string[] args)
    {
        if (args[0] == "usmverify")
        {
            // usmverify <origDir> <newDir> : chunk walk ends exactly at EOF, same size, seek entries hit @SFV, chunk sequence unchanged
            int bad = 0;
            foreach (var f in Directory.GetFiles(args[2], "*.usm"))
            {
                var nb = File.ReadAllBytes(f); var ob = File.ReadAllBytes(Path.Combine(args[1], Path.GetFileName(f)));
                var problems = new List<string>();
                if (nb.Length != ob.Length) problems.Add("size " + nb.Length + " != " + ob.Length);
                var seqN = new List<string>(); var seqO = new List<string>();
                foreach (var pair in new[] { new { B = nb, S = seqN }, new { B = ob, S = seqO } })
                {
                    int o = 0;
                    while (o + 8 <= pair.B.Length) { int sz = BE32(pair.B, o + 4); if (sz <= 0) break; string sig = Encoding.ASCII.GetString(pair.B, o, 4); if (sig != "@SBT") pair.S.Add(sig + pair.B[o + 15] + ":" + sz); o += 8 + sz; }
                    if (o != pair.B.Length && pair.B == nb) problems.Add("walk ended at " + o);
                }
                if (string.Join(",", seqN.ToArray()) != string.Join(",", seqO.ToArray())) problems.Add("media chunk sequence differs");
                foreach (var fld in OfsFields(nb, false)) { long v = ReadBE(nb, fld); if (v + 4 > nb.Length || Encoding.ASCII.GetString(nb, (int)v, 4) != "@SFV") { problems.Add("seek -> " + v); break; } }
                int th = 0, en = 0;
                foreach (var run in Runs(nb)) foreach (var s in run) if (s.Lang == 0) { if (System.Text.RegularExpressions.Regex.IsMatch(s.Text, "[฀-๿-]")) th++; else en++; }
                Console.WriteLine(Path.GetFileName(f) + "\tthai " + th + "\tenglish " + en + (problems.Count > 0 ? "\tPROBLEM " + string.Join("; ", problems.ToArray()) : "\tok"));
                if (problems.Count > 0 || en > 0) bad++;
            }
            Console.WriteLine(bad == 0 ? "ALL OK" : bad + " files with problems");
            return bad;
        }
        if (args[0] == "usmutf")
        {
            var b = File.ReadAllBytes(args[1]);
            var fs = OfsFields(b, true);
            Console.WriteLine("ofs_byte values: " + fs.Count);
            for (int i = 0; i < fs.Count && i < 8; i++) Console.WriteLine("  " + ReadBE(b, fs[i]) + " -> sig " + Encoding.ASCII.GetString(b, (int)ReadBE(b, fs[i]), 4));
            return 0;
        }
        if (args[0] == "usmextract")
        {
            // usmextract <moviesDir> <out.tsv> : file \t start \t english (lang 0)
            using (var w = new StreamWriter(args[2], false, new UTF8Encoding(false)))
                foreach (var f in Directory.GetFiles(args[1], "*.usm"))
                {
                    var b = File.ReadAllBytes(f);
                    foreach (var run in Runs(b))
                        foreach (var s in run)
                            if (s.Lang == 0) w.WriteLine(Path.GetFileName(f) + "\t" + s.Start + "\t" + Body(s.Text).Replace("\t", " ").Replace("\n", "\\n"));
                }
            return 0;
        }
        if (args[0] == "usmscan")
        {
            // usmscan <moviesDir> : runs, languages per run, byte budget vs english
            foreach (var f in Directory.GetFiles(args[1], "*.usm"))
            {
                var runs = Runs(File.ReadAllBytes(f));
                if (runs.Count == 0) continue;
                int en = 0, odd = 0; double minR = 99;
                foreach (var run in runs)
                {
                    var langs = new HashSet<int>(); int tot = 0; Sbt e = null;
                    foreach (var s in run) { if (!langs.Add(s.Lang)) odd++; tot += 8 + s.Size; if (s.Lang == 0) e = s; }
                    if (e == null) { odd++; continue; }
                    en++;
                    int eb = Encoding.UTF8.GetByteCount(Body(e.Text));
                    if (eb > 8) minR = Math.Min(minR, (tot - 64.0) / eb);
                }
                Console.WriteLine(Path.GetFileName(f) + "\truns " + runs.Count + "\ten " + en + "\todd " + odd + "\tminBudget " + minR.ToString("0.0") + "x");
            }
            return 0;
        }
        if (args[0] == "usminject")
        {
            // usminject <moviesDir> <outDir> <tsv english\tthai> : writes only movies that changed
            var tr = new TrTable(args[3], false);
            Directory.CreateDirectory(args[2]);
            int files = 0, done = 0, miss = 0, fail = 0;
            foreach (var f0 in Directory.GetFiles(args[1], "*.usm"))
            {
                // optional 5th arg: directory of pristine originals (backup) that takes precedence
                string f = f0;
                if (args.Length > 4 && File.Exists(Path.Combine(args[4], Path.GetFileName(f0)))) f = Path.Combine(args[4], Path.GetFileName(f0));
                var b = File.ReadAllBytes(f);
                var runs = Runs(b);
                int changed = 0;
                var plan = new Dictionary<List<Sbt>, List<KeyValuePair<Sbt, string>>>(); var blocked = new HashSet<List<Sbt>>();
                var pending = new List<KeyValuePair<List<Sbt>, List<KeyValuePair<Sbt, string>>>>();
                foreach (var run in runs)
                {
                    var thai = new List<KeyValuePair<Sbt, string>>();
                    foreach (var e in run)
                    {
                        if (e.Lang != 0) continue;
                        string th;
                        if (!tr.TryGet(Key(e), out th) && !tr.TryGet(TrTable.KeyExact(Key(e)), out th)) { miss++; Console.Error.WriteLine(Path.GetFileName(f) + " untranslated: [" + Key(e) + "]"); continue; }
                        thai.Add(new KeyValuePair<Sbt, string>(e, Prefix(e) + ThaiShaper.Shape(ThaiWrap.Apply(th.Replace("\\n", "\n"), 40))));
                    }
                    plan[run] = thai;
                    if (thai.Count == 0) continue;
                    int total = 0; foreach (var s in run) total += 8 + s.Size;
                    int used = 0; foreach (var kv in thai) used += Need(kv.Key, kv.Value);
                    if (used > total) { blocked.Add(run); pending.Add(new KeyValuePair<List<Sbt>, List<KeyValuePair<Sbt, string>>>(run, thai)); continue; }
                    var others = new List<Sbt>();
                    foreach (var s in run) if (!thai.Exists(kv => kv.Key == s)) others.Add(s);
                    if (!WriteRun(b, run[0].Off, total, thai, others)) throw new Exception("build failed " + f);
                    changed += thai.Count; done += thai.Count;
                }
                // Runs too small for the Thai text: grow the run by taking bytes from the next other-language-only run,
                // shifting the media chunks in between, and fix every seek-table offset in that window.
                if (pending.Count > 0)
                {
                    var ofs = OfsFields(b, false);
                    foreach (var pr in pending)
                    {
                        var run = pr.Key; var thai = pr.Value;
                        int total = 0; foreach (var s in run) total += 8 + s.Size;
                        int need = 0; foreach (var kv in thai) need += Need(kv.Key, kv.Value);
                        int d = (need - total + 31) & ~31;
                        int start = run[0].Off, end = start + total;
                        List<Sbt> donor = null; int dTotal = 0;
                        foreach (var r2 in runs)
                        {
                            if (r2 == run || blocked.Contains(r2)) continue;
                            int t2 = 0; foreach (var s in r2) t2 += 8 + s.Size;
                            int left = t2 - d, min = 0; foreach (var kv in plan[r2]) min += Need(kv.Key, kv.Value);
                            if (left < min || (min == 0 && left > 0 && left < Need(r2[0], Prefix(r2[0])))) continue;
                            if (donor == null || Math.Abs(r2[0].Off - start) < Math.Abs(donor[0].Off - start)) { donor = r2; dTotal = t2; }
                        }
                        if (donor == null) { fail++; Console.Error.WriteLine(Path.GetFileName(f) + " t=" + thai[0].Key.Start + ": no room and no donor run"); continue; }
                        int dStart = donor[0].Off, dEnd = dStart + dTotal, left2 = dTotal - d, newStart, donorAt, lo, hi, delta;
                        if (dStart >= end)
                        {   // donor after: shift [end, dStart) forward
                            lo = end; hi = dStart; delta = d; newStart = start; donorAt = dStart + d;
                            Buffer.BlockCopy(b, lo, b, lo + d, hi - lo);
                        }
                        else
                        {   // donor before: shrink donor, shift [dEnd, start) backward
                            lo = dEnd; hi = start; delta = -d; newStart = start - d; donorAt = dStart;
                            Buffer.BlockCopy(b, lo, b, lo - d, hi - lo);
                        }
                        foreach (var fld in ofs) { long v = ReadBE(b, fld); if (v >= lo && v < hi) WriteBE(b, fld, v + delta); }
                        foreach (var r in runs) foreach (var s in r) if (s.Off >= lo && s.Off < hi) s.Off += delta;
                        if (!WriteRun(b, newStart, total + d, thai, new List<Sbt>())) throw new Exception("grow failed " + f);
                        var dThai = plan[donor]; var dOthers = donor.FindAll(s => !dThai.Exists(kv => kv.Key == s));
                        if (left2 > 0 && !WriteRun(b, donorAt, left2, dThai, dOthers))
                        {
                            var filler = Build(donor[0], donor[0].Lang, Prefix(donor[0]), left2);
                            if (filler == null) throw new Exception("filler failed " + f);
                            Buffer.BlockCopy(filler, 0, b, donorAt, left2);
                        }
                        blocked.Add(run);
                        Console.Error.WriteLine(Path.GetFileName(f) + " t=" + thai[0].Key.Start + ": grew run by " + d + " bytes, shifted " + (hi - lo) + " bytes " + (delta > 0 ? "forward" : "back"));
                        changed += thai.Count; done += thai.Count;
                    }
                }
                if (changed > 0) { File.WriteAllBytes(Path.Combine(args[2], Path.GetFileName(f)), b); files++; }
            }
            Console.WriteLine("Movies written " + files + ", subtitles replaced " + done + ", untranslated " + miss + ", no room " + fail);
            return fail > 0 ? 2 : 0;
        }
        Console.WriteLine("usmextract <dir> <out.tsv> | usmscan <dir> | usminject <dir> <outDir> <tsv>");
        return 1;
    }
}
