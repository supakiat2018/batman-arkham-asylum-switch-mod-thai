import subprocess

code = r'''using System;
using System.IO;
using System.Text;

class DumpMap
{
    static void Main()
    {
        Environment.SetEnvironmentVariable("ASYLUM_CP", "874");
        foreach (var kv in Cp1252Thai.Map)
        {
            if (kv.Key >= 0xF700 && kv.Key <= 0xF71A)
            {
                Console.WriteLine(string.Format("U+{0:X4} -> U+{1:X4}", (int)kv.Key, (int)kv.Value));
            }
        }
    }
}
'''

with open(r'C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools\dump_map874.cs', 'w', encoding='utf-8') as f:
    f.write(code)

csc = r'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
src1 = r'C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools\source\UpkTool.cs'
src2 = r'C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools\source\BmFont.cs'
src3 = r'C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools\source\Font.cs'
src4 = r'C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools\source\Usm.cs'
src_main = r'C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools\dump_map874.cs'
out_exe = r'C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools\dump_map874.exe'

subprocess.run([csc, '/nologo', '/main:DumpMap', '/out:' + out_exe, src_main, src1, src2, src3, src4])
res = subprocess.run([out_exe], capture_output=True)
print(res.stdout.decode('utf-8', errors='replace'))
