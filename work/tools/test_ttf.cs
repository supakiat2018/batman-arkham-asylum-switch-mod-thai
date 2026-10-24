
using System;

class TestTtf
{
    static void Main()
    {
        var ttf = new Ttf(@"C:\Users\Supakiat\Downloads\Google_Sans\static\GoogleSans-Bold.ttf");
        Console.WriteLine("UnitsPerEm: " + ttf.UnitsPerEm + ", Cmap: " + ttf.Cmap.Count);
        int thaiCount = 0;
        for (int c = 0x0E01; c <= 0x0E5B; c++) if (ttf.Cmap.ContainsKey(c)) thaiCount++;
        Console.WriteLine("Thai characters found: " + thaiCount);
    }
}
