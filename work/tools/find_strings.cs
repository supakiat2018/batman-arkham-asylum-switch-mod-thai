
using System;
using System.Text;

class FindStrings
{
    static void Main()
    {
        string p = @"C:\Users\Supakiat\AppData\Roaming\yuzu\load\0100E870163CA000\MODTHAI\romfs\BmGame\CookedSwitch\Admin_B4_LOC_INT.xxx";
        var pkg = Package.Load(p);
        byte[] d = pkg.Data;

        // Search for "Batman" or "แบทแมน"
        for (int i = 0; i < d.Length - 20; i++)
        {
            // In FString: length (4 bytes)
            int len = BitConverter.ToInt32(d, i);
            if (len < -5 && len > -200) // Unicode FString has negative length!
            {
                int charCount = -len;
                int byteLen = charCount * 2;
                if (i + 4 + byteLen <= d.Length)
                {
                    string s = Encoding.Unicode.GetString(d, i + 4, byteLen - 2);
                    if (s.Contains("แบทแมน") || s.Contains("!") || s.Contains("ถอย"))
                    {
                        Console.WriteLine("Found Unicode String at " + i + ": " + s);
                        Console.Write("Chars: ");
                        foreach (char c in s)
                        {
                            Console.Write(string.Format("'{0}'(U+{1:X4}) ", c, (int)c));
                        }
                        Console.WriteLine();
                    }
                }
            }
        }
    }
}
