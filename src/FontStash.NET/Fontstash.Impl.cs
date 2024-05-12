using System;

namespace FontStash.NET
{
    public partial class FontManager
    {
        private const int Aprec = 16;
        private const int Zprec = 7;

        private void AddWhiteRect(int w, int h)
        {
            int gx = 0, gy = 0;
            if (!atlas.AddRect(w, h, ref gx, ref gy))
            {
                return;
            }

            var index = gx + gy * @params.Width;
            for (var y = 0; y < h; y++)
            {
                for (var x = 0; x < w; x++) texData[index + x] = 0xff;
                index += @params.Width;
            }

            dirtyRect[0] = Math.Min(dirtyRect[0], gx);
            dirtyRect[1] = Math.Min(dirtyRect[1], gy);
            dirtyRect[2] = Math.Max(dirtyRect[2], gx + w);
            dirtyRect[3] = Math.Max(dirtyRect[3], gy + h);
        }

        private FonsState GetState()
        {
            if (states[nstates - 1] == null)
            {
                states[nstates - 1] = new FonsState();
            }

            return states[nstates - 1];
        }

        private int AllocFont()
        {
            if (nfonts + 1 > cfonts)
            {
                cfonts = cfonts == 0 ? 8 : cfonts * 2;
                Array.Resize(ref fonts, cfonts);
            }

            FonsFont font = new();
            font.Glyphs = new FonsGlyph[InitGlyphs];
            font.Cglyphs = InitGlyphs;
            font.Nglyphs = 0;

            fonts[nfonts++] = font;
            return nfonts - 1;
        }

        private void BlurCols(int index, int w, int h, int dstStride, int alpha)
        {
            for (var y = 0; y < h; y++)
            {
                var z = 0;
                for (var x = 1; x < w; x++)
                {
                    z += (alpha * ((texData[index + x] << Zprec) - z)) >> Aprec;
                    texData[index + x] = (byte)(z >> Zprec);
                }

                texData[index + (w - 1)] = 0;
                z = 0;
                for (var x = w - 2; x >= 0; x--)
                {
                    z += (alpha * ((texData[index + x] << Zprec) - z)) >> Aprec;
                    texData[index + x] = (byte)(z >> Zprec);
                }

                texData[index + 0] = 0;
                index += dstStride;
            }
        }

        private void BlurRows(int index, int w, int h, int dstStride, int alpha)
        {
            for (var x = 0; x < w; x++)
            {
                var z = 0;
                for (var y = dstStride; y < h * dstStride; y += dstStride)
                {
                    z += (alpha * ((texData[index + y] << Zprec) - z)) >> Aprec;
                    texData[index + y] = (byte)(z >> Zprec);
                }

                texData[index + (h - 1) * dstStride] = 0;
                z = 0;
                for (var y = (h - 2) * dstStride; y >= 0; y -= dstStride)
                {
                    z += (alpha * ((texData[index + y] << Zprec) - z)) >> Aprec;
                    texData[index + y] = (byte)(z >> Zprec);
                }

                texData[index + 0] = 0;
                index++;
            }
        }

        private void Blur(int index, int w, int h, int dstStride, int blur)
        {
            if (blur < 1)
            {
                return;
            }

            var sigma = blur * 0.57735f; // 0.57735 =~= 1 / Sqrt(3)
            var alpha = (int)((1 << Aprec) * (1.0f - MathF.Exp(-2.3f / (sigma + 1.0f))));
            BlurRows(index, w, h, dstStride, alpha);
            BlurCols(index, w, h, dstStride, alpha);
            BlurRows(index, w, h, dstStride, alpha);
            BlurCols(index, w, h, dstStride, alpha);
        }

        private FonsGlyph GetGlyph(FonsFont font, uint codepoint, short isize, short iblur,
            FonsGlyphBitmap bitmapOption)
        {
            int gx = 0, gy = 0;
            FonsGlyph glyph = null;
            var size = isize / 10.0f;
            var renderFont = font;

            if (isize < 2)
            {
                return null;
            }

            if (iblur > 20)
            {
                iblur = 20;
            }

            var pad = iblur + 2;

            nscratch = 0;

            var h = Utils.HashInt(codepoint) & (HashLutSize - 1);
            var i = font.Lut[h];
            while (i != -1)
            {
                if (font.Glyphs[i].Codepoint == codepoint && font.Glyphs[i].Size == isize &&
                    font.Glyphs[i].Blur == iblur)
                {
                    glyph = font.Glyphs[i];
                    if (bitmapOption == FonsGlyphBitmap.Optional || (glyph.X0 >= 0 && glyph.Y0 >= 0))
                    {
                        return glyph;
                    }

                    break;
                }

                i = font.Glyphs[i].Next;
            }

            var g = FonsTt.GetGlyphIndex(font.Font, (int)codepoint);
            if (g == 0)
            {
                for (i = 0; i < font.Nfallbacks; i++)
                {
                    var fallbackFont = fonts[font.Fallbacks[i]];
                    var fallbackIndex = FonsTt.GetGlyphIndex(fallbackFont.Font, (int)codepoint);
                    if (fallbackIndex != 0)
                    {
                        g = fallbackIndex;
                        renderFont = fallbackFont;
                        break;
                    }
                }
            }

            var scale = FonsTt.GetPixelHeightScale(renderFont.Font, size);
            FonsTt.BuildGlyphBitmap(renderFont.Font, g, scale, out var advance, out var lsb, out var x0, out var y0,
                out var x1, out var y1);
            var gw = x1 - x0 + pad * 2;
            var gh = y1 - y0 + pad * 2;

            if (bitmapOption == FonsGlyphBitmap.Required)
            {
                var added = atlas.AddRect(gw, gh, ref gx, ref gy);
                if (!added)
                {
                    handleError?.Invoke(FonsErrorCode.AtlasFull, 0);
                    added = atlas.AddRect(gw, gh, ref gx, ref gy);
                }

                if (added == false)
                {
                    return null;
                }
            }
            else
            {
                gx = Invalid;
                gy = Invalid;
            }

            // Init glyph
            if (glyph == null)
            {
                glyph = font.AllocGlyph();
                glyph.Codepoint = codepoint;
                glyph.Size = isize;
                glyph.Blur = iblur;
                glyph.Next = 0;

                glyph.Next = font.Lut[h];
                font.Lut[h] = font.Nglyphs - 1;
            }

            glyph.Index = g;
            glyph.X0 = (short)gx;
            glyph.Y0 = (short)gy;
            glyph.X1 = (short)(glyph.X0 + gw);
            glyph.Y1 = (short)(glyph.Y0 + gh);
            glyph.Xadv = (short)(scale * advance * 10.0f);
            glyph.Xoff = (short)(x0 - pad);
            glyph.Yoff = (short)(y0 - pad);

            if (bitmapOption == FonsGlyphBitmap.Optional)
            {
                return glyph;
            }

            // rasterize
            var index = glyph.X0 + pad + (glyph.Y0 + pad) * @params.Width;
            FonsTt.RenderGlyphBitmap(renderFont.Font, texData, index, gw - pad * 2, gh - pad * 2, @params.Width, scale,
                scale, g);

            // Ensure border pixel
            index = glyph.X0 + glyph.Y0 * @params.Width;
            for (var y = 0; y < gh; y++)
            {
                texData[index + y * @params.Width] = 0;
                texData[index + (gw - 1) + y * @params.Width] = 0;
            }

            for (var x = 0; x < gw; x++)
            {
                texData[index + x] = 0;
                texData[index + (gh - 1) * @params.Width] = 0;
            }

            if (iblur > 0)
            {
                nscratch = 0;
                index = glyph.X0 + glyph.Y0 * @params.Width;
                Blur(index, gw, gh, @params.Width, iblur);
            }

            dirtyRect[0] = Math.Min(dirtyRect[0], glyph.X0);
            dirtyRect[1] = Math.Min(dirtyRect[1], glyph.Y0);
            dirtyRect[2] = Math.Max(dirtyRect[2], glyph.X1);
            dirtyRect[3] = Math.Max(dirtyRect[3], glyph.Y1);

            return glyph;
        }

        private FonsQuad GetQuad(FonsFont font, int prevGlyphIndex, FonsGlyph glyph, float scale, float spacing,
            ref float x, ref float y)
        {
            FonsQuad q = new();

            if (prevGlyphIndex != Invalid)
            {
                var adv = FonsTt.GetGlyphKernAdvance(font.Font, prevGlyphIndex, glyph.Index) * scale;
                x += (int)(adv + spacing + 0.5f);
            }

            float xoff = (short)(glyph.Xoff + 1);
            float yoff = (short)(glyph.Yoff + 1);
            float x0 = (short)(glyph.X0 + 1);
            float y0 = (short)(glyph.Y0 + 1);
            float x1 = (short)(glyph.X1 - 1);
            float y1 = (short)(glyph.Y1 - 1);

            float rx, ry;
            if ((@params.Flags & (byte)FontFlags.ZeroTopLeft) != 0)
            {
                rx = MathF.Floor(x + xoff);
                ry = MathF.Floor(y + yoff);

                q.X0 = rx;
                q.Y0 = ry;
                q.X1 = rx + x1 - x0;
                q.Y1 = ry + y1 - y0;

                q.S0 = x0 * itw;
                q.T0 = y0 * ith;
                q.S1 = x1 * itw;
                q.T1 = y1 * ith;
            }
            else
            {
                rx = MathF.Floor(x + xoff);
                ry = MathF.Floor(y - yoff);

                q.X0 = rx;
                q.Y0 = ry;
                q.X1 = rx + x1 - x0;
                q.Y1 = ry - y1 + y0;

                q.S0 = x0 * itw;
                q.T0 = y0 * ith;
                q.S1 = x1 * itw;
                q.T1 = y1 * ith;
            }

            x += (int)(glyph.Xadv / 10.0f + 0.5f);
            return q;
        }

        private void Flush()
        {
            if (dirtyRect[0] < dirtyRect[2] && dirtyRect[1] < dirtyRect[3])
            {
                @params.RenderUpdate?.Invoke(dirtyRect, texData);
                dirtyRect[0] = @params.Width;
                dirtyRect[1] = @params.Height;
                dirtyRect[2] = 0;
                dirtyRect[3] = 0;
            }

            if (nverts > 0)
            {
                @params.RenderDraw?.Invoke(verts, tcoords, colours, nverts);
                nverts = 0;
            }
        }

        private void Vertex(float x, float y, float s, float t, uint c)
        {
            verts[nverts * 2 + 0] = x;
            verts[nverts * 2 + 1] = y;
            tcoords[nverts * 2 + 0] = s;
            tcoords[nverts * 2 + 1] = t;
            colours[nverts] = c;
            nverts++;
        }

        private float GetVertAlign(FonsFont font, int align, short isize)
        {
            if ((@params.Flags & (uint)FontFlags.ZeroTopLeft) != 0)
            {
                if ((align & (uint)FonsAlign.Top) != 0)
                {
                    return font.Ascender * isize / 10.0f;
                }

                if ((align & (uint)FonsAlign.Middle) != 0)
                {
                    return (font.Ascender + font.Descender) / 2.0f * isize / 10.0f;
                }

                if ((align & (uint)FonsAlign.Baseline) != 0)
                {
                    return 0.0f;
                }

                if ((align & (uint)FonsAlign.Bottom) != 0)
                {
                    return font.Descender * isize / 10.0f;
                }
            }
            else
            {
                if ((align & (uint)FonsAlign.Top) != 0)
                {
                    return -font.Ascender * isize / 10.0f;
                }

                if ((align & (uint)FonsAlign.Middle) != 0)
                {
                    return -(font.Ascender + font.Descender) / 2.0f * isize / 10.0f;
                }

                if ((align & (uint)FonsAlign.Baseline) != 0)
                {
                    return 0.0f;
                }

                if ((align & (uint)FonsAlign.Bottom) != 0)
                {
                    return -font.Descender * isize / 10.0f;
                }
            }

            return 0.0f;
        }
    }
}