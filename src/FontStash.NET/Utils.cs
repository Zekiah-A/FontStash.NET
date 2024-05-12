namespace FontStash.NET;

internal static class Utils
{
    public static uint HashInt(uint a)
    {
        a += ~(a << 15);
        a ^= a >> 10;
        a += a << 3;
        a ^= a >> 6;
        a += ~(a << 11);
        a ^= a >> 16;
        return a;
    }
    
    public static uint RgbaToUint(byte r, byte g, byte b, byte a)
    {
        return (uint)(r | (g << 8) | (b << 16) | (a << 24));
    }
}
