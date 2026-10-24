using System;
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
