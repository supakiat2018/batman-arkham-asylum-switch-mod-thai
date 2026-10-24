
using System;

class CheckPlan
{
    static void Main()
    {
        var ttf = new Ttf(@"C:\Users\Supakiat\Downloads\Google_Sans\static\GoogleSans-Bold.ttf");
        var plan = ThaiRaster.Plan(ttf);
        Console.WriteLine("Plan count: " + plan.Count);
        foreach (var pl in plan)
        {
            if (pl[0] >= 0x0E01 && pl[0] <= 0x0E17)
            {
                Console.WriteLine(string.Format("Code 0x{0:X4} ({1}) -> glyph {2}", pl[0], (char)pl[0], pl[1]));
            }
        }
    }
}
