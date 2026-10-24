import subprocess

code = """
using System;

class CheckPlanValues
{
    static void Main()
    {
        var ttf = new Ttf(@"C:\\Users\\Supakiat\\Downloads\\Google_Sans\\static\\GoogleSans-Bold.ttf");
        Func<int, double[]> box = delegate(int code)
        {
            double[] r = { 1e9, -1e9, 1e9, -1e9 };
            foreach (var ct in ttf.Contours(ttf.Cmap[code])) foreach (var pt in ct)
            { r[0] = Math.Min(r[0], pt.X); r[1] = Math.Max(r[1], pt.X); r[2] = Math.Min(r[2], pt.Y); r[3] = Math.Max(r[3], pt.Y); }
            return r;
        };
        double em = ttf.UnitsPerEm;
        int UP = (int)Math.Round(box(0x0E37)[3] + 0.035 * em - box(0x0E48)[2]);
        int LEFT = (int)Math.Round(ttf.Advance(ttf.Cmap[0x0E1B]) * 230.0 / 1290.0);
        int DOWN = (int)Math.Round(box(0x0E38)[3] - box(0x0E0E)[2] + 0.033 * em);
        Console.WriteLine(string.Format("UnitsPerEm={0}, UP={1}, LEFT={2}, DOWN={3}", em, UP, LEFT, DOWN));
    }
}
"""

tools_dir = r"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools"
source_dir = rf"{tools_dir}\source"
src_cs = rf"{tools_dir}\check_plan.cs"
csc = r"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
out_exe = rf"{tools_dir}\check_plan.exe"

with open(src_cs, "w", encoding="utf-8") as f:
    f.write(code)

files = [
    rf"{source_dir}\Font.cs",
    src_cs
]

res = subprocess.run([csc, f"/out:{out_exe}", "/main:CheckPlanValues"] + files, capture_output=True, text=True)
if res.returncode != 0:
    print("CSC error:\n" + res.stdout + "\n" + res.stderr)
else:
    res2 = subprocess.run([out_exe], capture_output=True, text=True)
    print(res2.stdout)
