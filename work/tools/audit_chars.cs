using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

class AuditCharacters
{
    static void Main()
    {
        string modDir = @"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\modthai\0100E870163CA000\romfs\BmGame\CookedSwitch";
        
        var cgPkg = Package.Load(Path.Combine(modDir, "CommonGame_LOC_INT.xxx"));
        var cg3 = BmFontData.Load(cgPkg, 3);
        
        var startPkg = Package.Load(Path.Combine(modDir, "Startup.xxx"));
        var st6 = BmFontData.Load(startPkg, 6);

        var allChars = new HashSet<char>();
        var thaiChars = new HashSet<char>();
        var puaChars = new HashSet<char>();
        var otherChars = new HashSet<char>();

        var missingInCg = new HashSet<char>();
        var missingInSt = new HashSet<char>();

        string[] files = Directory.GetFiles(modDir, "*_LOC_INT.xxx");
        Console.WriteLine(string.Format("Scanning {0} LOC_INT packages for subtitles...", files.Length));

        int totalSubtitles = 0;
        foreach (string fpath in files)
        {
            var p = Package.Load(fpath);
            byte[] d = p.Data;

            for (int i = 0; i < d.Length - 8; i++)
            {
                int len = BitConverter.ToInt32(d, i);
                if (len < -2 && len > -2000)
                {
                    int charCount = -len;
                    int byteLen = charCount * 2;
                    if (i + 4 + byteLen <= d.Length)
                    {
                        bool hasThai = false;
                        for (int k = 0; k < charCount; k++)
                        {
                            ushort ch = BitConverter.ToUInt16(d, i + 4 + k * 2);
                            if ((ch >= 0x0E00 && ch <= 0x0E7F) || (ch >= 0xF700 && ch <= 0xF71A))
                            {
                                hasThai = true;
                                break;
                            }
                        }

                        if (hasThai)
                        {
                            totalSubtitles++;
                            string s = Encoding.Unicode.GetString(d, i + 4, byteLen - 2);
                            foreach (char c in s)
                            {
                                allChars.Add(c);
                                ushort code = (ushort)c;
                                if (code >= 0x0E00 && code <= 0x0E7F) thaiChars.Add(c);
                                else if (code >= 0xF700 && code <= 0xF71A) puaChars.Add(c);
                                else if (code > 127) otherChars.Add(c);

                                if (!cg3.Remap.ContainsKey(code)) missingInCg.Add(c);
                                if (!st6.Remap.ContainsKey(code)) missingInSt.Add(c);
                            }
                        }
                    }
                }
            }
        }

        Console.WriteLine(string.Format("Total Thai subtitle strings found: {0}", totalSubtitles));
        Console.WriteLine(string.Format("Total distinct characters used: {0}", allChars.Count));
        Console.WriteLine(string.Format("  - Thai Unicode chars: {0}", thaiChars.Count));
        Console.WriteLine(string.Format("  - Thai PUA chars: {0}", puaChars.Count));
        Console.WriteLine(string.Format("  - Other non-ASCII chars: {0}", otherChars.Count));
        
        Console.WriteLine("\n=== Audit: Missing in CommonGame_LOC_INT.xxx (Exp 3) ===");
        if (missingInCg.Count == 0)
        {
            Console.WriteLine("PERFECT! 0 missing characters in CommonGame_LOC_INT.xxx!");
        }
        else
        {
            Console.WriteLine(string.Format("WARNING! Missing {0} characters in CommonGame:", missingInCg.Count));
            foreach (char c in missingInCg)
            {
                Console.WriteLine(string.Format("   '{0}' U+{1:X4} ({2})", c, (int)c, (int)c));
            }
        }

        Console.WriteLine("\n=== Audit: Missing in Startup.xxx (Exp 6) ===");
        if (missingInSt.Count == 0)
        {
            Console.WriteLine("PERFECT! 0 missing characters in Startup.xxx!");
        }
        else
        {
            Console.WriteLine(string.Format("WARNING! Missing {0} characters in Startup:", missingInSt.Count));
            foreach (char c in missingInSt)
            {
                Console.WriteLine(string.Format("   '{0}' U+{1:X4} ({2})", c, (int)c, (int)c));
            }
        }

        Console.WriteLine("\nDistinct PUA characters used in dialogues:");
        foreach (char c in puaChars)
        {
            Console.WriteLine(string.Format("   PUA U+{0:X4}: CG Remap -> Cell {1}", (int)c, cg3.Remap.ContainsKey(c) ? cg3.Remap[c].ToString() : "MISSING"));
        }
    }
}
