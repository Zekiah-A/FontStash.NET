using Silk.NET.OpenGL.Legacy;
using System;

namespace FontStash.NET.GL.Legacy;

public sealed class GlFontLegacy : IDisposable
{
    private readonly Silk.NET.OpenGL.Legacy.GL gl;

    private FontManager fons;
    private uint tex;
    private int width, height;

    public GlFontLegacy(Silk.NET.OpenGL.Legacy.GL gl)
    {
        this.gl = gl;
    }

    private unsafe bool RenderCreate(int width, int height)
    {
        if (tex != 0)
        {
            gl.DeleteTexture(tex);
            tex = 0;
        }
        tex = gl.GenTexture();
        if (tex == 0)
            return false;
        this.width = width;
        this.height = height;
        gl.BindTexture(TextureTarget.Texture2D, tex);
        gl.TexImage2D(TextureTarget.Texture2D, 0, (int)GLEnum.Alpha, (uint)this.width, (uint)this.height, 0, GLEnum.Alpha, GLEnum.UnsignedByte, (void*)0);
        gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        return true;
    }

    private bool RenderResize(int width, int height)
    {
        return RenderCreate(width, height);
    }

    private unsafe void RenderUpdate(int[] rect, byte[] data)
    {
        int w = rect[2] - rect[0];
        int h = rect[3] - rect[1];

        if (tex == 0)
            return;
        gl.PushClientAttrib((uint)ClientAttribMask.ClientPixelStoreBit);
        gl.BindTexture(TextureTarget.Texture2D, tex);
        gl.PixelStore(PixelStoreParameter.UnpackAlignment, 1);
        gl.PixelStore(PixelStoreParameter.UnpackRowLength, width);
        gl.PixelStore(PixelStoreParameter.UnpackSkipPixels, rect[0]);
        gl.PixelStore(PixelStoreParameter.UnpackSkipRows, rect[1]);
        fixed (byte* d = data)
        {
            gl.TexSubImage2D(TextureTarget.Texture2D, 0, rect[0], rect[1], (uint)w, (uint)h, GLEnum.Alpha, GLEnum.UnsignedByte, d);
        }
        gl.PopClientAttrib();
    }

    private unsafe void RenderDraw(float[] verts, float[] tcoords, uint[] colours, int nverts)
    {
        gl.BindTexture(TextureTarget.Texture2D, tex);
        gl.Enable(EnableCap.Texture2D);
        gl.EnableClientState(EnableCap.VertexArray);
        gl.EnableClientState(EnableCap.TextureCoordArray);
        gl.EnableClientState(EnableCap.ColorArray);

        fixed (float* d = verts)
        {
            gl.VertexPointer(2, GLEnum.Float, sizeof(float) * 2, d);
        }
        fixed (float* tc = tcoords)
        {
            gl.TexCoordPointer(2, GLEnum.Float, sizeof(float) * 2, tc);
        }
        fixed (uint* col = colours)
        {
            gl.ColorPointer(4, GLEnum.UnsignedByte, sizeof(uint), col);
        }

        gl.DrawArrays(PrimitiveType.Triangles, 0, (uint)nverts);

        gl.Disable(EnableCap.Texture2D);
        gl.DisableClientState(EnableCap.VertexArray);
        gl.DisableClientState(EnableCap.TextureCoordArray);
        gl.DisableClientState(EnableCap.ColorArray);
    }

    private void RenderDelete()
    {
        if (tex != 0)
            gl.DeleteTexture(tex);
        tex = 0;
    }

    public FontManager Create(int width, int height, FontFlags flags)
    {
        FontParams prams = new()
        {
            Width = width,
            Height = height,
            Flags = (byte)flags,
            RenderCreate = RenderCreate,
            RenderResize = RenderResize,
            RenderUpdate = RenderUpdate,
            RenderDraw = RenderDraw,
            RenderDelete = RenderDelete
        };

        fons = new FontManager(prams);
        return fons;
    }

    public void Dispose()
    {
        fons.Dispose();
    }

    public static uint Rgba(byte r, byte g, byte b, byte a)
    {
        return (uint)((r) | (g << 8) | (b << 16) | (a << 24));
    }

}