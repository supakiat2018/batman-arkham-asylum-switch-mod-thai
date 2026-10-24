using System;
using System.IO;

class VerifyFinal
{
    static void Main()
    {
        var p = Package.Load(@"C:\Users\Supakiat\AppData\Roaming\yuzu\load\0100E870163CA000\MODTHAI\romfs\BmGame\CookedSwitch\Startup.xxx");
        var bf = BmFontData.Load(p, 6);
        Console.WriteLine("=== Final Deployed Startup.xxx Exp 6 ===");
        Console.WriteLine("Kerning: " + bf.Kerning + ", Total Chars: " + bf.Chars.Count + ", Remap: " + bf.Remap.Count);

        // Dialogue line from Cutscene.int
        int[] dialogueCodes = {
            0x0E16, 0x0E2D, 0x0E22, 0x0E44, 0x0E1B, 0x20, // ถอยไป_
            0x0E44, 0x0E2D, 0xF70B, 0x0E42, 0x0E07, 0xF70A, 0x21, 0x20, // ไอ้โง่!_
            0x0E09, 0x0E31, 0x0E19, 0x0E21, 0x0E35, 0x0E23, 0x0E30, 0x0E40, 0x0E1A, 0x0E34, 0x0E14, 0x20, // ฉันมีระเบิด_
            0x0E2D, 0xF70B, 0x0E2D, 0x20, // อ้อ_
            0x0E08, 0x0E23, 0x0E34, 0x0E07, 0x0E2A, 0x0E34, 0x20, // จริงสิ_
            0x0E40, 0x0E2E, 0x0E30, 0x20, // เฮะ_
            0x0E40, 0x0E2E, 0x0E30, 0x2E, 0x2E, 0x2E, 0x21, 0x21, 0x21 // เฮะ...!!!
        };

        int penX = 0;
        int missing = 0;
        foreach (int code in dialogueCodes)
        {
            int idx;
            if (bf.Remap.TryGetValue(code, out idx))
            {
                var c = bf.Chars[idx];
                int net = c.W + bf.Kerning;
                penX += net;
            }
            else
            {
                Console.WriteLine("MISSING: 0x" + code.ToString("X4"));
                missing++;
            }
        }
        Console.WriteLine(string.Format("Dialogue line: total characters={0}, missing={1}, total line width={2}px",
            dialogueCodes.Length, missing, penX));
    }
}
