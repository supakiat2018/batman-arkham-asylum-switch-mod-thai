import subprocess

code = r"""
using System;

class DumpPc
{
    static void Main()
    {
        string pcPkg = @"C:\program files (x86)\steam\steamapps\common\Batman Arkham Asylum GOTY\BmGame\CookedPC\Startup_INT.upk";
        var p = Package.Load(pcPkg);
        var bf = BmFontData.Load(p, 7);
        Console.WriteLine("PC Font 7 Kerning: " + bf.Kerning + ", Chars: " + bf.Chars.Count + ", Remap: " + bf.Remap.Count);
        Console.WriteLine("HasRemap: " + (bf.Remap.Count > 0));
        int cThai = 0, cPua = 0, cCp874 = 0;
        foreach (var kv in bf.Remap)
        {
            if (kv.Key >= 0x0E00 && kv.Key <= 0x0E7F) cThai++;
            else if (kv.Key >= 0xF700 && kv.Key <= 0xF71A) cPua++;
            else if (kv.Key >= 0x80 && kv.Key <= 0xFF) cCp874++;
        }
        Console.WriteLine(string.Format("Thai: {0}, PUA: {1}, CP874: {2}", cThai, cPua, cCp874));
        Console.WriteLine("Sample Remap keys:");
        int printed = 0;
        foreach (var kv in bf.Remap)
        {
            if (kv.Key > 127)
            {
                Console.WriteLine(string.Format("  0x{0:X4} -> {1}", kv.Key, kv.Value));
                if (++printed > 25) break;
            }
        }
    }
}
"""

tools_dir = r"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools"
source_dir = rf"{tools_dir}\source"
src_cs = rf"{tools_dir}\dump_pc.cs"
csc = r"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
out_exe = rf"{tools_dir}\dump_pc.exe"

with open(src_cs, "w", encoding="utf-8") as f:
    f.write(code)

files = [
    rf"{source_dir}\UpkTool.cs",
    rf"{source_dir}\Font.cs",
    rf"{source_dir}\BmFont.cs",
    rf"{source_dir}\Usm.cs",
    src_cs
]

res = subprocess.run([csc, f"/out:{out_exe}", "/main:DumpPc"] + files, capture_output=True, text=True)
if res.returncode != 0:
    print("CSC error:\n" + res.stdout + "\n" + res.stderr)
else:
    res2 = subprocess.run([out_exe], capture_output=True, text=True)
    print(res2.stdout)
