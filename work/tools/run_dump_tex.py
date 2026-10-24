import subprocess

code = r'''using System;
using System.IO;

class TextureDumper
{
    static void Main(string[] args)
    {
        var p = Package.Load(args[0]);
        var bf = BmFontData.Load(p, int.Parse(args[1]));
        var tx = BmTexture.Load(p, bf.Textures[0]);
        var px = Dxt5.Decode(p.Data, tx.MipDataPos, tx.SizeX, tx.SizeY);
        using (var bmp = new System.Drawing.Bitmap(tx.SizeX, tx.SizeY))
        {
            for (int y = 0; y < tx.SizeY; y++)
                for (int x = 0; x < tx.SizeX; x++)
                {
                    uint v = px[y * tx.SizeX + x];
                    int a = (int)(v >> 24);
                    int r = (int)(v >> 16 & 255);
                    int g = (int)(v >> 8 & 255);
                    int b = (int)(v & 255);
                    bmp.SetPixel(x, y, System.Drawing.Color.FromArgb(a, r, g, b));
                }
            bmp.Save(args[2], System.Drawing.Imaging.ImageFormat.Png);
        }
        Console.WriteLine("Saved " + args[2]);
    }
}
'''

with open(r'C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools\dump_tex.cs', 'w', encoding='utf-8') as f:
    f.write(code)

csc = r'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
src1 = r'C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools\source\UpkTool.cs'
src2 = r'C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools\source\BmFont.cs'
src3 = r'C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools\source\Font.cs'
src4 = r'C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools\source\Usm.cs'
src_main = r'C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools\dump_tex.cs'
out_exe = r'C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools\dump_tex.exe'

subprocess.run([csc, '/nologo', '/main:TextureDumper', '/out:' + out_exe, src_main, src1, src2, src3, src4])
sw_startup = r'C:\Users\Supakiat\AppData\Roaming\yuzu\load\0100E870163CA000\MODTHAI\romfs\BmGame\CookedSwitch\Startup.xxx'
out_png = r'C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\f6_texture_atlas.png'
res = subprocess.run([out_exe, sw_startup, '6', out_png], capture_output=True)
print(res.stdout.decode('utf-8', errors='replace'))
