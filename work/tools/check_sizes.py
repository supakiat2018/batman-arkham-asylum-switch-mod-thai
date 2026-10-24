import subprocess

code = """
using System;

class CheckSizes
{
    static void Main()
    {
        var pStart = Package.Load(@"C:\\Users\\Supakiat\\Downloads\\Compressed\\Batman Arkham Asylum\\romfs\\BmGame\\CookedSwitch\\Startup.xxx");
        Console.WriteLine("=== Startup.xxx ===");
        int[] sExps = { 4, 5, 6, 7, 8, 9, 20, 21 };
        foreach (int exp in sExps)
        {
            var bf = BmFontData.Load(pStart, exp);
            int hIdx = bf.Remap.ContainsKey('H') ? bf.Remap['H'] : -1;
            int hH = hIdx >= 0 ? bf.Chars[hIdx].H : 0;
            string name = pStart.Exports[exp - 1].Name;
            Console.WriteLine(string.Format("Exp {0} ({1}): chars={2}, 'H' height={3}", exp, name, bf.Chars.Count, hH));
        }

        var pCg = Package.Load(@"C:\\Users\\Supakiat\\Downloads\\Compressed\\Batman Arkham Asylum\\romfs\\BmGame\\CookedSwitch\\CommonGame_LOC_INT.xxx");
        Console.WriteLine("\\n=== CommonGame_LOC_INT.xxx ===");
        int[] cExps = { 1, 2, 3, 4 };
        foreach (int exp in cExps)
        {
            var bf = BmFontData.Load(pCg, exp);
            int hIdx = bf.Remap.ContainsKey('H') ? bf.Remap['H'] : -1;
            int hH = hIdx >= 0 ? bf.Chars[hIdx].H : 0;
            string name = pCg.Exports[exp - 1].Name;
            Console.WriteLine(string.Format("Exp {0} ({1}): chars={2}, 'H' height={3}", exp, name, bf.Chars.Count, hH));
        }
    }
}
"""

tools_dir = r"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools"
source_dir = rf"{tools_dir}\source"
src_cs = rf"{tools_dir}\check_sizes.cs"
csc = r"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
out_exe = rf"{tools_dir}\check_sizes.exe"

with open(src_cs, "w", encoding="utf-8") as f:
    f.write(code)

files = [
    rf"{source_dir}\UpkTool.cs",
    rf"{source_dir}\Font.cs",
    rf"{source_dir}\BmFont.cs",
    rf"{source_dir}\Usm.cs",
    src_cs
]

res = subprocess.run([csc, f"/out:{out_exe}", "/main:CheckSizes"] + files, capture_output=True, text=True)
if res.returncode != 0:
    print("CSC error:\n" + res.stdout + "\n" + res.stderr)
else:
    res2 = subprocess.run([out_exe], capture_output=True, text=True)
    print(res2.stdout)
