import subprocess

code = r'''using System;
using System.IO;

class TexInspector
{
    static void Main(string[] args)
    {
        var p = Package.Load(args[0]);
        var bf = BmFontData.Load(p, int.Parse(args[1]));
        Console.WriteLine("Font " + Path.GetFileName(args[0]) + " chars=" + bf.Chars.Count + ", textures=" + bf.Textures.Count);
        foreach (int t in bf.Textures)
        {
            var tx = BmTexture.Load(p, t);
            Console.WriteLine(string.Format("  Tex exp={0}: {1}x{2}, format={3}, mipDataLen={4}", t, tx.SizeX, tx.SizeY, tx.Format, tx.MipDataLen));
        }
    }
}
'''

with open(r'C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools\inspect_tex.cs', 'w', encoding='utf-8') as f:
    f.write(code)

csc = r'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
src1 = r'C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools\source\UpkTool.cs'
src2 = r'C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools\source\BmFont.cs'
src3 = r'C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools\source\Font.cs'
src4 = r'C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools\source\Usm.cs'
src_main = r'C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools\inspect_tex.cs'
out_exe = r'C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools\inspect_tex.exe'

subprocess.run([csc, '/nologo', '/main:TexInspector', '/out:' + out_exe, src_main, src1, src2, src3, src4])
pc_startup = r'C:\program files (x86)\steam\steamapps\common\Batman Arkham Asylum GOTY\BmGame\CookedPC\Startup_INT.upk'
res = subprocess.run([out_exe, pc_startup, '7'], capture_output=True)
print('PC Startup Exp 7:')
print(res.stdout.decode('utf-8', errors='replace'))

sw_startup = r'C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\romfs\BmGame\CookedSwitch\Startup.xxx'
res2 = subprocess.run([out_exe, sw_startup, '6'], capture_output=True)
print('SW Startup Exp 6:')
print(res2.stdout.decode('utf-8', errors='replace'))
