import subprocess

code = """
using System;

class CheckTexExp
{
    static void Main()
    {
        string vanillaPath = @"C:\\Users\\Supakiat\\Downloads\\Compressed\\Batman Arkham Asylum\\romfs\\BmGame\\CookedSwitch\\Startup.xxx";
        var p = Package.Load(vanillaPath);
        var bf = BmFontData.Load(p, 6);
        Console.WriteLine("Font 6 Tex 0 export index: " + bf.Textures[0]);
        var tx = BmTexture.Load(p, bf.Textures[0]);
        Console.WriteLine(string.Format("SizeX={0}, SizeY={1}, Format={2}, NativeStart={3}, MipDataPos={4}, MipDataLen={5}",
            tx.SizeX, tx.SizeY, tx.Format, tx.NativeStart, tx.MipDataPos, tx.MipDataLen));
    }
}
"""

tools_dir = r"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools"
source_dir = rf"{tools_dir}\source"
src_cs = rf"{tools_dir}\check_tex_exp.cs"
csc = r"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
out_exe = rf"{tools_dir}\check_tex_exp.exe"

with open(src_cs, "w", encoding="utf-8") as f:
    f.write(code)

files = [
    rf"{source_dir}\UpkTool.cs",
    rf"{source_dir}\Font.cs",
    rf"{source_dir}\BmFont.cs",
    rf"{source_dir}\Usm.cs",
    src_cs
]

res = subprocess.run([csc, f"/out:{out_exe}", "/main:CheckTexExp"] + files, capture_output=True, text=True)
if res.returncode != 0:
    print("CSC error:\n" + res.stdout + "\n" + res.stderr)
else:
    res2 = subprocess.run([out_exe], capture_output=True, text=True)
    print(res2.stdout)
