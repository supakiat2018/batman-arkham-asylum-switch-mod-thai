import subprocess

code = """
using System;

class CheckAllVanillaTex
{
    static void Main()
    {
        string vanillaPath = @"C:\\Users\\Supakiat\\Downloads\\Compressed\\Batman Arkham Asylum\\romfs\\BmGame\\CookedSwitch\\Startup.xxx";
        var p = Package.Load(vanillaPath);
        int[] exps = { 4, 5, 6, 7, 8, 9, 20, 21 };
        foreach (int exp in exps)
        {
            var bf = BmFontData.Load(p, exp);
            Console.WriteLine(string.Format("Font Exp {0}: Textures={1}", exp, bf.Textures.Count));
            for (int i = 0; i < bf.Textures.Count; i++)
            {
                var tx = BmTexture.Load(p, bf.Textures[i]);
                Console.WriteLine(string.Format("  Tex {0} (exp {1}): {2}x{3}, format={4}, MipDataLen={5}",
                    i, bf.Textures[i], tx.SizeX, tx.SizeY, tx.Format, tx.MipDataLen));
            }
        }
    }
}
"""

tools_dir = r"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools"
source_dir = rf"{tools_dir}\source"
src_cs = rf"{tools_dir}\check_all_vanilla_tex.cs"
csc = r"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
out_exe = rf"{tools_dir}\check_all_vanilla_tex.exe"

with open(src_cs, "w", encoding="utf-8") as f:
    f.write(code)

files = [
    rf"{source_dir}\UpkTool.cs",
    rf"{source_dir}\Font.cs",
    rf"{source_dir}\BmFont.cs",
    rf"{source_dir}\Usm.cs",
    src_cs
]

res = subprocess.run([csc, f"/out:{out_exe}", "/main:CheckAllVanillaTex"] + files, capture_output=True, text=True)
if res.returncode != 0:
    print("CSC error:\n" + res.stdout + "\n" + res.stderr)
else:
    res2 = subprocess.run([out_exe], capture_output=True, text=True)
    print(res2.stdout)
