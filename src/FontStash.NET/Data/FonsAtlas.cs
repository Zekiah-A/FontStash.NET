using System;

namespace FontStash.NET
{
    internal class FonsAtlas
    {
        public int Cnodes;
        public int Height;
        public int Nnodes;
        public FonsAtlasNode[] Nodes;
        private int width;

        public FonsAtlas(int w, int h, int nnodes)
        {
            width = w;
            Height = h;

            Nodes = new FonsAtlasNode[nnodes];
            Nnodes = 0;
            Cnodes = nnodes;

            Nodes[0].X = 0;
            Nodes[0].Y = 0;
            Nodes[0].Width = (short)w;
            Nnodes++;
        }

        public bool InsertNode(int idx, int x, int y, int w)
        {
            if (Nnodes + 1 > Cnodes)
            {
                Cnodes = Cnodes == 0 ? 8 : Cnodes * 2;
                try
                {
                    Array.Resize(ref Nodes, Cnodes);
                }
                catch
                {
                    return false;
                }
            }

            for (var i = Nnodes; i > idx; i--)
                Nodes[i] = Nodes[i - 1];
            Nodes[idx].X = (short)x;
            Nodes[idx].Y = (short)y;
            Nodes[idx].Width = (short)w;
            Nnodes++;
            return true;
        }

        public void RemoveNode(int idx)
        {
            if (Nnodes == 0)
            {
                return;
            }

            for (var i = idx; i < Nnodes - 1; i++) Nodes[i] = Nodes[i + 1];
            Nnodes--;
        }

        public void Expand(int w, int h)
        {
            if (w > width)
            {
                InsertNode(Nnodes, width, 0, w - width);
            }

            width = w;
            Height = h;
        }

        public void Reset(int w, int h)
        {
            width = w;
            Height = h;
            Nnodes = 0;

            Nodes[0].X = 0;
            Nodes[0].Y = 0;
            Nodes[0].Width = (short)w;
            Nnodes++;
        }

        public bool AddSkylineLevel(int idx, int x, int y, int w, int h)
        {
            if (!InsertNode(idx, x, y + h, w))
            {
                return false;
            }

            for (var i = idx + 1; i < Nnodes; i++)
                if (Nodes[i].X < Nodes[i - 1].X + Nodes[i - 1].Width)
                {
                    var shrink = Nodes[i - 1].X + Nodes[i - 1].Width - Nodes[i].X;
                    Nodes[i].X += (short)shrink;
                    Nodes[i].Width -= (short)shrink;
                    if (Nodes[i].Width <= 0)
                    {
                        RemoveNode(i);
                        i--;
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }

            for (var i = 0; i < Nnodes - 1; i++)
                if (Nodes[i].Y == Nodes[i + 1].Y)
                {
                    Nodes[i].Width += Nodes[i + 1].Width;
                    RemoveNode(i + 1);
                    i--;
                }

            return true;
        }

        public int RectFits(int i, int w, int h)
        {
            int x = Nodes[i].X;
            int y = Nodes[i].Y;
            if (x + w > width)
            {
                return -1;
            }

            var spaceLeft = w;
            while (spaceLeft > 0)
            {
                if (i == Nnodes)
                {
                    return FontManager.Invalid;
                }

                y = Math.Max(y, Nodes[i].Y);
                if (y + h > Height)
                {
                    return FontManager.Invalid;
                }

                spaceLeft -= Nodes[i].Width;
                i++;
            }

            return y;
        }

        public bool AddRect(int rw, int rh, ref int rx, ref int ry)
        {
            int besth = Height, bestw = width, besti = FontManager.Invalid;
            int bestx = -1, besty = -1;

            for (var i = 0; i < Nnodes; i++)
            {
                var y = RectFits(i, rw, rh);
                if (y != -1)
                {
                    if (y + rh < besth || (y + rh == besth && Nodes[i].Width < bestw))
                    {
                        besti = i;
                        bestw = Nodes[i].Width;
                        besth = y + rh;
                        bestx = Nodes[i].X;
                        besty = y;
                    }
                }
            }

            if (besti == FontManager.Invalid)
            {
                return false;
            }

            if (!AddSkylineLevel(besti, bestx, besty, rw, rh))
            {
                return false;
            }

            rx = bestx;
            ry = besty;

            return true;
        }
    }
}