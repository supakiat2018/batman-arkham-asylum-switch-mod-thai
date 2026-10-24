using System;

class InspectVanillaPropsEnd
{
    static void Main()
    {
        var p = Package.Load(@"C:\Users\Supakiat\Downloads\Compressed\Batman Arkham Asylum\romfs\BmGame\CookedSwitch\Startup.xxx");
        var bf = BmFontData.Load(p, 6);
        Console.WriteLine(string.Format("bf.PropsEnd = {0}, SerialOffset = {1}, SerialSize = {2}, End = {3}",
            bf.PropsEnd, bf.E.SerialOffset, bf.E.SerialSize, bf.E.SerialOffset + bf.E.SerialSize));
        int rel = bf.PropsEnd - bf.E.SerialOffset;
        Console.WriteLine("PropsEnd relative to SerialOffset: " + rel);
        // Print 32 bytes before and after PropsEnd
        for (int i = bf.PropsEnd - 16; i < bf.PropsEnd + 32; i += 16)
        {
            string hex = "";
            for (int j = 0; j < 16; j++) hex += p.Data[i + j].ToString("X2") + " ";
            Console.WriteLine(string.Format("{0:X8}: {1}", i, hex));
        }
    }
}
