import subprocess

sw_startup = r'C:\Users\Supakiat\AppData\Roaming\yuzu\load\0100E870163CA000\MODTHAI\romfs\BmGame\CookedSwitch\Startup.xxx'

code = r'''using System;
using System.IO;

class DetailInspector
{
    static void Main(string[] args)
    {
        var p = Package.Load(args[0]);
        var bf = BmFontData.Load(p, int.Parse(args[1]));
        Console.WriteLine("Chars: " + bf.Chars.Count + ", Remap: " + bf.Remap.Count);
        char[] testChars = { '!', 'X', 'P', 'เ', 'ฮ', 'ช', 'า', 'ร', 'ป', (char)0xF702, (char)0xF70B, (char)0xF70E, (char)0xF714 };
        foreach (char c in testChars)
        {
            int idx = -1;
            bool found = bf.Remap.TryGetValue(c, out idx);
            if (found)
            {
                var fc = bf.Chars[idx];
                Console.WriteLine(string.Format("Char U+{0:X4}: idx={1}, U={2}, V={3}, W={4}, H={5}, Tex={6}, VOff={7}", (int)c, idx, fc.U, fc.V, fc.W, fc.H, fc.Tex, fc.VOff));
            }
            else
            {
                Console.WriteLine(string.Format("Char U+{0:X4}: NOT FOUND", (int)c));
            }
        }
    }
}
'''

with open(r'C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools\inspect_detail.cs', 'w', encoding='utf-8') as f:
    f.write(code)

csc = r'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
src1 = r'C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools\source\UpkTool.cs'
src2 = r'C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools\source\BmFont.cs'
src3 = r'C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools\source\Font.cs'
src4 = r'C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools\source\Usm.cs'
src_main = r'C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools\inspect_detail.cs'
out_exe = r'C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\work\tools\inspect_detail.exe'

subprocess.run([csc, '/nologo', '/main:DetailInspector', '/out:' + out_exe, src_main, src1, src2, src3, src4])
res = subprocess.run([out_exe, sw_startup, '20'], capture_output=True)
print(res.stdout.decode('utf-8', errors='replace'))
