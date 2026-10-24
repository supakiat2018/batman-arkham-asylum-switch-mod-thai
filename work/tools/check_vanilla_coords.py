import subprocess

code = """
using System;

class CheckVanillaCoords
{
    static void Main()
    {
        string vanillaPath = @"C:\\Users\\Supakiat\\Downloads\\Compressed\\Batman Arkham Asylum\\romfs\\BmGame\\CookedSwitch\\Startup.xxx";
        var p = Package.Load(vanillaPath);
        var bf = BmFontData.Load(p, 6);
        foreach (char ch in "ABCDEFGHIJ01234")
        {
            int idx = bf.Remap[ch];
            var c = bf.Chars[idx];
            Console.WriteLine(string.Format("Char '{0}': Tex={1}, U={2}, V={3}, W={4}, H={5}, VOff={6}",
                ch, c.Tex, c.U, c.V, c.W, c.H, c.VOff));
        }
    }
}
"""

tools_dir = r"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools"
source_dir = rf"{tools_dir}\source"
src_cs = rf"{tools_dir}\check_vanilla_coords.cs"
csc = r"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
out_exe = rf"{tools_dir}\check_vanilla_coords.exe"

with open(src_cs, "w", encoding="utf-8") as f:
    f.write(code)

files = [
    rf"{source_dir}\UpkTool.cs",
    rf"{source_dir}\Font.cs",
    rf"{source_dir}\BmFont.cs",
    rf"{source_dir}\Usm.cs",
    src_cs
]

res = subprocess.run([csc, f"/out:{out_exe}", "/main:CheckVanillaCoords"] + files, capture_output=True, text=True)
if res.returncode != 0:
    print("CSC error:\n" + res.stdout + "\n" + res.stderr)
else:
    res2 = subprocess.run([out_exe], capture_output=True, text=True)
    print(res2.stdout)
