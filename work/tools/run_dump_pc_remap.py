import subprocess

code = r'''using System;
using System.IO;

class RemapDumper
{
    static void Main(string[] args)
    {
        var p = Package.Load(args[0]);
        var bf = BmFontData.Load(p, int.Parse(args[1]));
        foreach (var kv in bf.Remap)
        {
            if (kv.Key > 127 && (kv.Key < 0x0E00 || kv.Key > 0x0E7F))
            {
                Console.WriteLine(string.Format("Key U+{0:X4}: idx={1}", kv.Key, kv.Value));
            }
        }
    }
}
'''

with open(r'C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools\dump_pc_remap.cs', 'w', encoding='utf-8') as f:
    f.write(code)

csc = r'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
src1 = r'C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools\source\UpkTool.cs'
src2 = r'C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools\source\BmFont.cs'
src3 = r'C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools\source\Font.cs'
src4 = r'C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools\source\Usm.cs'
src_main = r'C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools\dump_pc_remap.cs'
out_exe = r'C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools\dump_pc_remap.exe'

subprocess.run([csc, '/nologo', '/main:RemapDumper', '/out:' + out_exe, src_main, src1, src2, src3, src4])
pc_startup = r'C:\program files (x86)\steam\steamapps\common\Batman Arkham Asylum GOTY\BmGame\CookedPC\Startup_INT.upk'
res = subprocess.run([out_exe, pc_startup, '7'], capture_output=True)
print(res.stdout.decode('utf-8', errors='replace'))
