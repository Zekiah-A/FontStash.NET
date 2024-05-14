using Silk.NET.OpenGL;
using System;

namespace FontStash.NET.GL
{
    public sealed class GlFont : IDisposable
    {
        private const int VertexAttrib = 0;
        private const int TcoordAttrib = 1;
        private const int ColourAttrib = 2;

        private uint tex;
        private int width, height;
        private uint vertexArray;
        private uint vertexBuffer;
        private uint tcoordBuffer;
        private uint colourBuffer;
        private FontManager fs;

        private readonly Silk.NET.OpenGL.GL gl;

        public GlFont(Silk.NET.OpenGL.GL gl)
        {
            this.gl = gl;
        }

        private unsafe bool RenderCreate(int width, int height)
        {
            if (tex == 0)
            {
                gl.DeleteTexture(tex);
                tex = 0;
            }
            tex = gl.GenTexture();
            if (tex == 0)
            {
                return false;
            }

            if (vertexArray == 0)
            {
                vertexArray = gl.GenVertexArray();
            }

            if (vertexArray == 0)
            {
                return false;
            }

            gl.BindVertexArray(vertexArray);

            if (vertexBuffer == 0)
            {
                vertexBuffer = gl.GenBuffer();
            }

            if (vertexBuffer == 0)
            {
                return false;
            }

            if (tcoordBuffer == 0)
            {
                tcoordBuffer = gl.GenBuffer();
            }

            if (tcoordBuffer == 0)
            {
                return false;
            }

            if (colourBuffer == 0)
            {
                colourBuffer = gl.GenBuffer();
            }

            if (colourBuffer == 0)
            {
                return false;
            }

            this.width = width;
            this.height = height;
            gl.BindTexture(TextureTarget.Texture2D, tex);
            gl.TexImage2D(TextureTarget.Texture2D, 0, (int)InternalFormat.Red, (uint)this.width, (uint)this.height, 0, GLEnum.Red, GLEnum.UnsignedByte, null);
            gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            int[] swizzleRgbaParams = { (int)GLEnum.One, (int)GLEnum.One, (int)GLEnum.One, (int)GLEnum.Red };
            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureSwizzleRgba, swizzleRgbaParams);

            return true;
        }

        private bool RenderResize(int width, int height)
        {
            return RenderCreate(width, height);
        }

        private unsafe void RenderUpdate(int[] rect, byte[] data)
        {
            var w = rect[2] - rect[0];
            var h = rect[3] - rect[1];

            if (tex == 0)
            {
                return;
            }

            var alignement = gl.GetInteger(GLEnum.UnpackAlignment);
            var rowLength = gl.GetInteger(GLEnum.UnpackRowLength);
            var skipPixels = gl.GetInteger(GLEnum.UnpackSkipPixels);
            var skipRows = gl.GetInteger(GLEnum.UnpackSkipRows);

            gl.BindTexture(TextureTarget.Texture2D, tex);

            gl.PixelStore(PixelStoreParameter.UnpackAlignment, 1);
            gl.PixelStore(PixelStoreParameter.UnpackRowLength, width);
            gl.PixelStore(PixelStoreParameter.UnpackSkipPixels, rect[0]);
            gl.PixelStore(PixelStoreParameter.UnpackSkipRows, rect[1]);

            fixed (byte* d = data)
            {
                gl.TexSubImage2D(GLEnum.Texture2D, 0, rect[0], rect[1], (uint)w, (uint)h, GLEnum.Red, GLEnum.UnsignedByte, d);
            }

            gl.PixelStore(PixelStoreParameter.UnpackAlignment, alignement);
            gl.PixelStore(PixelStoreParameter.UnpackRowLength, rowLength);
            gl.PixelStore(PixelStoreParameter.UnpackSkipPixels, skipPixels);
            gl.PixelStore(PixelStoreParameter.UnpackSkipRows, skipRows);
        }

        private unsafe void RenderDraw(float[] verts, float[] tcoords, uint[] colours, int nverts)
        {
            if (tex == 0 || vertexArray == 0)
            {
                return;
            }

            gl.ActiveTexture(TextureUnit.Texture0);
            gl.BindTexture(TextureTarget.Texture2D, tex);

            gl.BindVertexArray(vertexArray);

            gl.EnableVertexAttribArray(VertexAttrib);
            gl.BindBuffer(BufferTargetARB.ArrayBuffer, vertexBuffer);
            fixed (float* d = verts)
            {
                gl.BufferData(BufferTargetARB.ArrayBuffer, (uint)(nverts * 2 * sizeof(float)), d, BufferUsageARB.DynamicDraw);
            }
            gl.VertexAttribPointer(VertexAttrib, 2, GLEnum.Float, false, 0, null);

            gl.EnableVertexAttribArray(TcoordAttrib);
            gl.BindBuffer(BufferTargetARB.ArrayBuffer, tcoordBuffer);
            fixed (float* d = tcoords)
            {
                gl.BufferData(BufferTargetARB.ArrayBuffer, (uint)(nverts * 2 * sizeof(float)), d, BufferUsageARB.DynamicDraw);
            }
            gl.VertexAttribPointer(TcoordAttrib, 2, GLEnum.Float, false, 0, null);

            gl.EnableVertexAttribArray(ColourAttrib);
            gl.BindBuffer(BufferTargetARB.ArrayBuffer, colourBuffer);
            fixed (uint* d = colours)
            {
                gl.BufferData(BufferTargetARB.ArrayBuffer, (uint)(nverts * sizeof(uint)), d, BufferUsageARB.DynamicDraw);
            }
            gl.VertexAttribPointer(ColourAttrib, 4, GLEnum.UnsignedByte, false, 0, null);

            gl.DrawArrays(PrimitiveType.Triangles, 0, (uint)nverts);
            
            gl.DisableVertexAttribArray(VertexAttrib);
            gl.DisableVertexAttribArray(TcoordAttrib);
            gl.DisableVertexAttribArray(ColourAttrib);

            gl.BindVertexArray(0);
        }

        private void RenderDelete()
        {
            if (tex != 0)
            {
                gl.DeleteTexture(tex);
                tex = 0;
            }

            gl.BindVertexArray(0);

            if (vertexBuffer != 0)
            {
                gl.DeleteBuffer(vertexBuffer);
                vertexBuffer = 0;
            }
            if (-tcoordBuffer != 0)
            {
                gl.DeleteBuffer(tcoordBuffer);
                tcoordBuffer = 0;
            }
            if (colourBuffer != 0)
            {
                gl.DeleteBuffer(colourBuffer);
                colourBuffer = 0;
            }
            if (vertexArray != 0)
            {
                gl.DeleteVertexArray(vertexArray);
                vertexArray = 0;
            }
        }

        public FontManager Create(int width, int height, FontFlags flags)
        {
            FontParams prams = default;
            prams.Width = width;
            prams.Height = height;
            prams.Flags = (byte)flags;
            prams.RenderCreate = RenderCreate;
            prams.RenderResize = RenderResize;
            prams.RenderUpdate = RenderUpdate;
            prams.RenderDraw = RenderDraw;
            prams.RenderDelete = RenderDelete;

            fs = new FontManager(prams);
            return fs;
        }

        public void Dispose()
        {
            fs.Dispose();
        }
    }
}
