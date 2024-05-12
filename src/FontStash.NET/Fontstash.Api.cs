using System;
using System.IO;

namespace FontStash.NET
{
    public delegate void HandleError(FonsErrorCode error, int val);

    public sealed partial class FontManager : IDisposable
    {
        public const int Invalid = -1;

        public const int ScratchBufSize = 96000;
        public const int HashLutSize = 256;
        public const int InitFonts = 4;
        public const int InitGlyphs = 256;
        public const int InitAtlasNodes = 256;
        public const int VertexCount = 1024;
        public const int MaxStates = 20;
        public const int MaxFallbacks = 20;
        private readonly uint[] colours = new uint[VertexCount * 2];

        // Texture
        private readonly int[] dirtyRect = new int[4];

        // States
        private readonly FonsState[] states = new FonsState[MaxStates];
        private readonly float[] tcoords = new float[VertexCount * 2];

        // Rendering
        private readonly float[] verts = new float[VertexCount * 2];
        private FonsAtlas atlas;
        private int cfonts;

        // Fonts
        private FonsFont[] fonts;

        // Error Handling
        private HandleError handleError;
        private float itw, ith;
        private int nfonts;
        private int nscratch;
        private int nstates;
        private int nverts;

        // Meta
        private FontParams @params;

        // Scratch Buffer
        private byte[] scratch;
        private byte[] texData;
        
        #region Error

        public void SetErrorCallback(HandleError callback)
        {
            handleError = callback;
        }

        #endregion

        #region Debug

        public void DrawDebug(float x, float y)
        {
            var w = @params.Width;
            var h = @params.Height;
            var u = w == 0 ? 0 : 1.0f / w;
            var v = h == 0 ? 0 : 1.0f / h;

            if (nverts + 6 + 6 > VertexCount)
            {
                Flush();
            }

            Vertex(x + 0, y + 0, u, v, 0x0fffffff);
            Vertex(x + w, y + h, u, v, 0x0fffffff);
            Vertex(x + w, y + 0, u, v, 0x0fffffff);

            Vertex(x + 0, y + 0, u, v, 0x0fffffff);
            Vertex(x + 0, y + h, u, v, 0x0fffffff);
            Vertex(x + w, y + h, u, v, 0x0fffffff);

            Vertex(x + 0, y + 0, 0, 0, 0xffffffff);
            Vertex(x + w, y + h, 1, 1, 0xffffffff);
            Vertex(x + w, y + 0, 1, 0, 0xffffffff);

            Vertex(x + 0, y + 0, 0, 0, 0xffffffff);
            Vertex(x + 0, y + h, 0, 1, 0xffffffff);
            Vertex(x + w, y + h, 1, 1, 0xffffffff);

            for (var i = 0; i < atlas.Nnodes; i++)
            {
                var n = atlas.Nodes[i];

                if (nverts + 6 > VertexCount)
                {
                    Flush();
                }

                Vertex(x + n.X + 0, y + n.Y + 0, u, v, 0xc00000ff);
                Vertex(x + n.X + n.Width, y + n.Y + 1, u, v, 0xc00000ff);
                Vertex(x + n.X + n.Width, y + n.Y + 0, u, v, 0xc00000ff);

                Vertex(x + n.X + 0, y + n.Y + 0, u, v, 0xc00000ff);
                Vertex(x + n.X + 0, y + n.Y + 1, u, v, 0xc00000ff);
                Vertex(x + n.X + n.Width, y + n.Y + 1, u, v, 0xc00000ff);
            }

            Flush();
        }

        #endregion

        #region Constructor and Destructor

        public FontManager(FontParams @params)
        {
            this.@params = @params;

            scratch = new byte[ScratchBufSize];

            if (this.@params.RenderCreate != null)
            {
                if (!this.@params.RenderCreate.Invoke(this.@params.Width, this.@params.Height))
                {
                    Dispose();
                    throw new Exception("Failed to create fontstash!");
                }
            }

            atlas = new FonsAtlas(this.@params.Width, this.@params.Height, InitAtlasNodes);

            fonts = new FonsFont[InitFonts];
            cfonts = InitFonts;
            nfonts = 0;

            itw = 1.0f / this.@params.Width;
            ith = 1.0f / this.@params.Height;
            texData = new byte[this.@params.Width * this.@params.Height];

            dirtyRect[0] = this.@params.Width;
            dirtyRect[1] = this.@params.Height;
            dirtyRect[2] = 0;
            dirtyRect[3] = 0;

            AddWhiteRect(2, 2);

            PushState();
            ClearState();
        }

        public void Dispose()
        {
            @params.RenderDelete?.Invoke();
            Array.Clear(fonts, 0, nfonts);
            Array.Clear(states, 0, nstates);
            atlas = null;
            GC.Collect();
        }

        #endregion

        #region Metrics

        public void GetAtlasSize(out int width, out int height)
        {
            width = @params.Width;
            height = @params.Height;
        }

        public bool ExpandAtlas(int width, int height)
        {
            width = Math.Max(width, @params.Width);
            height = Math.Max(height, @params.Height);

            if (width == @params.Width && height == @params.Height)
            {
                return true;
            }

            Flush();

            if (@params.RenderResize?.Invoke(width, height) == false)
            {
                return false;
            }

            var data = new byte[width * height];
            for (var i = 0; i < @params.Height; i++)
            {
                var dstIdx = i * width;
                var srcIdx = i * @params.Width;
                Array.Copy(texData, srcIdx, data, dstIdx, @params.Width);
                if (width > @params.Width)
                {
                    Array.Fill<byte>(data, 0, dstIdx + @params.Width, width - @params.Width);
                }
            }

            if (height > @params.Height)
            {
                Array.Fill<byte>(data, 0, @params.Height * width, (height - @params.Height) * width);
            }

            texData = data;

            atlas.Expand(width, height);

            var maxy = 0;
            for (var i = 0; i < atlas.Nnodes; i++)
                maxy = Math.Max(maxy, atlas.Nodes[i].Y);
            dirtyRect[0] = 0;
            dirtyRect[1] = 0;
            dirtyRect[2] = @params.Width;
            dirtyRect[3] = maxy;

            @params.Width = width;
            @params.Height = height;
            itw = 1.0f / @params.Width;
            ith = 1.0f / @params.Height;

            return true;
        }

        public bool ResetAtlas(int width, int height)
        {
            Flush();

            if (@params.RenderResize != null)
            {
                if (@params.RenderResize.Invoke(width, height) == false)
                {
                    return false;
                }
            }

            atlas.Reset(width, height);

            texData = new byte[width * height];

            dirtyRect[0] = width;
            dirtyRect[1] = height;
            dirtyRect[2] = 0;
            dirtyRect[3] = 0;

            for (var i = 0; i < nfonts; i++)
            {
                var font = fonts[i];
                font.Nglyphs = 0;
                for (var j = 0; j < HashLutSize; j++) font.Lut[j] = -1;
            }

            @params.Width = width;
            @params.Height = height;
            itw = 1.0f / @params.Width;
            ith = 1.0f / @params.Height;

            AddWhiteRect(2, 2);

            return true;
        }

        #endregion

        #region Add Fonts

        public int AddFont(string name, string path, int fontIndex)
        {
            if (!File.Exists(path))
            {
                return Invalid;
            }

            var data = File.ReadAllBytes(path);
            return AddFontMem(name, data, 1, fontIndex);
        }

        public int AddFont(string name, string path)
        {
            return AddFont(name, path, 0);
        }

        public int AddFontMem(string name, byte[] data, int freeData, int fontIndex)
        {
            var idx = AllocFont();
            if (idx == Invalid)
            {
                return Invalid;
            }

            var font = fonts[idx];

            font.Name = name;

            for (var i = 0; i < HashLutSize; i++) font.Lut[i] = -1;

            font.DataSize = data.Length;
            font.Data = data;
            font.FreeData = (byte)freeData;

            nscratch = 0;
            if (FonsTt.LoadFont(font.Font, data, fontIndex) == 0)
            {
                nfonts--;
                return Invalid;
            }

            FonsTt.GetFontVMetrics(font.Font, out var ascent, out var descent, out var lineGap);
            ascent += lineGap;
            var fh = ascent - descent;
            font.Ascender = ascent / (float)fh;
            font.Descender = descent / (float)fh;
            font.Lineh = font.Ascender - font.Descender;

            return idx;
        }

        public int AddFontMem(string name, byte[] data, int freeData)
        {
            return AddFontMem(name, data, freeData, 0);
        }

        public int GetFontByName(string name)
        {
            for (var i = 0; i < nfonts; i++)
                if (fonts[i].Name == name)
                {
                    return i;
                }

            return Invalid;
        }

        public bool AddFallbackFont(int @base, int fallback)
        {
            var baseFont = fonts[@base];
            if (baseFont.Nfallbacks < MaxFallbacks)
            {
                baseFont.Fallbacks[baseFont.Nfallbacks++] = fallback;
                return true;
            }

            return false;
        }

        public void ResetFallbackFont(int @base)
        {
            var baseFont = fonts[@base];
            baseFont.Nfallbacks = 0;
            baseFont.Nglyphs = 0;
            for (var i = 0; i < HashLutSize; i++) baseFont.Lut[i] = -1;
        }

        #endregion

        #region State Handling

        public void PushState()
        {
            if (nstates >= MaxStates)
            {
                handleError?.Invoke(FonsErrorCode.StatesOverflow, 0);
                return;
            }

            if (nstates > 0)
            {
                states[nstates - 1] = states[nstates].Copy();
            }

            nstates++;
        }

        public void PopState()
        {
            if (nstates <= 1)
            {
                handleError.Invoke(FonsErrorCode.StatesUnderflow, 0);
                return;
            }

            nstates--;
        }

        public void ClearState()
        {
            var state = GetState();
            state.Size = 12.0f;
            state.Colour = 0xffffffff;
            state.Font = 0;
            state.Blur = 0;
            state.Spacing = 0;
            state.Align = (int)FonsAlign.Left | (int)FonsAlign.Baseline;
        }

        #endregion

        #region State Settings

        public void SetSize(float size)
        {
            GetState().Size = size;
        }

        public void SetColour(uint colour)
        {
            GetState().Colour = colour;
        }

        public void SetSpacing(float spacing)
        {
            GetState().Spacing = spacing;
        }

        public void SetBlur(float blur)
        {
            GetState().Blur = blur;
        }

        public void SetAlign(int align)
        {
            GetState().Align = align;
        }

        public void SetFont(int font)
        {
            GetState().Font = font;
        }

        #endregion

        #region Draw Text

        public float DrawText(float x, float y, string str, string end)
        {
            var state = GetState();
            uint codepoint = 0;
            uint utf8State = 0;
            FonsGlyph glyph = null;
            var prevGlyphIndex = Invalid;
            var isize = (short)(state.Size * 10.0f);
            var iblur = (short)state.Blur;

            if (state.Font < 0 || state.Font >= nfonts)
            {
                return x;
            }

            var font = fonts[state.Font];
            if (font.Data == null)
            {
                return x;
            }

            var scale = FonsTt.GetPixelHeightScale(font.Font, isize / 10.0f);

            // Horizontal alignment
            if ((state.Align & (int)FonsAlign.Left) != 0)
            {
                // empty
            }
            else if ((state.Align & (int)FonsAlign.Right) != 0)
            {
                var width = TextBounds(x, y, str, end, out var _);
                x -= width;
            }
            else if ((state.Align & (int)FonsAlign.Center) != 0)
            {
                var _ = Array.Empty<float>();
                var width = TextBounds(x, y, str, end, out var _);
                x -= width * 0.5f;
            }

            y += GetVertAlign(font, state.Align, isize);

            for (var i = 0; i < str.Length; i++)
            {
                var c = str[i];
                if (str[i..] == end)
                {
                    break;
                }

                if (Utf8.DecUtf8(ref utf8State, ref codepoint, c) != 0)
                {
                    continue;
                }

                glyph = GetGlyph(font, codepoint, isize, iblur, FonsGlyphBitmap.Required);
                if (glyph != null)
                {
                    var q = GetQuad(font, prevGlyphIndex, glyph, scale, state.Spacing, ref x, ref y);

                    if (nverts + 6 > VertexCount)
                    {
                        Flush();
                    }

                    Vertex(q.X0, q.Y0, q.S0, q.T0, state.Colour);
                    Vertex(q.X1, q.Y1, q.S1, q.T1, state.Colour);
                    Vertex(q.X1, q.Y0, q.S1, q.T0, state.Colour);

                    Vertex(q.X0, q.Y0, q.S0, q.T0, state.Colour);
                    Vertex(q.X0, q.Y1, q.S0, q.T1, state.Colour);
                    Vertex(q.X1, q.Y1, q.S1, q.T1, state.Colour);
                }

                prevGlyphIndex = glyph != null ? glyph.Index : -1;
            }

            Flush();

            return x;
        }

        public float DrawText(float x, float y, string str)
        {
            return DrawText(x, y, str, null);
        }

        #endregion

        #region Measure Text

        public float TextBounds(float x, float y, string str, string end, out float[] bounds)
        {
            var state = GetState();
            uint codepoint = 0;
            uint utf8State = 0;
            FonsGlyph glyph = null;
            var prevGlyphIndex = -1;
            var isize = (short)(state.Size * 10.0f);
            var iblur = (short)state.Blur;
            bounds = new float[] { -1, -1, -1, -1 };

            if (state.Font < 0 || state.Font >= nfonts)
            {
                return x;
            }

            var font = fonts[state.Font];
            if (font.Data == null)
            {
                return x;
            }

            var scale = FonsTt.GetPixelHeightScale(font.Font, isize / 10.0f);

            y += GetVertAlign(font, state.Align, isize);

            float minx = x, maxx = x;
            float miny = y, maxy = y;
            var startx = x;

            for (var i = 0; i < str.Length; i++)
            {
                var c = str[i];
                if (str[i..] == end)
                {
                    break;
                }

                if (Utf8.DecUtf8(ref utf8State, ref codepoint, c) != 0)
                {
                    continue;
                }

                glyph = GetGlyph(font, codepoint, isize, iblur, FonsGlyphBitmap.Optional);
                if (glyph != null)
                {
                    var q = GetQuad(font, prevGlyphIndex, glyph, scale, state.Spacing, ref x, ref y);
                    if (q.X0 < minx)
                    {
                        minx = q.X0;
                    }

                    if (q.X1 > maxx)
                    {
                        maxx = q.X1;
                    }

                    if ((@params.Flags & (uint)FontFlags.ZeroTopLeft) != 0)
                    {
                        if (q.Y0 < miny)
                        {
                            miny = q.Y0;
                        }

                        if (q.Y1 > maxy)
                        {
                            maxy = q.Y1;
                        }
                    }
                    else
                    {
                        if (q.Y1 < miny)
                        {
                            miny = q.Y1;
                        }

                        if (q.Y0 > maxy)
                        {
                            maxy = q.Y0;
                        }
                    }
                }

                prevGlyphIndex = glyph != null ? glyph.Index : -1;
            }

            var advance = x - startx;

            if ((state.Align & (int)FonsAlign.Left) != 0)
            {
                // empty
            }
            else if ((state.Align & (int)FonsAlign.Right) != 0)
            {
                minx -= advance;
                maxx -= advance;
            }
            else if ((state.Align & (int)FonsAlign.Center) != 0)
            {
                minx -= advance * 0.5f;
                maxx -= advance * 0.5f;
            }

            bounds[0] = minx;
            bounds[1] = miny;
            bounds[2] = maxx;
            bounds[3] = maxy;

            return advance;
        }

        public void LineBounds(float y, out float miny, out float maxy)
        {
            miny = maxy = Invalid;
            var state = GetState();

            if (state.Font < 0 || state.Font >= nfonts)
            {
                return;
            }

            var font = fonts[state.Font];
            var isize = (short)(state.Size * 10.0f);
            if (font.Data == null)
            {
                return;
            }

            y += GetVertAlign(font, state.Align, isize);

            if ((@params.Flags & (byte)FontFlags.ZeroTopLeft) != 0)
            {
                miny = y - font.Ascender * isize / 10.0f;
                maxy = miny + font.Lineh * isize / 10.0f;
            }
            else
            {
                miny = y + font.Ascender * isize / 10.0f;
                maxy = miny - font.Lineh * isize / 10.0f;
            }
        }

        public void VertMetrics(out float ascender, out float descender, out float lineh)
        {
            var state = GetState();
            ascender = descender = lineh = Invalid;

            if (state.Font < 0 || state.Font >= nfonts)
            {
                return;
            }

            var font = fonts[state.Font];
            var isize = (short)(state.Size * 10.0f);
            if (font.Data == null)
            {
                return;
            }

            ascender = font.Ascender * isize / 10.0f;
            descender = font.Descender * isize / 10.0f;
            lineh = font.Lineh * isize / 10.0f;
        }

        #endregion

        #region Text iterator

        public bool TextIterInit(out FonsTextIter iter, float x, float y, string str, string end,
            FonsGlyphBitmap bitmapOption)
        {
            var state = GetState();

            iter = default;

            if (state.Font < 0 || state.Font >= nfonts)
            {
                return false;
            }

            iter.Font = fonts[state.Font];
            if (iter.Font.Data == null)
            {
                return false;
            }

            iter.Isize = (short)(state.Size * 10.0f);
            iter.Iblur = (short)state.Blur;
            iter.Scale = FonsTt.GetPixelHeightScale(iter.Font.Font, iter.Isize / 10.0f);

            if ((state.Align & (int)FonsAlign.Left) != 0)
            {
                // empty
            }
            else if ((state.Align & (int)FonsAlign.Right) != 0)
            {
                var width = TextBounds(x, y, str, end, out var _);
                x -= width;
            }
            else if ((state.Align & (int)FonsAlign.Center) != 0)
            {
                var width = TextBounds(x, y, str, end, out var _);
                x -= width * 0.5f;
            }

            y += GetVertAlign(iter.Font, state.Align, iter.Isize);

            iter.X = iter.Nextx = x;
            iter.Y = iter.Nexty = y;
            iter.Spacing = state.Spacing;
            iter.Str = str;
            iter.Next = str;
            iter.End = end;
            iter.Codepoint = 0;
            iter.PrevGlyphIndex = -1;
            iter.BitmapOption = bitmapOption;

            return true;
        }

        public bool TextIterNext(ref FonsTextIter iter, ref FonsQuad quad)
        {
            FonsGlyph glyph = null;
            var str = iter.Next;
            iter.Str = iter.Next;

            if (str.Length == 0 || str == iter.End)
            {
                return false;
            }

            int i;
            for (i = 0; i < str.Length; i++)
            {
                var c = str[i];
                if (str[i..] == iter.End)
                {
                    break;
                }

                if (Utf8.DecUtf8(ref iter.Utf8State, ref iter.Codepoint, c) != 0)
                {
                    continue;
                }

                iter.X = iter.Nextx;
                iter.Y = iter.Nexty;
                glyph = GetGlyph(iter.Font, iter.Codepoint, iter.Isize, iter.Iblur, iter.BitmapOption);
                if (glyph != null)
                {
                    quad = GetQuad(iter.Font, iter.PrevGlyphIndex, glyph, iter.Scale, iter.Spacing, ref iter.Nextx,
                        ref iter.Nexty);
                }

                iter.PrevGlyphIndex = glyph != null ? glyph.Index : Invalid;
                break;
            }

            iter.Next = str.Remove(0, i + 1);

            return true;
        }

        #endregion

        #region Pull Texture Changes

        public byte[] GetTextureData(out int width, out int height)
        {
            width = @params.Width;
            height = @params.Height;
            return texData;
        }

        public bool ValidateTexture(out int[] dirty)
        {
            dirty = new[] { -1, -1, -1, -1 };
            if (dirtyRect[0] < dirtyRect[2] && dirtyRect[1] < dirtyRect[3])
            {
                dirty[0] = dirtyRect[0];
                dirty[1] = dirtyRect[1];
                dirty[2] = dirtyRect[2];
                dirty[3] = dirtyRect[3];

                dirtyRect[0] = @params.Width;
                dirtyRect[1] = @params.Height;
                dirtyRect[2] = 0;
                dirtyRect[3] = 0;
                return true;
            }

            return false;
        }

        #endregion
    }
}