namespace FontStash.NET
{
    internal class FonsState
    {
        public int Align;
        public float Blur;
        public uint Colour;

        public int Font;
        public float Size;
        public float Spacing;

        public FonsState Copy()
        {
            return (FonsState)MemberwiseClone();
        }
    }
}