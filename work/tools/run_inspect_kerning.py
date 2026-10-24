import subprocess

code = r'''using System;
using System.IO;

class KerningInspector
{
    static void Main(string[] args)
    {
        var p = Package.Load(args[0]);
        var bf = BmFontData.Load(p, int.Parse(args[1]));
        Console.WriteLine(Path.GetFileName(args[0]) + " exp " + args[1] + ": Kerning=" + bf.Kerning + ", HasKerning=" + bf.HasKerning);
        for (int i = 0; i < 5; i++)
        {
            var c = bf.Chars[i];
            Console.WriteLine(string.Format("  char {0}: U={1}, V={2}, W={3}, H={4}, VOff={5}", i, c.U, c.V, c.W, c.H, c.VOff));
        }
    }
}
'''

with open(r'C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools\inspect_kerning.cs', 'w', encoding='utf-8') as f:
    f.write(code)

csc = r'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
src1 = r'C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools\source\UpkTool.cs'
src2 = r'C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools\source\BmFont.cs'
src3 = r'C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools\source\Font.cs'
src4 = r'C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools\source\Usm.cs'
src_main = r'C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools\inspect_kerning.cs'
out_exe = r'C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools\inspect_kerning.exe'

subprocess.run([csc, '/nologo', '/main:KerningInspector', '/out:' + out_exe, src_main, src1, src2, src3, src4])
pc_startup = r'C:\program files (x86)\steam\steamapps\common\Batman Arkham Asylum GOTY\BmGame\CookedPC\Startup_INT.upk'
res1 = subprocess.run([out_exe, pc_startup, '7'], capture_output=True)
print(res1.stdout.decode('utf-8', errors='replace'))

sw_startup = r'C:\Users\Supakiat\AppData\Roaming\yuzu\load\0100E870163CA000\MODTHAI\romfs\BmGame\CookedSwitch\Startup.xxx'
for exp in ['6', '8', '20']:
    res2 = subprocess.run([out_exe, sw_startup, exp], capture_output=True)
    print(res2.stdout.decode('utf-8', errors='replace'))
