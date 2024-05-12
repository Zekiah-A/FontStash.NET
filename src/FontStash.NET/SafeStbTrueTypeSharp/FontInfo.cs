using System;
using static StbTrueTypeSharp.Common;

namespace StbTrueTypeSharp
{
    public class FontInfo
    {
        public Buf Cff;
        public Buf Charstrings;
        public FakePtr<byte> Data;
        public Buf Fdselect;
        public Buf Fontdicts;
        public int Fontstart;
        public int Glyf;
        public int Gpos;
        public Buf Gsubrs;
        public int Head;
        public int Hhea;
        public int Hmtx;
        public int IndexMap;
        public int IndexToLocFormat;
        public int Kern;
        public int Loca;
        public int NumGlyphs;
        public Buf Subrs;
        public int Svg;

        public int stbtt__get_svg()
        {
            uint t = 0;
            if (Svg < 0)
            {
                t = stbtt__find_table(Data, (uint)Fontstart, "SVG ");
                if (t != 0)
                {
                    var offset = TtUlong(Data + t + 2);
                    Svg = (int)(t + offset);
                }
                else
                {
                    Svg = 0;
                }
            }

            return Svg;
        }

        public int stbtt_InitFont_internal(byte[] data, int fontstart)
        {
            uint cmap = 0;
            uint t = 0;
            var i = 0;
            var numTables = 0;
            var ptr = new FakePtr<byte>(data);
            Data = ptr;
            Fontstart = fontstart;
            Cff = new Buf(FakePtr<byte>.Null, 0);
            cmap = stbtt__find_table(ptr, (uint)fontstart, "cmap");
            Loca = (int)stbtt__find_table(ptr, (uint)fontstart, "loca");
            Head = (int)stbtt__find_table(ptr, (uint)fontstart, "head");
            Glyf = (int)stbtt__find_table(ptr, (uint)fontstart, "glyf");
            Hhea = (int)stbtt__find_table(ptr, (uint)fontstart, "hhea");
            Hmtx = (int)stbtt__find_table(ptr, (uint)fontstart, "hmtx");
            Kern = (int)stbtt__find_table(ptr, (uint)fontstart, "kern");
            Gpos = (int)stbtt__find_table(ptr, (uint)fontstart, "GPOS");
            if (cmap == 0 || Head == 0 || Hhea == 0 || Hmtx == 0)
            {
                return 0;
            }

            if (Glyf != 0)
            {
                if (Loca == 0)
                {
                    return 0;
                }
            }
            else
            {
                Buf b = null;
                Buf topdict = null;
                Buf topdictidx = null;
                var cstype = (uint)2;
                var charstrings = (uint)0;
                var fdarrayoff = (uint)0;
                var fdselectoff = (uint)0;
                uint cff = 0;
                cff = stbtt__find_table(ptr, (uint)fontstart, "CFF ");
                if (cff == 0)
                {
                    return 0;
                }

                Fontdicts = new Buf(FakePtr<byte>.Null, 0);
                Fdselect = new Buf(FakePtr<byte>.Null, 0);
                Cff = new Buf(new FakePtr<byte>(ptr, (int)cff), 512 * 1024 * 1024);
                b = Cff;
                b.stbtt__buf_skip(2);
                b.stbtt__buf_seek(b.stbtt__buf_get8());
                b.stbtt__cff_get_index();
                topdictidx = b.stbtt__cff_get_index();
                topdict = topdictidx.stbtt__cff_index_get(0);
                b.stbtt__cff_get_index();
                Gsubrs = b.stbtt__cff_get_index();
                topdict.stbtt__dict_get_ints(17, out charstrings);
                topdict.stbtt__dict_get_ints(0x100 | 6, out cstype);
                topdict.stbtt__dict_get_ints(0x100 | 36, out fdarrayoff);
                topdict.stbtt__dict_get_ints(0x100 | 37, out fdselectoff);
                Subrs = Buf.stbtt__get_subrs(b, topdict);

                if (cstype != 2)
                {
                    return 0;
                }

                if (charstrings == 0)
                {
                    return 0;
                }

                if (fdarrayoff != 0)
                {
                    if (fdselectoff == 0)
                    {
                        return 0;
                    }

                    b.stbtt__buf_seek((int)fdarrayoff);
                    Fontdicts = b.stbtt__cff_get_index();
                    Fdselect = b.stbtt__buf_range((int)fdselectoff, (int)(b.Size - fdselectoff));
                }

                b.stbtt__buf_seek((int)charstrings);
                Charstrings = b.stbtt__cff_get_index();
            }

            t = stbtt__find_table(ptr, (uint)fontstart, "maxp");
            if (t != 0)
            {
                NumGlyphs = TtUshort(ptr + t + 4);
            }
            else
            {
                NumGlyphs = 0xffff;
            }

            Svg = -1;
            numTables = TtUshort(ptr + cmap + 2);
            IndexMap = 0;
            for (i = 0; i < numTables; ++i)
            {
                var encodingRecord = (uint)(cmap + 4 + 8 * i);
                switch (TtUshort(ptr + encodingRecord))
                {
                    case StbttPlatformIdMicrosoft:
                        switch (TtUshort(ptr + encodingRecord + 2))
                        {
                            case StbttMsEidUnicodeBmp:
                            case StbttMsEidUnicodeFull:
                                IndexMap = (int)(cmap + TtUlong(ptr + encodingRecord + 4));
                                break;
                        }

                        break;
                    case StbttPlatformIdUnicode:
                        IndexMap = (int)(cmap + TtUlong(ptr + encodingRecord + 4));
                        break;
                }
            }

            if (IndexMap == 0)
            {
                return 0;
            }

            IndexToLocFormat = TtUshort(ptr + Head + 50);
            return 1;
        }

        public int stbtt_FindGlyphIndex(int unicodeCodepoint)
        {
            var data = Data;
            var indexMap = (uint)IndexMap;
            var format = TtUshort(data + indexMap + 0);
            if (format == 0)
            {
                var bytes = (int)TtUshort(data + indexMap + 2);
                if (unicodeCodepoint < bytes - 6)
                {
                    return data[indexMap + 6 + unicodeCodepoint];
                }

                return 0;
            }

            if (format == 6)
            {
                var first = (uint)TtUshort(data + indexMap + 6);
                var count = (uint)TtUshort(data + indexMap + 8);
                if ((uint)unicodeCodepoint >= first && (uint)unicodeCodepoint < first + count)
                {
                    return TtUshort(data + indexMap + 10 + (unicodeCodepoint - first) * 2);
                }

                return 0;
            }

            if (format == 2)
            {
                return 0;
            }

            if (format == 4)
            {
                var segcount = (ushort)(TtUshort(data + indexMap + 6) >> 1);
                var searchRange = (ushort)(TtUshort(data + indexMap + 8) >> 1);
                var entrySelector = TtUshort(data + indexMap + 10);
                var rangeShift = (ushort)(TtUshort(data + indexMap + 12) >> 1);
                var endCount = indexMap + 14;
                var search = endCount;
                if (unicodeCodepoint > 0xffff)
                {
                    return 0;
                }

                if (unicodeCodepoint >= TtUshort(data + search + rangeShift * 2))
                {
                    search += (uint)(rangeShift * 2);
                }

                search -= 2;
                while (entrySelector != 0)
                {
                    ushort end = 0;
                    searchRange >>= 1;
                    end = TtUshort(data + search + searchRange * 2);
                    if (unicodeCodepoint > end)
                    {
                        search += (uint)(searchRange * 2);
                    }

                    --entrySelector;
                }

                search += 2;
                {
                    ushort offset = 0;
                    ushort start = 0;
                    var item = (ushort)((search - endCount) >> 1);
                    start = TtUshort(data + indexMap + 14 + segcount * 2 + 2 + 2 * item);
                    if (unicodeCodepoint < start)
                    {
                        return 0;
                    }

                    offset = TtUshort(data + indexMap + 14 + segcount * 6 + 2 + 2 * item);
                    if (offset == 0)
                    {
                        return (ushort)(unicodeCodepoint +
                                        TtShort(data + indexMap + 14 + segcount * 4 + 2 + 2 * item));
                    }

                    return TtUshort(data + offset + (unicodeCodepoint - start) * 2 + indexMap + 14 + segcount * 6 +
                                    2 + 2 * item);
                }
            }

            if (format == 12 || format == 13)
            {
                var ngroups = TtUlong(data + indexMap + 12);
                var low = 0;
                var high = 0;
                low = 0;
                high = (int)ngroups;
                while (low < high)
                {
                    var mid = low + ((high - low) >> 1);
                    var startChar = TtUlong(data + indexMap + 16 + mid * 12);
                    var endChar = TtUlong(data + indexMap + 16 + mid * 12 + 4);
                    if ((uint)unicodeCodepoint < startChar)
                    {
                        high = mid;
                    }
                    else if ((uint)unicodeCodepoint > endChar)
                    {
                        low = mid + 1;
                    }
                    else
                    {
                        var startGlyph = TtUlong(data + indexMap + 16 + mid * 12 + 8);
                        if (format == 12)
                        {
                            return (int)(startGlyph + unicodeCodepoint - startChar);
                        }

                        return (int)startGlyph;
                    }
                }

                return 0;
            }

            return 0;
        }

        public int stbtt_GetCodepointShape(int unicodeCodepoint, out StbttVertex[] vertices)
        {
            return stbtt_GetGlyphShape(stbtt_FindGlyphIndex(unicodeCodepoint), out vertices);
        }

        public int stbtt__GetGlyfOffset(int glyphIndex)
        {
            var g1 = 0;
            var g2 = 0;
            if (glyphIndex >= NumGlyphs)
            {
                return -1;
            }

            if (IndexToLocFormat >= 2)
            {
                return -1;
            }

            if (IndexToLocFormat == 0)
            {
                g1 = Glyf + TtUshort(Data + Loca + glyphIndex * 2) * 2;
                g2 = Glyf + TtUshort(Data + Loca + glyphIndex * 2 + 2) * 2;
            }
            else
            {
                g1 = (int)(Glyf + TtUlong(Data + Loca + glyphIndex * 4));
                g2 = (int)(Glyf + TtUlong(Data + Loca + glyphIndex * 4 + 4));
            }

            return g1 == g2 ? -1 : g1;
        }

        public int stbtt_GetGlyphBox(int glyphIndex, ref int x0, ref int y0, ref int x1,
            ref int y1)
        {
            if (Cff.Size != 0)
            {
                stbtt__GetGlyphInfoT2(glyphIndex, ref x0, ref y0, ref x1, ref y1);
            }
            else
            {
                var g = stbtt__GetGlyfOffset(glyphIndex);
                if (g < 0)
                {
                    return 0;
                }

                x0 = TtShort(Data + g + 2);
                y0 = TtShort(Data + g + 4);
                x1 = TtShort(Data + g + 6);
                y1 = TtShort(Data + g + 8);
            }

            return 1;
        }

        public int stbtt_GetCodepointBox(int codepoint, ref int x0, ref int y0, ref int x1,
            ref int y1)
        {
            return stbtt_GetGlyphBox(stbtt_FindGlyphIndex(codepoint), ref x0, ref y0, ref x1, ref y1);
        }

        public int stbtt_IsGlyphEmpty(int glyphIndex)
        {
            short numberOfContours = 0;
            var g = 0;

            int x0 = 0, y0 = 0, x1 = 0, y1 = 0;
            if (Cff.Size != 0)
            {
                return stbtt__GetGlyphInfoT2(glyphIndex, ref x0, ref y0, ref x1, ref y1) == 0 ? 1 : 0;
            }

            g = stbtt__GetGlyfOffset(glyphIndex);
            if (g < 0)
            {
                return 1;
            }

            numberOfContours = TtShort(Data + g);
            return numberOfContours == 0 ? 1 : 0;
        }

        public int stbtt__GetGlyphShapeTT(int glyphIndex, out StbttVertex[] pvertices)
        {
            short numberOfContours = 0;
            FakePtr<byte> endPtsOfContours;
            var data = Data;
            StbttVertex[] vertices = null;
            var numVertices = 0;
            var g = stbtt__GetGlyfOffset(glyphIndex);
            pvertices = null;
            if (g < 0)
            {
                return 0;
            }

            numberOfContours = TtShort(data + g);
            if (numberOfContours > 0)
            {
                var flags = (byte)0;
                byte flagcount = 0;
                var ins = 0;
                var i = 0;
                var j = 0;
                var m = 0;
                var n = 0;
                var nextMove = 0;
                var wasOff = 0;
                var off = 0;
                var startOff = 0;
                var x = 0;
                var y = 0;
                var cx = 0;
                var cy = 0;
                var sx = 0;
                var sy = 0;
                var scx = 0;
                var scy = 0;
                endPtsOfContours = data + g + 10;
                ins = TtUshort(data + g + 10 + numberOfContours * 2);
                var points = data + g + 10 + numberOfContours * 2 + 2 + ins;
                n = 1 + TtUshort(endPtsOfContours + numberOfContours * 2 - 2);
                m = n + 2 * numberOfContours;
                vertices = new StbttVertex[m];
                nextMove = 0;
                flagcount = 0;
                off = m - n;
                for (i = 0; i < n; ++i)
                {
                    if (flagcount == 0)
                    {
                        flags = points.GetAndIncrease();
                        if ((flags & 8) != 0)
                        {
                            flagcount = points.GetAndIncrease();
                        }
                    }
                    else
                    {
                        --flagcount;
                    }

                    vertices[off + i].type = flags;
                }

                x = 0;
                for (i = 0; i < n; ++i)
                {
                    flags = vertices[off + i].type;
                    if ((flags & 2) != 0)
                    {
                        var dx = (short)points.GetAndIncrease();
                        x += (flags & 16) != 0 ? dx : -dx;
                    }
                    else
                    {
                        if ((flags & 16) == 0)
                        {
                            x = x + (short)(points[0] * 256 + points[1]);
                            points += 2;
                        }
                    }

                    vertices[off + i].x = (short)x;
                }

                y = 0;
                for (i = 0; i < n; ++i)
                {
                    flags = vertices[off + i].type;
                    if ((flags & 4) != 0)
                    {
                        var dy = (short)points.GetAndIncrease();
                        y += (flags & 32) != 0 ? dy : -dy;
                    }
                    else
                    {
                        if ((flags & 32) == 0)
                        {
                            y = y + (short)(points[0] * 256 + points[1]);
                            points += 2;
                        }
                    }

                    vertices[off + i].y = (short)y;
                }

                numVertices = 0;
                sx = sy = cx = cy = scx = scy = 0;
                for (i = 0; i < n; ++i)
                {
                    flags = vertices[off + i].type;
                    x = vertices[off + i].x;
                    y = vertices[off + i].y;
                    if (nextMove == i)
                    {
                        if (i != 0)
                        {
                            numVertices = stbtt__close_shape(vertices, numVertices, wasOff, startOff, sx, sy, scx,
                                scy, cx, cy);
                        }

                        startOff = (flags & 1) != 0 ? 0 : 1;
                        if (startOff != 0)
                        {
                            scx = x;
                            scy = y;
                            if ((vertices[off + i + 1].type & 1) == 0)
                            {
                                sx = (x + vertices[off + i + 1].x) >> 1;
                                sy = (y + vertices[off + i + 1].y) >> 1;
                            }
                            else
                            {
                                sx = vertices[off + i + 1].x;
                                sy = vertices[off + i + 1].y;
                                ++i;
                            }
                        }
                        else
                        {
                            sx = x;
                            sy = y;
                        }

                        stbtt_setvertex(ref vertices[numVertices++], StbttVmove, sx, sy, 0, 0);
                        wasOff = 0;
                        nextMove = 1 + TtUshort(endPtsOfContours + j * 2);
                        ++j;
                    }
                    else
                    {
                        if ((flags & 1) == 0)
                        {
                            if (wasOff != 0)
                            {
                                stbtt_setvertex(ref vertices[numVertices++], StbttVcurve, (cx + x) >> 1,
                                    (cy + y) >> 1, cx, cy);
                            }

                            cx = x;
                            cy = y;
                            wasOff = 1;
                        }
                        else
                        {
                            if (wasOff != 0)
                            {
                                stbtt_setvertex(ref vertices[numVertices++], StbttVcurve, x, y, cx, cy);
                            }
                            else
                            {
                                stbtt_setvertex(ref vertices[numVertices++], StbttVline, x, y, 0, 0);
                            }

                            wasOff = 0;
                        }
                    }
                }

                numVertices = stbtt__close_shape(vertices, numVertices, wasOff, startOff, sx, sy, scx, scy, cx, cy);
            }
            else if (numberOfContours < 0)
            {
                var more = 1;
                var comp = data + g + 10;
                numVertices = 0;
                vertices = null;
                while (more != 0)
                {
                    ushort flags = 0;
                    ushort gidx = 0;
                    var compNumVerts = 0;
                    var i = 0;
                    StbttVertex[] compVerts;
                    StbttVertex[] tmp;
                    var mtx = new float[6];
                    mtx[0] = 1;
                    mtx[1] = 0;
                    mtx[2] = 0;
                    mtx[3] = 1;
                    mtx[4] = 0;
                    mtx[5] = 0;
                    float m = 0;
                    float n = 0;
                    flags = (ushort)TtShort(comp);
                    comp += 2;
                    gidx = (ushort)TtShort(comp);
                    comp += 2;
                    if ((flags & 2) != 0)
                    {
                        if ((flags & 1) != 0)
                        {
                            mtx[4] = TtShort(comp);
                            comp += 2;
                            mtx[5] = TtShort(comp);
                            comp += 2;
                        }
                        else
                        {
                            mtx[4] = (sbyte)comp.Value;
                            comp += 1;
                            mtx[5] = (sbyte)comp.Value;
                            comp += 1;
                        }
                    }

                    if ((flags & (1 << 3)) != 0)
                    {
                        mtx[0] = mtx[3] = TtShort(comp) / 16384.0f;
                        comp += 2;
                        mtx[1] = mtx[2] = 0;
                    }
                    else if ((flags & (1 << 6)) != 0)
                    {
                        mtx[0] = TtShort(comp) / 16384.0f;
                        comp += 2;
                        mtx[1] = mtx[2] = 0;
                        mtx[3] = TtShort(comp) / 16384.0f;
                        comp += 2;
                    }
                    else if ((flags & (1 << 7)) != 0)
                    {
                        mtx[0] = TtShort(comp) / 16384.0f;
                        comp += 2;
                        mtx[1] = TtShort(comp) / 16384.0f;
                        comp += 2;
                        mtx[2] = TtShort(comp) / 16384.0f;
                        comp += 2;
                        mtx[3] = TtShort(comp) / 16384.0f;
                        comp += 2;
                    }

                    m = (float)Math.Sqrt(mtx[0] * mtx[0] + mtx[1] * mtx[1]);
                    n = (float)Math.Sqrt(mtx[2] * mtx[2] + mtx[3] * mtx[3]);
                    compNumVerts = stbtt_GetGlyphShape(gidx, out compVerts);
                    if (compNumVerts > 0)
                    {
                        for (i = 0; i < compNumVerts; ++i)
                        {
                            short x = 0;
                            short y = 0;
                            x = compVerts[i].x;
                            y = compVerts[i].y;
                            compVerts[i].x = (short)(m * (mtx[0] * x + mtx[2] * y + mtx[4]));
                            compVerts[i].y = (short)(n * (mtx[1] * x + mtx[3] * y + mtx[5]));
                            x = compVerts[i].cx;
                            y = compVerts[i].cy;
                            compVerts[i].cx = (short)(m * (mtx[0] * x + mtx[2] * y + mtx[4]));
                            compVerts[i].cy = (short)(n * (mtx[1] * x + mtx[3] * y + mtx[5]));
                        }

                        tmp = new StbttVertex[numVertices + compNumVerts];
                        if (numVertices > 0)
                        {
                            Array.Copy(vertices, tmp, numVertices);
                        }

                        Array.Copy(compVerts, 0, tmp, numVertices, compNumVerts);
                        vertices = tmp;
                        numVertices += compNumVerts;
                    }

                    more = flags & (1 << 5);
                }
            }

            pvertices = vertices;
            return numVertices;
        }

        public Buf stbtt__cid_get_glyph_subrs(int glyphIndex)
        {
            var fdselect = Fdselect;
            var nranges = 0;
            var start = 0;
            var end = 0;
            var v = 0;
            var fmt = 0;
            var fdselector = -1;
            var i = 0;
            fdselect.stbtt__buf_seek(0);
            fmt = fdselect.stbtt__buf_get8();
            if (fmt == 0)
            {
                fdselect.stbtt__buf_skip(glyphIndex);
                fdselector = fdselect.stbtt__buf_get8();
            }
            else if (fmt == 3)
            {
                nranges = (int)fdselect.stbtt__buf_get(2);
                start = (int)fdselect.stbtt__buf_get(2);
                for (i = 0; i < nranges; i++)
                {
                    v = fdselect.stbtt__buf_get8();
                    end = (int)fdselect.stbtt__buf_get(2);
                    if (glyphIndex >= start && glyphIndex < end)
                    {
                        fdselector = v;
                        break;
                    }

                    start = end;
                }
            }

            if (fdselector == -1)
            {
                new Buf(FakePtr<byte>.Null, 0);
            }

            return Buf.stbtt__get_subrs(Cff, Fontdicts.stbtt__cff_index_get(fdselector));
        }

        public int stbtt__run_charstring(int glyphIndex, CharStringContext c)
        {
            var inHeader = 1;
            var maskbits = 0;
            var subrStackHeight = 0;
            var sp = 0;
            var v = 0;
            var i = 0;
            var b0 = 0;
            var hasSubrs = 0;
            var clearStack = 0;
            var s = new float[48];
            var subrStack = new Buf[10];
            for (i = 0; i < subrStack.Length; ++i)
                subrStack[i] = null;

            var subrs = Subrs;
            float f = 0;
            var b = Charstrings.stbtt__cff_index_get(glyphIndex);
            while (b.Cursor < b.Size)
            {
                i = 0;
                clearStack = 1;
                b0 = b.stbtt__buf_get8();
                switch (b0)
                {
                    case 0x13:
                    case 0x14:
                        if (inHeader != 0)
                        {
                            maskbits += sp / 2;
                        }

                        inHeader = 0;
                        b.stbtt__buf_skip((maskbits + 7) / 8);
                        break;
                    case 0x01:
                    case 0x03:
                    case 0x12:
                    case 0x17:
                        maskbits += sp / 2;
                        break;
                    case 0x15:
                        inHeader = 0;
                        if (sp < 2)
                        {
                            return 0;
                        }

                        c.stbtt__csctx_rmove_to(s[sp - 2], s[sp - 1]);
                        break;
                    case 0x04:
                        inHeader = 0;
                        if (sp < 1)
                        {
                            return 0;
                        }

                        c.stbtt__csctx_rmove_to(0, s[sp - 1]);
                        break;
                    case 0x16:
                        inHeader = 0;
                        if (sp < 1)
                        {
                            return 0;
                        }

                        c.stbtt__csctx_rmove_to(s[sp - 1], 0);
                        break;
                    case 0x05:
                        if (sp < 2)
                        {
                            return 0;
                        }

                        for (; i + 1 < sp; i += 2)
                            c.stbtt__csctx_rline_to(s[i], s[i + 1]);
                        break;
                    case 0x07:
                    case 0x06:
                        if (sp < 1)
                        {
                            return 0;
                        }

                        var gotoVlineto = b0 == 0x07 ? 1 : 0;
                        for (;;)
                        {
                            if (gotoVlineto == 0)
                            {
                                if (i >= sp)
                                {
                                    break;
                                }

                                c.stbtt__csctx_rline_to(s[i], 0);
                                i++;
                            }

                            gotoVlineto = 0;
                            if (i >= sp)
                            {
                                break;
                            }

                            c.stbtt__csctx_rline_to(0, s[i]);
                            i++;
                        }

                        break;
                    case 0x1F:
                    case 0x1E:
                        if (sp < 4)
                        {
                            return 0;
                        }

                        var gotoHvcurveto = b0 == 0x1F ? 1 : 0;
                        for (;;)
                        {
                            if (gotoHvcurveto == 0)
                            {
                                if (i + 3 >= sp)
                                {
                                    break;
                                }

                                c.stbtt__csctx_rccurve_to(0, s[i], s[i + 1], s[i + 2], s[i + 3],
                                    sp - i == 5 ? s[i + 4] : 0.0f);
                                i += 4;
                            }

                            gotoHvcurveto = 0;
                            if (i + 3 >= sp)
                            {
                                break;
                            }

                            c.stbtt__csctx_rccurve_to(s[i], 0, s[i + 1], s[i + 2], sp - i == 5 ? s[i + 4] : 0.0f,
                                s[i + 3]);
                            i += 4;
                        }

                        break;
                    case 0x08:
                        if (sp < 6)
                        {
                            return 0;
                        }

                        for (; i + 5 < sp; i += 6)
                            c.stbtt__csctx_rccurve_to(s[i], s[i + 1], s[i + 2], s[i + 3], s[i + 4], s[i + 5]);
                        break;
                    case 0x18:
                        if (sp < 8)
                        {
                            return 0;
                        }

                        for (; i + 5 < sp - 2; i += 6)
                            c.stbtt__csctx_rccurve_to(s[i], s[i + 1], s[i + 2], s[i + 3], s[i + 4], s[i + 5]);
                        if (i + 1 >= sp)
                        {
                            return 0;
                        }

                        c.stbtt__csctx_rline_to(s[i], s[i + 1]);
                        break;
                    case 0x19:
                        if (sp < 8)
                        {
                            return 0;
                        }

                        for (; i + 1 < sp - 6; i += 2)
                            c.stbtt__csctx_rline_to(s[i], s[i + 1]);
                        if (i + 5 >= sp)
                        {
                            return 0;
                        }

                        c.stbtt__csctx_rccurve_to(s[i], s[i + 1], s[i + 2], s[i + 3], s[i + 4], s[i + 5]);
                        break;
                    case 0x1A:
                    case 0x1B:
                        if (sp < 4)
                        {
                            return 0;
                        }

                        f = (float)0.0;
                        if ((sp & 1) != 0)
                        {
                            f = s[i];
                            i++;
                        }

                        for (; i + 3 < sp; i += 4)
                        {
                            if (b0 == 0x1B)
                            {
                                c.stbtt__csctx_rccurve_to(s[i], f, s[i + 1], s[i + 2], s[i + 3], (float)0.0);
                            }
                            else
                            {
                                c.stbtt__csctx_rccurve_to(f, s[i], s[i + 1], s[i + 2], (float)0.0, s[i + 3]);
                            }

                            f = (float)0.0;
                        }

                        break;
                    case 0x0A:
                    case 0x1D:
                        if (b0 == 0x0A)
                        {
                            if (hasSubrs == 0)
                            {
                                if (Fdselect.Size != 0)
                                {
                                    subrs = stbtt__cid_get_glyph_subrs(glyphIndex);
                                }

                                hasSubrs = 1;
                            }
                        }

                        if (sp < 1)
                        {
                            return 0;
                        }

                        v = (int)s[--sp];
                        if (subrStackHeight >= 10)
                        {
                            return 0;
                        }

                        subrStack[subrStackHeight++] = b;
                        b = b0 == 0x0A ? subrs.stbtt__get_subr(v) : Gsubrs.stbtt__get_subr(v);
                        if (b.Size == 0)
                        {
                            return 0;
                        }

                        b.Cursor = 0;
                        clearStack = 0;
                        break;
                    case 0x0B:
                        if (subrStackHeight <= 0)
                        {
                            return 0;
                        }

                        b = subrStack[--subrStackHeight];
                        clearStack = 0;
                        break;
                    case 0x0E:
                        c.stbtt__csctx_close_shape();
                        return 1;
                    case 0x0C:
                    {
                        float dx1 = 0;
                        float dx2 = 0;
                        float dx3 = 0;
                        float dx4 = 0;
                        float dx5 = 0;
                        float dx6 = 0;
                        float dy1 = 0;
                        float dy2 = 0;
                        float dy3 = 0;
                        float dy4 = 0;
                        float dy5 = 0;
                        float dy6 = 0;
                        float dx = 0;
                        float dy = 0;
                        var b1 = (int)b.stbtt__buf_get8();
                        switch (b1)
                        {
                            case 0x22:
                                if (sp < 7)
                                {
                                    return 0;
                                }

                                dx1 = s[0];
                                dx2 = s[1];
                                dy2 = s[2];
                                dx3 = s[3];
                                dx4 = s[4];
                                dx5 = s[5];
                                dx6 = s[6];
                                c.stbtt__csctx_rccurve_to(dx1, 0, dx2, dy2, dx3, 0);
                                c.stbtt__csctx_rccurve_to(dx4, 0, dx5, -dy2, dx6, 0);
                                break;
                            case 0x23:
                                if (sp < 13)
                                {
                                    return 0;
                                }

                                dx1 = s[0];
                                dy1 = s[1];
                                dx2 = s[2];
                                dy2 = s[3];
                                dx3 = s[4];
                                dy3 = s[5];
                                dx4 = s[6];
                                dy4 = s[7];
                                dx5 = s[8];
                                dy5 = s[9];
                                dx6 = s[10];
                                dy6 = s[11];
                                c.stbtt__csctx_rccurve_to(dx1, dy1, dx2, dy2, dx3, dy3);
                                c.stbtt__csctx_rccurve_to(dx4, dy4, dx5, dy5, dx6, dy6);
                                break;
                            case 0x24:
                                if (sp < 9)
                                {
                                    return 0;
                                }

                                dx1 = s[0];
                                dy1 = s[1];
                                dx2 = s[2];
                                dy2 = s[3];
                                dx3 = s[4];
                                dx4 = s[5];
                                dx5 = s[6];
                                dy5 = s[7];
                                dx6 = s[8];
                                c.stbtt__csctx_rccurve_to(dx1, dy1, dx2, dy2, dx3, 0);
                                c.stbtt__csctx_rccurve_to(dx4, 0, dx5, dy5, dx6, -(dy1 + dy2 + dy5));
                                break;
                            case 0x25:
                                if (sp < 11)
                                {
                                    return 0;
                                }

                                dx1 = s[0];
                                dy1 = s[1];
                                dx2 = s[2];
                                dy2 = s[3];
                                dx3 = s[4];
                                dy3 = s[5];
                                dx4 = s[6];
                                dy4 = s[7];
                                dx5 = s[8];
                                dy5 = s[9];
                                dx6 = dy6 = s[10];
                                dx = dx1 + dx2 + dx3 + dx4 + dx5;
                                dy = dy1 + dy2 + dy3 + dy4 + dy5;
                                if (Math.Abs((double)dx) > Math.Abs((double)dy))
                                {
                                    dy6 = -dy;
                                }
                                else
                                {
                                    dx6 = -dx;
                                }

                                c.stbtt__csctx_rccurve_to(dx1, dy1, dx2, dy2, dx3, dy3);
                                c.stbtt__csctx_rccurve_to(dx4, dy4, dx5, dy5, dx6, dy6);
                                break;
                            default:
                                return 0;
                        }
                    }
                        break;
                    default:
                        if (b0 != 255 && b0 != 28 && (b0 < 32 || b0 > 254))
                        {
                            return 0;
                        }

                        if (b0 == 255)
                        {
                            f = (float)(int)b.stbtt__buf_get(4) / 0x10000;
                        }
                        else
                        {
                            b.stbtt__buf_skip(-1);
                            f = (short)b.stbtt__cff_int();
                        }

                        if (sp >= 48)
                        {
                            return 0;
                        }

                        s[sp++] = f;
                        clearStack = 0;
                        break;
                }

                if (clearStack != 0)
                {
                    sp = 0;
                }
            }

            return 0;
        }

        public int stbtt__GetGlyphShapeT2(int glyphIndex, out StbttVertex[] pvertices)
        {
            var countCtx = new CharStringContext();
            countCtx.Bounds = 1;
            var outputCtx = new CharStringContext();
            if (stbtt__run_charstring(glyphIndex, countCtx) != 0)
            {
                pvertices = new StbttVertex[countCtx.NumVertices];
                outputCtx.Pvertices = pvertices;
                if (stbtt__run_charstring(glyphIndex, outputCtx) != 0)
                {
                    return outputCtx.NumVertices;
                }
            }

            pvertices = null;
            return 0;
        }

        public int stbtt__GetGlyphInfoT2(int glyphIndex, ref int x0, ref int y0,
            ref int x1, ref int y1)
        {
            var c = new CharStringContext();
            c.Bounds = 1;
            var r = stbtt__run_charstring(glyphIndex, c);
            x0 = r != 0 ? c.MinX : 0;
            y0 = r != 0 ? c.MinY : 0;
            x1 = r != 0 ? c.MaxX : 0;
            y1 = r != 0 ? c.MaxY : 0;
            return r != 0 ? c.NumVertices : 0;
        }

        public int stbtt_GetGlyphShape(int glyphIndex, out StbttVertex[] pvertices)
        {
            if (Cff.Size == 0)
            {
                return stbtt__GetGlyphShapeTT(glyphIndex, out pvertices);
            }

            return stbtt__GetGlyphShapeT2(glyphIndex, out pvertices);
        }

        public void stbtt_GetGlyphHMetrics(int glyphIndex, ref int advanceWidth,
            ref int leftSideBearing)
        {
            var numOfLongHorMetrics = TtUshort(Data + Hhea + 34);
            if (glyphIndex < numOfLongHorMetrics)
            {
                advanceWidth = TtShort(Data + Hmtx + 4 * glyphIndex);
                leftSideBearing = TtShort(Data + Hmtx + 4 * glyphIndex + 2);
            }
            else
            {
                advanceWidth = TtShort(Data + Hmtx + 4 * (numOfLongHorMetrics - 1));
                leftSideBearing = TtShort(Data + Hmtx + 4 * numOfLongHorMetrics +
                                          2 * (glyphIndex - numOfLongHorMetrics));
            }
        }

        public int stbtt_GetKerningTableLength()
        {
            var data = Data + Kern;
            if (Kern == 0)
            {
                return 0;
            }

            if (TtUshort(data + 2) < 1)
            {
                return 0;
            }

            if (TtUshort(data + 8) != 1)
            {
                return 0;
            }

            return TtUshort(data + 10);
        }

        public int stbtt_GetKerningTable(StbttKerningentry[] table, int tableLength)
        {
            var data = Data + Kern;
            var k = 0;
            var length = 0;
            if (Kern == 0)
            {
                return 0;
            }

            if (TtUshort(data + 2) < 1)
            {
                return 0;
            }

            if (TtUshort(data + 8) != 1)
            {
                return 0;
            }

            length = TtUshort(data + 10);
            if (tableLength < length)
            {
                length = tableLength;
            }

            for (k = 0; k < length; k++)
            {
                table[k].glyph1 = TtUshort(data + 18 + k * 6);
                table[k].glyph2 = TtUshort(data + 20 + k * 6);
                table[k].advance = TtShort(data + 22 + k * 6);
            }

            return length;
        }

        public int stbtt__GetGlyphKernInfoAdvance(int glyph1, int glyph2)
        {
            var data = Data + Kern;
            uint needle = 0;
            uint straw = 0;
            var l = 0;
            var r = 0;
            var m = 0;
            if (Kern == 0)
            {
                return 0;
            }

            if (TtUshort(data + 2) < 1)
            {
                return 0;
            }

            if (TtUshort(data + 8) != 1)
            {
                return 0;
            }

            l = 0;
            r = TtUshort(data + 10) - 1;
            needle = (uint)((glyph1 << 16) | glyph2);
            while (l <= r)
            {
                m = (l + r) >> 1;
                straw = TtUlong(data + 18 + m * 6);
                if (needle < straw)
                {
                    r = m - 1;
                }
                else if (needle > straw)
                {
                    l = m + 1;
                }
                else
                {
                    return TtShort(data + 22 + m * 6);
                }
            }

            return 0;
        }

        public int stbtt__GetGlyphGPOSInfoAdvance(int glyph1, int glyph2)
        {
            ushort lookupListOffset = 0;
            ushort lookupCount = 0;
            var i = 0;
            if (Gpos == 0)
            {
                return 0;
            }

            var data = Data + Gpos;
            if (TtUshort(data + 0) != 1)
            {
                return 0;
            }

            if (TtUshort(data + 2) != 0)
            {
                return 0;
            }

            lookupListOffset = TtUshort(data + 8);
            var lookupList = data + lookupListOffset;
            lookupCount = TtUshort(lookupList);
            for (i = 0; i < lookupCount; ++i)
            {
                var lookupOffset = TtUshort(lookupList + 2 + 2 * i);
                var lookupTable = lookupList + lookupOffset;
                var lookupType = TtUshort(lookupTable);
                var subTableCount = TtUshort(lookupTable + 4);
                var subTableOffsets = lookupTable + 6;
                switch (lookupType)
                {
                    case 2:
                    {
                        var sti = 0;
                        for (sti = 0; sti < subTableCount; sti++)
                        {
                            var subtableOffset = TtUshort(subTableOffsets + 2 * sti);
                            var table = lookupTable + subtableOffset;
                            var posFormat = TtUshort(table);
                            var coverageOffset = TtUshort(table + 2);
                            var coverageIndex = stbtt__GetCoverageIndex(table + coverageOffset, glyph1);
                            if (coverageIndex == -1)
                            {
                                continue;
                            }

                            switch (posFormat)
                            {
                                case 1:
                                {
                                    var l = 0;
                                    var r = 0;
                                    var m = 0;
                                    var straw = 0;
                                    var needle = 0;
                                    var valueFormat1 = TtUshort(table + 4);
                                    var valueFormat2 = TtUshort(table + 6);
                                    var valueRecordPairSizeInBytes = 2;
                                    var pairSetCount = TtUshort(table + 8);
                                    var pairPosOffset = TtUshort(table + 10 + 2 * coverageIndex);
                                    var pairValueTable = table + pairPosOffset;
                                    var pairValueCount = TtUshort(pairValueTable);
                                    var pairValueArray = pairValueTable + 2;
                                    if (valueFormat1 != 4)
                                    {
                                        return 0;
                                    }

                                    if (valueFormat2 != 0)
                                    {
                                        return 0;
                                    }

                                    needle = glyph2;
                                    r = pairValueCount - 1;
                                    l = 0;
                                    while (l <= r)
                                    {
                                        ushort secondGlyph = 0;
                                        m = (l + r) >> 1;
                                        var pairValue = pairValueArray + (2 + valueRecordPairSizeInBytes) * m;
                                        secondGlyph = TtUshort(pairValue);
                                        straw = secondGlyph;
                                        if (needle < straw)
                                        {
                                            r = m - 1;
                                        }
                                        else if (needle > straw)
                                        {
                                            l = m + 1;
                                        }
                                        else
                                        {
                                            var xAdvance = TtShort(pairValue + 2);
                                            return xAdvance;
                                        }
                                    }
                                }
                                    break;
                                case 2:
                                {
                                    var valueFormat1 = TtUshort(table + 4);
                                    var valueFormat2 = TtUshort(table + 6);
                                    var classDef1Offset = TtUshort(table + 8);
                                    var classDef2Offset = TtUshort(table + 10);
                                    var glyph1Class = stbtt__GetGlyphClass(table + classDef1Offset, glyph1);
                                    var glyph2Class = stbtt__GetGlyphClass(table + classDef2Offset, glyph2);
                                    var class1Count = TtUshort(table + 12);
                                    var class2Count = TtUshort(table + 14);
                                    if (valueFormat1 != 4)
                                    {
                                        return 0;
                                    }

                                    if (valueFormat2 != 0)
                                    {
                                        return 0;
                                    }

                                    if (glyph1Class >= 0 && glyph1Class < class1Count && glyph2Class >= 0 &&
                                        glyph2Class < class2Count)
                                    {
                                        var class1Records = table + 16;
                                        var class2Records = class1Records + 2 * glyph1Class * class2Count;
                                        var xAdvance = TtShort(class2Records + 2 * glyph2Class);
                                        return xAdvance;
                                    }
                                }
                                    break;
                            }
                        }

                        break;
                    }
                }
            }

            return 0;
        }

        public int stbtt_GetGlyphKernAdvance(int g1, int g2)
        {
            var xAdvance = 0;
            if (Gpos != 0)
            {
                xAdvance += stbtt__GetGlyphGPOSInfoAdvance(g1, g2);
            }
            else if (Kern != 0)
            {
                xAdvance += stbtt__GetGlyphKernInfoAdvance(g1, g2);
            }

            return xAdvance;
        }

        public int stbtt_GetCodepointKernAdvance(int ch1, int ch2)
        {
            if (Kern == 0 && Gpos == 0)
            {
                return 0;
            }

            return stbtt_GetGlyphKernAdvance(stbtt_FindGlyphIndex(ch1), stbtt_FindGlyphIndex(ch2));
        }

        public void stbtt_GetCodepointHMetrics(int codepoint, ref int advanceWidth,
            ref int leftSideBearing)
        {
            stbtt_GetGlyphHMetrics(stbtt_FindGlyphIndex(codepoint), ref advanceWidth, ref leftSideBearing);
        }

        public void stbtt_GetFontVMetrics(out int ascent, out int descent, out int lineGap)
        {
            ascent = TtShort(Data + Hhea + 4);
            descent = TtShort(Data + Hhea + 6);
            lineGap = TtShort(Data + Hhea + 8);
        }

        public int stbtt_GetFontVMetricsOS2(ref int typoAscent, ref int typoDescent,
            ref int typoLineGap)
        {
            var tab = (int)stbtt__find_table(Data, (uint)Fontstart, "OS/2");
            if (tab == 0)
            {
                return 0;
            }

            typoAscent = TtShort(Data + tab + 68);
            typoDescent = TtShort(Data + tab + 70);
            typoLineGap = TtShort(Data + tab + 72);
            return 1;
        }

        public void stbtt_GetFontBoundingBox(ref int x0, ref int y0, ref int x1, ref int y1)
        {
            x0 = TtShort(Data + Head + 36);
            y0 = TtShort(Data + Head + 38);
            x1 = TtShort(Data + Head + 40);
            y1 = TtShort(Data + Head + 42);
        }

        public float stbtt_ScaleForPixelHeight(float height)
        {
            var fheight = TtShort(Data + Hhea + 4) - TtShort(Data + Hhea + 6);
            return height / fheight;
        }

        public float stbtt_ScaleForMappingEmToPixels(float pixels)
        {
            var unitsPerEm = (int)TtUshort(Data + Head + 18);
            return pixels / unitsPerEm;
        }

        public FakePtr<byte> stbtt_FindSVGDoc(int gl)
        {
            var i = 0;
            var data = Data;
            var svgDocList = data + stbtt__get_svg();
            var numEntries = (int)TtUshort(svgDocList);
            var svgDocs = svgDocList + 2;
            for (i = 0; i < numEntries; i++)
            {
                var svgDoc = svgDocs + 12 * i;
                if (gl >= TtUshort(svgDoc) && gl <= TtUshort(svgDoc + 2))
                {
                    return svgDoc;
                }
            }

            return FakePtr<byte>.Null;
        }

        public int stbtt_GetGlyphSVG(int gl, ref FakePtr<byte> svg)
        {
            var data = Data;
            if (Svg == 0)
            {
                return 0;
            }

            var svgDoc = stbtt_FindSVGDoc(gl);
            if (!svgDoc.IsNull)
            {
                svg = data + Svg + TtUlong(svgDoc + 4);
                return (int)TtUlong(svgDoc + 8);
            }

            return 0;
        }

        public int stbtt_GetCodepointSVG(int unicodeCodepoint, ref FakePtr<byte> svg)
        {
            return stbtt_GetGlyphSVG(stbtt_FindGlyphIndex(unicodeCodepoint), ref svg);
        }

        public void stbtt_GetGlyphBitmapBoxSubpixel(int glyph, float scaleX, float scaleY,
            float shiftX, float shiftY, ref int ix0, ref int iy0, ref int ix1, ref int iy1)
        {
            var x0 = 0;
            var y0 = 0;
            var x1 = 0;
            var y1 = 0;
            if (stbtt_GetGlyphBox(glyph, ref x0, ref y0, ref x1, ref y1) == 0)
            {
                ix0 = 0;
                iy0 = 0;
                ix1 = 0;
                iy1 = 0;
            }
            else
            {
                ix0 = (int)Math.Floor(x0 * scaleX + shiftX);
                iy0 = (int)Math.Floor(-y1 * scaleY + shiftY);
                ix1 = (int)Math.Ceiling(x1 * scaleX + shiftX);
                iy1 = (int)Math.Ceiling(-y0 * scaleY + shiftY);
            }
        }

        public void stbtt_GetGlyphBitmapBox(int glyph, float scaleX, float scaleY,
            ref int ix0, ref int iy0, ref int ix1, ref int iy1)
        {
            stbtt_GetGlyphBitmapBoxSubpixel(glyph, scaleX, scaleY, 0.0f, 0.0f, ref ix0, ref iy0, ref ix1, ref iy1);
        }

        public void stbtt_GetCodepointBitmapBoxSubpixel(int codepoint, float scaleX,
            float scaleY, float shiftX, float shiftY, ref int ix0, ref int iy0, ref int ix1, ref int iy1)
        {
            stbtt_GetGlyphBitmapBoxSubpixel(stbtt_FindGlyphIndex(codepoint), scaleX, scaleY, shiftX,
                shiftY, ref ix0, ref iy0, ref ix1, ref iy1);
        }

        public void stbtt_GetCodepointBitmapBox(int codepoint, float scaleX, float scaleY,
            ref int ix0, ref int iy0, ref int ix1, ref int iy1)
        {
            stbtt_GetCodepointBitmapBoxSubpixel(codepoint, scaleX, scaleY, 0.0f, 0.0f, ref ix0, ref iy0,
                ref ix1, ref iy1);
        }

        public FakePtr<byte> stbtt_GetGlyphBitmapSubpixel(float scaleX, float scaleY,
            float shiftX, float shiftY, int glyph, ref int width, ref int height, ref int xoff, ref int yoff)
        {
            var ix0 = 0;
            var iy0 = 0;
            var ix1 = 0;
            var iy1 = 0;
            var gbm = new Bitmap();
            StbttVertex[] vertices;
            var numVerts = stbtt_GetGlyphShape(glyph, out vertices);
            if (scaleX == 0)
            {
                scaleX = scaleY;
            }

            if (scaleY == 0)
            {
                if (scaleX == 0)
                {
                    return FakePtr<byte>.Null;
                }

                scaleY = scaleX;
            }

            stbtt_GetGlyphBitmapBoxSubpixel(glyph, scaleX, scaleY, shiftX, shiftY, ref ix0, ref iy0, ref ix1,
                ref iy1);
            gbm.W = ix1 - ix0;
            gbm.H = iy1 - iy0;
            width = gbm.W;
            height = gbm.H;
            xoff = ix0;
            yoff = iy0;
            if (gbm.W != 0 && gbm.H != 0)
            {
                gbm.Pixels = FakePtr<byte>.CreateWithSize(gbm.W * gbm.H);
                gbm.Stride = gbm.W;
                gbm.stbtt_Rasterize(0.35f, vertices, numVerts, scaleX, scaleY, shiftX, shiftY, ix0, iy0, 1);
            }

            return gbm.Pixels;
        }

        public FakePtr<byte> stbtt_GetGlyphBitmap(float scaleX, float scaleY, int glyph,
            ref int width, ref int height, ref int xoff, ref int yoff)
        {
            return stbtt_GetGlyphBitmapSubpixel(scaleX, scaleY, 0.0f, 0.0f, glyph, ref width, ref height,
                ref xoff, ref yoff);
        }

        public void stbtt_MakeGlyphBitmapSubpixel(FakePtr<byte> output, int outW,
            int outH, int outStride, float scaleX, float scaleY, float shiftX, float shiftY, int glyph)
        {
            var ix0 = 0;
            var iy0 = 0;
            var ix1 = 0;
            var iy1 = 0;
            StbttVertex[] vertices;
            var numVerts = stbtt_GetGlyphShape(glyph, out vertices);
            var gbm = new Bitmap();
            stbtt_GetGlyphBitmapBoxSubpixel(glyph, scaleX, scaleY, shiftX, shiftY, ref ix0, ref iy0, ref ix1,
                ref iy1);
            gbm.Pixels = output;
            gbm.W = outW;
            gbm.H = outH;
            gbm.Stride = outStride;

            if (gbm.W != 0 && gbm.H != 0)
            {
                gbm.stbtt_Rasterize(0.35f, vertices, numVerts, scaleX, scaleY, shiftX, shiftY, ix0, iy0, 1);
            }
        }

        public void stbtt_MakeGlyphBitmap(FakePtr<byte> output, int outW, int outH,
            int outStride, float scaleX, float scaleY, int glyph)
        {
            stbtt_MakeGlyphBitmapSubpixel(output, outW, outH, outStride, scaleX, scaleY, 0.0f, 0.0f, glyph);
        }

        public FakePtr<byte> stbtt_GetCodepointBitmapSubpixel(float scaleX, float scaleY,
            float shiftX, float shiftY, int codepoint, ref int width, ref int height, ref int xoff, ref int yoff)
        {
            return stbtt_GetGlyphBitmapSubpixel(scaleX, scaleY, shiftX, shiftY,
                stbtt_FindGlyphIndex(codepoint), ref width, ref height, ref xoff, ref yoff);
        }

        public void stbtt_MakeCodepointBitmapSubpixelPrefilter(FakePtr<byte> output,
            int outW, int outH, int outStride, float scaleX, float scaleY, float shiftX, float shiftY,
            int oversampleX, int oversampleY, ref float subX, ref float subY, int codepoint)
        {
            stbtt_MakeGlyphBitmapSubpixelPrefilter(output, outW, outH, outStride, scaleX, scaleY, shiftX,
                shiftY, oversampleX, oversampleY, ref subX, ref subY, stbtt_FindGlyphIndex(codepoint));
        }

        public void stbtt_MakeCodepointBitmapSubpixel(FakePtr<byte> output, int outW,
            int outH, int outStride, float scaleX, float scaleY, float shiftX, float shiftY, int codepoint)
        {
            stbtt_MakeGlyphBitmapSubpixel(output, outW, outH, outStride, scaleX, scaleY, shiftX, shiftY,
                stbtt_FindGlyphIndex(codepoint));
        }

        public FakePtr<byte> stbtt_GetCodepointBitmap(float scaleX, float scaleY,
            int codepoint, ref int width, ref int height, ref int xoff, ref int yoff)
        {
            return stbtt_GetCodepointBitmapSubpixel(scaleX, scaleY, 0.0f, 0.0f, codepoint, ref width,
                ref height, ref xoff, ref yoff);
        }

        public void stbtt_MakeCodepointBitmap(FakePtr<byte> output, int outW, int outH,
            int outStride, float scaleX, float scaleY, int codepoint)
        {
            stbtt_MakeCodepointBitmapSubpixel(output, outW, outH, outStride, scaleX, scaleY, 0.0f, 0.0f,
                codepoint);
        }

        public void stbtt_MakeGlyphBitmapSubpixelPrefilter(FakePtr<byte> output, int outW,
            int outH, int outStride, float scaleX, float scaleY, float shiftX, float shiftY, int prefilterX,
            int prefilterY, ref float subX, ref float subY, int glyph)
        {
            stbtt_MakeGlyphBitmapSubpixel(output, outW - (prefilterX - 1), outH - (prefilterY - 1),
                outStride, scaleX, scaleY, shiftX, shiftY, glyph);
            if (prefilterX > 1)
            {
                stbtt__h_prefilter(output, outW, outH, outStride, (uint)prefilterX);
            }

            if (prefilterY > 1)
            {
                stbtt__v_prefilter(output, outW, outH, outStride, (uint)prefilterY);
            }

            subX = stbtt__oversample_shift(prefilterX);
            subY = stbtt__oversample_shift(prefilterY);
        }

        public byte[] stbtt_GetGlyphSDF(float scale, int glyph, int padding,
            byte onedgeValue, float pixelDistScale, ref int width, ref int height, ref int xoff, ref int yoff)
        {
            var scaleX = scale;
            var scaleY = scale;
            var ix0 = 0;
            var iy0 = 0;
            var ix1 = 0;
            var iy1 = 0;
            var w = 0;
            var h = 0;
            byte[] data = null;
            if (scale == 0)
            {
                return null;
            }

            stbtt_GetGlyphBitmapBoxSubpixel(glyph, scale, scale, 0.0f, 0.0f, ref ix0, ref iy0, ref ix1, ref iy1);
            if (ix0 == ix1 || iy0 == iy1)
            {
                return null;
            }

            ix0 -= padding;
            iy0 -= padding;
            ix1 += padding;
            iy1 += padding;
            w = ix1 - ix0;
            h = iy1 - iy0;
            width = w;
            height = h;
            xoff = ix0;
            yoff = iy0;
            scaleY = -scaleY;
            {
                var x = 0;
                var y = 0;
                var i = 0;
                var j = 0;
                StbttVertex[] verts;
                var numVerts = stbtt_GetGlyphShape(glyph, out verts);
                data = new byte[w * h];
                var precompute = new float[numVerts];
                for (i = 0, j = numVerts - 1; i < numVerts; j = i++)
                    if (verts[i].type == StbttVline)
                    {
                        var x0 = verts[i].x * scaleX;
                        var y0 = verts[i].y * scaleY;
                        var x1 = verts[j].x * scaleX;
                        var y1 = verts[j].y * scaleY;
                        var dist = (float)Math.Sqrt((x1 - x0) * (x1 - x0) + (y1 - y0) * (y1 - y0));
                        precompute[i] = dist == 0 ? 0.0f : 1.0f / dist;
                    }
                    else if (verts[i].type == StbttVcurve)
                    {
                        var x2 = verts[j].x * scaleX;
                        var y2 = verts[j].y * scaleY;
                        var x1 = verts[i].cx * scaleX;
                        var y1 = verts[i].cy * scaleY;
                        var x0 = verts[i].x * scaleX;
                        var y0 = verts[i].y * scaleY;
                        var bx = x0 - 2 * x1 + x2;
                        var by = y0 - 2 * y1 + y2;
                        var len2 = bx * bx + by * by;
                        if (len2 != 0.0f)
                        {
                            precompute[i] = 1.0f / (bx * bx + by * by);
                        }
                        else
                        {
                            precompute[i] = 0.0f;
                        }
                    }
                    else
                    {
                        precompute[i] = 0.0f;
                    }

                for (y = iy0; y < iy1; ++y)
                for (x = ix0; x < ix1; ++x)
                {
                    float val = 0;
                    var minDist = 999999.0f;
                    var sx = x + 0.5f;
                    var sy = y + 0.5f;
                    var xGspace = sx / scaleX;
                    var yGspace = sy / scaleY;
                    var winding = stbtt__compute_crossings_x(xGspace, yGspace, numVerts, verts);
                    for (i = 0; i < numVerts; ++i)
                    {
                        var x0 = verts[i].x * scaleX;
                        var y0 = verts[i].y * scaleY;
                        var dist2 = (x0 - sx) * (x0 - sx) + (y0 - sy) * (y0 - sy);
                        if (dist2 < minDist * minDist)
                        {
                            minDist = (float)Math.Sqrt(dist2);
                        }

                        if (verts[i].type == StbttVline)
                        {
                            var x1 = verts[i - 1].x * scaleX;
                            var y1 = verts[i - 1].y * scaleY;
                            var dist = (float)Math.Abs((double)((x1 - x0) * (y0 - sy) - (y1 - y0) * (x0 - sx))) *
                                       precompute[i];
                            if (dist < minDist)
                            {
                                var dx = x1 - x0;
                                var dy = y1 - y0;
                                var px = x0 - sx;
                                var py = y0 - sy;
                                var t = -(px * dx + py * dy) / (dx * dx + dy * dy);
                                if (t >= 0.0f && t <= 1.0f)
                                {
                                    minDist = dist;
                                }
                            }
                        }
                        else if (verts[i].type == StbttVcurve)
                        {
                            var x2 = verts[i - 1].x * scaleX;
                            var y2 = verts[i - 1].y * scaleY;
                            var x1 = verts[i].cx * scaleX;
                            var y1 = verts[i].cy * scaleY;
                            var boxX0 = (x0 < x1 ? x0 : x1) < x2 ? x0 < x1 ? x0 : x1 : x2;
                            var boxY0 = (y0 < y1 ? y0 : y1) < y2 ? y0 < y1 ? y0 : y1 : y2;
                            var boxX1 = (x0 < x1 ? x1 : x0) < x2 ? x2 : x0 < x1 ? x1 : x0;
                            var boxY1 = (y0 < y1 ? y1 : y0) < y2 ? y2 : y0 < y1 ? y1 : y0;
                            if (sx > boxX0 - minDist && sx < boxX1 + minDist && sy > boxY0 - minDist &&
                                sy < boxY1 + minDist)
                            {
                                var num = 0;
                                var ax = x1 - x0;
                                var ay = y1 - y0;
                                var bx = x0 - 2 * x1 + x2;
                                var by = y0 - 2 * y1 + y2;
                                var mx = x0 - sx;
                                var my = y0 - sy;
                                var res = new float[3];
                                float px = 0;
                                float py = 0;
                                float t = 0;
                                float it = 0;
                                var aInv = precompute[i];
                                if (aInv == 0.0)
                                {
                                    var a = 3 * (ax * bx + ay * by);
                                    var b = 2 * (ax * ax + ay * ay) + (mx * bx + my * by);
                                    var c = mx * ax + my * ay;
                                    if (a == 0.0)
                                    {
                                        if (b != 0.0)
                                        {
                                            res[num++] = -c / b;
                                        }
                                    }
                                    else
                                    {
                                        var discriminant = b * b - 4 * a * c;
                                        if (discriminant < 0)
                                        {
                                            num = 0;
                                        }
                                        else
                                        {
                                            var root = (float)Math.Sqrt(discriminant);
                                            res[0] = (-b - root) / (2 * a);
                                            res[1] = (-b + root) / (2 * a);
                                            num = 2;
                                        }
                                    }
                                }
                                else
                                {
                                    var b = 3 * (ax * bx + ay * by) * aInv;
                                    var c = (2 * (ax * ax + ay * ay) + (mx * bx + my * by)) * aInv;
                                    var d = (mx * ax + my * ay) * aInv;
                                    num = stbtt__solve_cubic(b, c, d, res);
                                }

                                if (num >= 1 && res[0] >= 0.0f && res[0] <= 1.0f)
                                {
                                    t = res[0];
                                    it = 1.0f - t;
                                    px = it * it * x0 + 2 * t * it * x1 + t * t * x2;
                                    py = it * it * y0 + 2 * t * it * y1 + t * t * y2;
                                    dist2 = (px - sx) * (px - sx) + (py - sy) * (py - sy);
                                    if (dist2 < minDist * minDist)
                                    {
                                        minDist = (float)Math.Sqrt(dist2);
                                    }
                                }

                                if (num >= 2 && res[1] >= 0.0f && res[1] <= 1.0f)
                                {
                                    t = res[1];
                                    it = 1.0f - t;
                                    px = it * it * x0 + 2 * t * it * x1 + t * t * x2;
                                    py = it * it * y0 + 2 * t * it * y1 + t * t * y2;
                                    dist2 = (px - sx) * (px - sx) + (py - sy) * (py - sy);
                                    if (dist2 < minDist * minDist)
                                    {
                                        minDist = (float)Math.Sqrt(dist2);
                                    }
                                }

                                if (num >= 3 && res[2] >= 0.0f && res[2] <= 1.0f)
                                {
                                    t = res[2];
                                    it = 1.0f - t;
                                    px = it * it * x0 + 2 * t * it * x1 + t * t * x2;
                                    py = it * it * y0 + 2 * t * it * y1 + t * t * y2;
                                    dist2 = (px - sx) * (px - sx) + (py - sy) * (py - sy);
                                    if (dist2 < minDist * minDist)
                                    {
                                        minDist = (float)Math.Sqrt(dist2);
                                    }
                                }
                            }
                        }
                    }

                    if (winding == 0)
                    {
                        minDist = -minDist;
                    }

                    val = onedgeValue + pixelDistScale * minDist;
                    if (val < 0)
                    {
                        val = 0;
                    }
                    else if (val > 255)
                    {
                        val = 255;
                    }

                    data[(y - iy0) * w + (x - ix0)] = (byte)val;
                }
            }

            return data;
        }

        public byte[] stbtt_GetCodepointSDF(float scale, int codepoint, int padding,
            byte onedgeValue, float pixelDistScale, ref int width, ref int height, ref int xoff, ref int yoff)
        {
            return stbtt_GetGlyphSDF(scale, stbtt_FindGlyphIndex(codepoint), padding, onedgeValue,
                pixelDistScale, ref width, ref height, ref xoff, ref yoff);
        }

        public FakePtr<byte> stbtt_GetFontNameString(FontInfo font, ref int length, int platformId,
            int encodingId, int languageId, int nameId)
        {
            var i = 0;
            var count = 0;
            var stringOffset = 0;
            var fc = font.Data;
            var offset = (uint)font.Fontstart;
            var nm = stbtt__find_table(fc, offset, "name");
            if (nm == 0)
            {
                return FakePtr<byte>.Null;
            }

            count = TtUshort(fc + nm + 2);
            stringOffset = (int)(nm + TtUshort(fc + nm + 4));
            for (i = 0; i < count; ++i)
            {
                var loc = (uint)(nm + 6 + 12 * i);
                if (platformId == TtUshort(fc + loc + 0) && encodingId == TtUshort(fc + loc + 2) &&
                    languageId == TtUshort(fc + loc + 4) && nameId == TtUshort(fc + loc + 6))
                {
                    length = TtUshort(fc + loc + 8);
                    return fc + stringOffset + TtUshort(fc + loc + 10);
                }
            }

            return FakePtr<byte>.Null;
        }

        public int stbtt_InitFont(byte[] data, int offset)
        {
            return stbtt_InitFont_internal(data, offset);
        }
    }
}