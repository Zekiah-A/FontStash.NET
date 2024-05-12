namespace FontStash.NET
{
    public delegate bool RenderCreate(int width, int height);

    public delegate bool RenderResize(int width, int height);

    public delegate void RenderUpdate(int[] rect, byte[] data);

    public delegate void RenderDraw(float[] verts, float[] tcoords, uint[] colours, int nverts);

    public delegate void RenderDelete();

    public struct FontParams
    {
        public int Width;
        public int Height;

        public byte Flags;

        public RenderCreate RenderCreate;
        public RenderResize RenderResize;
        public RenderUpdate RenderUpdate;
        public RenderDraw RenderDraw;
        public RenderDelete RenderDelete;
    }
}