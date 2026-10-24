
using System;

class CheckDialogue
{
    static void Main()
    {
        string p = @"C:\Users\Supakiat\AppData\Roaming\yuzu\load\0100E870163CA000\MODTHAI\romfs\BmGame\CookedSwitch\Admin_B4_LOC_INT.xxx";
        var pkg = Package.Load(p);
        byte[] d = pkg.Data;
        // Search for '!' in unicode or ascii
        // Let's search for "Batman" or Thai chars
        for (int i = 0; i < d.Length - 10; i++)
        {
            // check for 'ถ' (0x0E16)
            if (d[i] == 0x16 && d[i+1] == 0x0E)
            {
                Console.WriteLine("Found Thai UTF-16 at offset " + i);
                // print 40 chars
                for (int j = 0; j < 40; j++)
                {
                    ushort ch = BitConverter.ToUInt16(d, i + j * 2);
                    Console.Write(string.Format("U+{0:X4} ", ch));
                }
                Console.WriteLine();
                break;
            }
        }
    }
}
