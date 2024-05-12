using System;

namespace StbTrueTypeSharp
{
    public class PackContext
    {
        public int Height;
        public uint HOversample;
        public StbrpContext PackInfo;
        public int Padding;
        public FakePtr<byte> Pixels;
        public int SkipMissing;
        public int StrideInBytes;
        public uint VOversample;
        public int Width;

        public int stbtt_PackBegin(byte[] pixels, int pw, int ph, int strideInBytes, int padding)
        {
            var context = new StbrpContext(pw - padding, ph - padding);

            Width = pw;
            Height = ph;
            Pixels = new FakePtr<byte>(pixels);
            PackInfo = context;
            Padding = padding;
            StrideInBytes = strideInBytes != 0 ? strideInBytes : pw;
            HOversample = 1;
            VOversample = 1;
            SkipMissing = 0;
            if (pixels != null)
            {
                Array.Clear(pixels, 0, pw * ph);
            }

            return 1;
        }

        public void stbtt_PackSetOversampling(uint hOversample, uint vOversample)
        {
            if (hOversample <= 8)
            {
                HOversample = hOversample;
            }

            if (vOversample <= 8)
            {
                VOversample = vOversample;
            }
        }

        public void stbtt_PackSetSkipMissingCodepoints(int skip)
        {
            SkipMissing = skip;
        }

        public int stbtt_PackFontRangesGatherRects(FontInfo info,
            FakePtr<StbttPackRange> ranges, int numRanges, StbrpRect[] rects)
        {
            var i = 0;
            var j = 0;
            var k = 0;
            var missingGlyphAdded = 0;
            k = 0;
            for (i = 0; i < numRanges; ++i)
            {
                var fh = ranges[i].FontSize;
                var scale = fh > 0 ? info.stbtt_ScaleForPixelHeight(fh) : info.stbtt_ScaleForMappingEmToPixels(-fh);
                ranges[i].HOversample = (byte)HOversample;
                ranges[i].VOversample = (byte)VOversample;
                for (j = 0; j < ranges[i].NumChars; ++j)
                {
                    var x0 = 0;
                    var y0 = 0;
                    var x1 = 0;
                    var y1 = 0;
                    var codepoint = ranges[i].ArrayOfUnicodeCodepoints == null
                        ? ranges[i].FirstUnicodeCodepointInRange + j
                        : ranges[i].ArrayOfUnicodeCodepoints[j];
                    var glyph = info.stbtt_FindGlyphIndex(codepoint);
                    if (glyph == 0 && (SkipMissing != 0 || missingGlyphAdded != 0))
                    {
                        rects[k].W = rects[k].H = 0;
                    }
                    else
                    {
                        info.stbtt_GetGlyphBitmapBoxSubpixel(glyph, scale * HOversample, scale * VOversample,
                            0, 0, ref x0, ref y0, ref x1, ref y1);
                        rects[k].W = (int)(x1 - x0 + Padding + HOversample - 1);
                        rects[k].H = (int)(y1 - y0 + Padding + VOversample - 1);
                        if (glyph == 0)
                        {
                            missingGlyphAdded = 1;
                        }
                    }

                    ++k;
                }
            }

            return k;
        }

        public int stbtt_PackFontRangesRenderIntoRects(FontInfo info,
            FakePtr<StbttPackRange> ranges, int numRanges, StbrpRect[] rects)
        {
            var i = 0;
            var j = 0;
            var k = 0;
            var missingGlyph = -1;
            var returnValue = 1;
            var oldHOver = (int)HOversample;
            var oldVOver = (int)VOversample;
            k = 0;
            for (i = 0; i < numRanges; ++i)
            {
                var fh = ranges[i].FontSize;
                var scale = fh > 0 ? info.stbtt_ScaleForPixelHeight(fh) : info.stbtt_ScaleForMappingEmToPixels(-fh);
                float recipH = 0;
                float recipV = 0;
                float subX = 0;
                float subY = 0;
                HOversample = ranges[i].HOversample;
                VOversample = ranges[i].VOversample;
                recipH = 1.0f / HOversample;
                recipV = 1.0f / VOversample;
                subX = Common.stbtt__oversample_shift((int)HOversample);
                subY = Common.stbtt__oversample_shift((int)VOversample);
                for (j = 0; j < ranges[i].NumChars; ++j)
                {
                    var r = rects[k];
                    if (r.WasPacked != 0 && r.W != 0 && r.H != 0)
                    {
                        var bc = ranges[i].ChardataForRange[j];
                        var advance = 0;
                        var lsb = 0;
                        var x0 = 0;
                        var y0 = 0;
                        var x1 = 0;
                        var y1 = 0;
                        var codepoint = ranges[i].ArrayOfUnicodeCodepoints == null
                            ? ranges[i].FirstUnicodeCodepointInRange + j
                            : ranges[i].ArrayOfUnicodeCodepoints[j];
                        var glyph = info.stbtt_FindGlyphIndex(codepoint);
                        var pad = Padding;
                        r.X += pad;
                        r.Y += pad;
                        r.W -= pad;
                        r.H -= pad;
                        info.stbtt_GetGlyphHMetrics(glyph, ref advance, ref lsb);
                        info.stbtt_GetGlyphBitmapBox(glyph, scale * HOversample, scale * VOversample, ref x0,
                            ref y0, ref x1, ref y1);
                        info.stbtt_MakeGlyphBitmapSubpixel(Pixels + r.X + r.Y * StrideInBytes,
                            (int)(r.W - HOversample + 1), (int)(r.H - VOversample + 1), StrideInBytes,
                            scale * HOversample, scale * VOversample, 0, 0, glyph);
                        if (HOversample > 1)
                        {
                            Common.stbtt__h_prefilter(Pixels + r.X + r.Y * StrideInBytes, r.W, r.H,
                                StrideInBytes, HOversample);
                        }

                        if (VOversample > 1)
                        {
                            Common.stbtt__v_prefilter(Pixels + r.X + r.Y * StrideInBytes, r.W, r.H,
                                StrideInBytes, VOversample);
                        }

                        bc.X0 = (ushort)(short)r.X;
                        bc.Y0 = (ushort)(short)r.Y;
                        bc.X1 = (ushort)(short)(r.X + r.W);
                        bc.Y1 = (ushort)(short)(r.Y + r.H);
                        bc.Xadvance = scale * advance;
                        bc.Xoff = x0 * recipH + subX;
                        bc.Yoff = y0 * recipV + subY;
                        bc.Xoff2 = (x0 + r.W) * recipH + subX;
                        bc.Yoff2 = (y0 + r.H) * recipV + subY;
                        if (glyph == 0)
                        {
                            missingGlyph = j;
                        }
                    }
                    else if (SkipMissing != 0)
                    {
                        returnValue = 0;
                    }
                    else if (r.WasPacked != 0 && r.W == 0 && r.H == 0 && missingGlyph >= 0)
                    {
                        ranges[i].ChardataForRange[j] = ranges[i].ChardataForRange[missingGlyph];
                    }
                    else
                    {
                        returnValue = 0;
                    }

                    ++k;
                }
            }

            HOversample = (uint)oldHOver;
            VOversample = (uint)oldVOver;
            return returnValue;
        }

        public void stbtt_PackFontRangesPackRects(StbrpRect[] rects, int numRects)
        {
            PackInfo.stbrp_pack_rects(rects, numRects);
        }

        public int stbtt_PackFontRanges(byte[] fontdata, int fontIndex,
            FakePtr<StbttPackRange> ranges, int numRanges)
        {
            var info = new FontInfo();
            var i = 0;
            var j = 0;
            var n = 0;
            var returnValue = 1;
            for (i = 0; i < numRanges; ++i)
            for (j = 0; j < ranges[i].NumChars; ++j)
                ranges[i].ChardataForRange[j].X0 = ranges[i].ChardataForRange[j].Y0 =
                    ranges[i].ChardataForRange[j].X1 = ranges[i].ChardataForRange[j].Y1 = 0;
            n = 0;
            for (i = 0; i < numRanges; ++i)
                n += ranges[i].NumChars;
            var rects = new StbrpRect[n];
            for (i = 0; i < rects.Length; ++i)
                rects[i] = new StbrpRect();
            if (rects == null)
            {
                return 0;
            }

            info.stbtt_InitFont(fontdata, Common.stbtt_GetFontOffsetForIndex(fontdata, fontIndex));
            n = stbtt_PackFontRangesGatherRects(info, ranges, numRanges, rects);
            stbtt_PackFontRangesPackRects(rects, n);
            returnValue = stbtt_PackFontRangesRenderIntoRects(info, ranges, numRanges, rects);
            return returnValue;
        }

        public int stbtt_PackFontRange(byte[] fontdata, int fontIndex, float fontSize,
            int firstUnicodeCodepointInRange, int numCharsInRange, StbttPackedchar[] chardataForRange)
        {
            var range = new StbttPackRange();
            range.FirstUnicodeCodepointInRange = firstUnicodeCodepointInRange;
            range.ArrayOfUnicodeCodepoints = null;
            range.NumChars = numCharsInRange;
            range.ChardataForRange = chardataForRange;
            range.FontSize = fontSize;

            var ranges = new FakePtr<StbttPackRange>(range);
            return stbtt_PackFontRanges(fontdata, fontIndex, ranges, 1);
        }

        public class StbrpContext
        {
            public int BottomY;
            public int Height;
            public int Width;
            public int X;
            public int Y;

            public StbrpContext(int pw, int ph)
            {
                Width = pw;
                Height = ph;
                X = 0;
                Y = 0;
                BottomY = 0;
            }

            public void stbrp_pack_rects(StbrpRect[] rects, int numRects)
            {
                var i = 0;
                for (i = 0; i < numRects; ++i)
                {
                    if (X + rects[i].W > Width)
                    {
                        X = 0;
                        Y = BottomY;
                    }

                    if (Y + rects[i].H > Height)
                    {
                        break;
                    }

                    rects[i].X = X;
                    rects[i].Y = Y;
                    rects[i].WasPacked = 1;
                    X += rects[i].W;
                    if (Y + rects[i].H > BottomY)
                    {
                        BottomY = Y + rects[i].H;
                    }
                }

                for (; i < numRects; ++i)
                    rects[i].WasPacked = 0;
            }
        }

        public class StbrpRect
        {
            public int H;
            public int Id;
            public int W;
            public int WasPacked;
            public int X;
            public int Y;
        }

        public class StbttPackedchar
        {
            public ushort X0;
            public ushort X1;
            public float Xadvance;
            public float Xoff;
            public float Xoff2;
            public ushort Y0;
            public ushort Y1;
            public float Yoff;
            public float Yoff2;
        }

        public class StbttPackRange
        {
            public int[] ArrayOfUnicodeCodepoints;
            public StbttPackedchar[] ChardataForRange;
            public int FirstUnicodeCodepointInRange;
            public float FontSize;
            public byte HOversample;
            public int NumChars;
            public byte VOversample;
        }
    }
}