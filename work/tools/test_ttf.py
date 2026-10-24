import subprocess

tools_dir = r"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools"
source_dir = rf"{tools_dir}\source"
out_cs = rf"{tools_dir}\test_ttf.cs"
csc = r"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
out_exe = rf"{tools_dir}\test_ttf.exe"

code = """
using System;

class TestTtf
{
    static void Main()
    {
        var ttf = new Ttf(@"C:\\Users\\Supakiat\\Downloads\\Google_Sans\\static\\GoogleSans-Bold.ttf");
        Console.WriteLine("UnitsPerEm: " + ttf.UnitsPerEm + ", Cmap: " + ttf.Cmap.Count);
        int thaiCount = 0;
        for (int c = 0x0E01; c <= 0x0E5B; c++) if (ttf.Cmap.ContainsKey(c)) thaiCount++;
        Console.WriteLine("Thai characters found: " + thaiCount);
    }
}
"""

with open(out_cs, "w", encoding="utf-8") as f:
    f.write(code)

files = [rf"{source_dir}\Font.cs", out_cs]
res = subprocess.run([csc, f"/out:{out_exe}", "/main:TestTtf"] + files, capture_output=True, text=True)
if res.returncode != 0:
    print("CSC error:\n" + res.stdout + "\n" + res.stderr)
else:
    res2 = subprocess.run([out_exe], capture_output=True, text=True)
    print(res2.stdout)
