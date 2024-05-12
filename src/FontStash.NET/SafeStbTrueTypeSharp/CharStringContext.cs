using static StbTrueTypeSharp.Common;

namespace StbTrueTypeSharp
{
    public class CharStringContext
    {
        public int Bounds;
        public float FirstX;
        public float FirstY;
        public int MaxX;
        public int MaxY;
        public int MinX;
        public int MinY;
        public int NumVertices;
        public StbttVertex[] Pvertices;
        public int Started;
        public float X;
        public float Y;

        public void stbtt__track_vertex(int x, int y)
        {
            if (x > MaxX || Started == 0)
            {
                MaxX = x;
            }

            if (y > MaxY || Started == 0)
            {
                MaxY = y;
            }

            if (x < MinX || Started == 0)
            {
                MinX = x;
            }

            if (y < MinY || Started == 0)
            {
                MinY = y;
            }

            Started = 1;
        }

        public void stbtt__csctx_v(byte type, int x, int y, int cx, int cy, int cx1, int cy1)
        {
            if (Bounds != 0)
            {
                stbtt__track_vertex(x, y);
                if (type == StbttVcubic)
                {
                    stbtt__track_vertex(cx, cy);
                    stbtt__track_vertex(cx1, cy1);
                }
            }
            else
            {
                var v = new StbttVertex();
                stbtt_setvertex(ref v, type, x, y, cx, cy);
                Pvertices[NumVertices] = v;
                Pvertices[NumVertices].cx1 = (short)cx1;
                Pvertices[NumVertices].cy1 = (short)cy1;
            }

            NumVertices++;
        }

        public void stbtt__csctx_close_shape()
        {
            if (FirstX != X || FirstY != Y)
            {
                stbtt__csctx_v(StbttVline, (int)FirstX, (int)FirstY, 0, 0, 0, 0);
            }
        }

        public void stbtt__csctx_rmove_to(float dx, float dy)
        {
            stbtt__csctx_close_shape();
            FirstX = X = X + dx;
            FirstY = Y = Y + dy;
            stbtt__csctx_v(StbttVmove, (int)X, (int)Y, 0, 0, 0, 0);
        }

        public void stbtt__csctx_rline_to(float dx, float dy)
        {
            X += dx;
            Y += dy;
            stbtt__csctx_v(StbttVline, (int)X, (int)Y, 0, 0, 0, 0);
        }

        public void stbtt__csctx_rccurve_to(float dx1, float dy1, float dx2, float dy2,
            float dx3, float dy3)
        {
            var cx1 = X + dx1;
            var cy1 = Y + dy1;
            var cx2 = cx1 + dx2;
            var cy2 = cy1 + dy2;
            X = cx2 + dx3;
            Y = cy2 + dy3;
            stbtt__csctx_v(StbttVcubic, (int)X, (int)Y, (int)cx1, (int)cy1, (int)cx2, (int)cy2);
        }
    }
}