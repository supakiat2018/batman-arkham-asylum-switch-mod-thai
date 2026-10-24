
using System;

class CheckTtfCmap
{
    static void Main()
    {
        var ttf = new Ttf(@"C:\Users\Supakiat\Downloads\Google_Sans\static\GoogleSans-Bold.ttf");
        Console.WriteLine("Cmap has 0xF700: " + ttf.Cmap.ContainsKey(0xF700));
        Console.WriteLine("Cmap has 0xF70F: " + ttf.Cmap.ContainsKey(0xF70F));
        Console.WriteLine("Cmap has 0x0E10 (ฐ): " + ttf.Cmap.ContainsKey(0x0E10));
        Console.WriteLine("Cmap has 0x0E0D (ญ): " + ttf.Cmap.ContainsKey(0x0E0D));
    }
}
