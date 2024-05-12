namespace FontStash.NET
{
    public struct FonsTextIter
    {
        public float X, Y, Nextx, Nexty, Scale, Spacing;
        public uint Codepoint;
        public short Isize, Iblur;
        public FonsFont Font;
        public int PrevGlyphIndex;
        public string Str;
        public string Next;
        public string End;
        public uint Utf8State;
        public FonsGlyphBitmap BitmapOption;
    }
}