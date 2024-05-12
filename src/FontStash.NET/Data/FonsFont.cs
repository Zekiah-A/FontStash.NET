using System;

namespace FontStash.NET
{
    public class FonsFont
    {
        public float Ascender;
        public int Cglyphs;
        public byte[] Data;
        public int DataSize;
        public float Descender;
        public int[] Fallbacks = new int[FontManager.MaxFallbacks];

        public FonsTtImpl Font;
        public byte FreeData;
        public FonsGlyph[] Glyphs;
        public float Lineh;
        public int[] Lut = new int[FontManager.HashLutSize];
        public string Name;
        public int Nfallbacks;
        public int Nglyphs;

        public FonsFont()
        {
            Font = new FonsTtImpl();
        }

        public FonsGlyph AllocGlyph()
        {
            if (Nglyphs + 1 > Cglyphs)
            {
                Cglyphs = Cglyphs == 0 ? 8 : Cglyphs * 2;
                Array.Resize(ref Glyphs, Cglyphs);
            }

            FonsGlyph glyph = new();
            Glyphs[Nglyphs] = glyph;
            Nglyphs++;
            return Glyphs[Nglyphs - 1];
        }
    }
}