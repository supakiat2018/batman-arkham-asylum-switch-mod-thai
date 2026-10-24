
using System;

class InspectIsRemappedProp
{
    static void Main()
    {
        var p = Package.Load(@"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\romfs\BmGame\CookedSwitch\Startup.xxx");
        var e = p.Exports[10 - 1]; // Exp 10: MediumFont_JPN
        Subs.Walk(p, e.SerialOffset + 4, e.SerialOffset + e.SerialSize, delegate(int t, string n, int sp, int v, int s)
        {
            if (n == "IsRemapped")
            {
                // Print the raw tag bytes
                int tagStart = sp - 16;
                int tagLen = (v + s) - tagStart;
                Console.WriteLine("Tag Start=" + tagStart + ", Len=" + tagLen);
                for (int i = 0; i < tagLen; i++)
                {
                    Console.Write(p.Data[tagStart + i].ToString("X2") + " ");
                }
                Console.WriteLine();
                // Names:
                int nameIdx = BitConverter.ToInt32(p.Data, tagStart);
                int typeIdx = BitConverter.ToInt32(p.Data, tagStart + 8);
                Console.WriteLine(string.Format("PropName: {0}, TypeName: {1}, Size: {2}, Val: {3}",
                    p.Names[nameIdx], p.Names[typeIdx], s, BitConverter.ToInt32(p.Data, v)));
            }
        });
    }
}
