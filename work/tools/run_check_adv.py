import subprocess

code = r'''using System;
using System.IO;
using System.Linq;

class CheckAdv
{
    static void Main()
    {
        var ttf = new Ttf(@"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\lora-bold.ttf");
        var p = Package.Load(@"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\romfs\BmGame\CookedSwitch\Startup.xxx");
        var bf = BmFontData.Load(p, 6);
        var hc = bf.Chars[bf.Remap['H']];
        double thaiPx = hc.H - 2;
        double s = thaiPx / ttf.Contours(ttf.Cmap[0x0E01]).ConvertAll(ct => ct.ConvertAll(q => q.Y).Max()).Max();
        var plan = ThaiRaster.Plan(ttf);
        double left = 0;
        foreach (var pl in plan) left = Math.Max(left, ThaiRaster.LeftInk(ttf, ttf.Cmap[pl[1]], s, pl[2]));
        int pad = (int)Math.Ceiling(left) + 2;
        Console.WriteLine("thaiPx=" + thaiPx + ", s=" + s + ", pad=" + pad);
        foreach (char ch in new[] { 'ก', 'า', 'ร', 'บ', 'เ', 'ฮ', '!' })
        {
            int g = ttf.Cmap[ch];
            int adv = (int)Math.Round(ttf.Advance(g) * s);
            Console.WriteLine(string.Format("Char '{0}': adv={1}, W={2}, advance in UE3={3}", ch, adv, pad + adv, (pad + adv) - pad));
        }
    }
}
'''

with open(r'C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools\check_adv.cs', 'w', encoding='utf-8') as f:
    f.write(code)

csc = r'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
src1 = r'C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools\source\UpkTool.cs'
src2 = r'C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools\source\BmFont.cs'
src3 = r'C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools\source\Font.cs'
src4 = r'C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools\source\Usm.cs'
src_main = r'C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools\check_adv.cs'
out_exe = r'C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools\check_adv.exe'

subprocess.run([csc, '/nologo', '/main:CheckAdv', '/out:' + out_exe, src_main, src1, src2, src3, src4])
res = subprocess.run([out_exe], capture_output=True)
print(repr(res.stdout))
