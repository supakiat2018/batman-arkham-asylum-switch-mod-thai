import subprocess

code = """
using System;

class CheckLower
{
    static void Main()
    {
        var p = Package.Load(@"C:\\Users\\Supakiat\\AppData\\Roaming\\yuzu\\load\\0100E870163CA000\\MODTHAI\\romfs\\BmGame\\CookedSwitch\\Startup.xxx");
        var bf = BmFontData.Load(p, 6);
        foreach (char ch in "abcdefghijklmnopqrstuvwxyz")
        {
            int idx;
            if (bf.Remap.TryGetValue(ch, out idx))
            {
                var c = bf.Chars[idx];
                if (c.W <= 0 || c.H <= 0)
                    Console.WriteLine(string.Format("Char '{0}' has zero size: W={1}, H={2}", ch, c.W, c.H));
            }
            else
            {
                Console.WriteLine(string.Format("Char '{0}' NOT IN REMAP!", ch));
            }
        }
        Console.WriteLine("Check finished.");
    }
}
"""

tools_dir = r"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools"
source_dir = rf"{tools_dir}\source"
src_cs = rf"{tools_dir}\check_lower.cs"
csc = r"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
out_exe = rf"{tools_dir}\check_lower.exe"

with open(src_cs, "w", encoding="utf-8") as f:
    f.write(code)

files = [
    rf"{source_dir}\UpkTool.cs",
    rf"{source_dir}\Font.cs",
    rf"{source_dir}\BmFont.cs",
    rf"{source_dir}\Usm.cs",
    rf"{tools_dir}\Tegra.cs",
    src_cs
]

res = subprocess.run([csc, f"/out:{out_exe}", "/main:CheckLower"] + files, capture_output=True, text=True)
if res.returncode != 0:
    print("CSC error:\n" + res.stdout + "\n" + res.stderr)
else:
    res2 = subprocess.run([out_exe], capture_output=True, text=True)
    print(res2.stdout)
