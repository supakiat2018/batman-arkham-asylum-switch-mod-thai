import subprocess

code = r'''using System;
using System.IO;

class VanillaInspector
{
    static void Main(string[] args)
    {
        var p = Package.Load(args[0]);
        var bf = BmFontData.Load(p, int.Parse(args[1]));
        Console.WriteLine(Path.GetFileName(args[0]) + " exp " + args[1] + ": Chars=" + bf.Chars.Count + ", Textures=" + bf.Textures.Count + ", Kerning=" + bf.Kerning);
        foreach (int t in bf.Textures)
        {
            var tx = BmTexture.Load(p, t);
            Console.WriteLine(string.Format("  Tex {0}: {1}x{2}, format={3}, mipDataLen={4}", t, tx.SizeX, tx.SizeY, tx.Format, tx.MipDataLen));
        }
    }
}
'''

with open(r'C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools\inspect_vanilla.cs', 'w', encoding='utf-8') as f:
    f.write(code)

csc = r'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
src1 = r'C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools\source\UpkTool.cs'
src2 = r'C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools\source\BmFont.cs'
src3 = r'C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools\source\Font.cs'
src4 = r'C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools\source\Usm.cs'
src_main = r'C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools\inspect_vanilla.cs'
out_exe = r'C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools\inspect_vanilla.exe'

subprocess.run([csc, '/nologo', '/main:VanillaInspector', '/out:' + out_exe, src_main, src1, src2, src3, src4])
vanilla_startup = r'C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\romfs\BmGame\CookedSwitch\Startup.xxx'
for exp in ['4', '5', '6', '7', '8', '20']:
    res = subprocess.run([out_exe, vanilla_startup, exp], capture_output=True)
    print(res.stdout.decode('utf-8', errors='replace'))
