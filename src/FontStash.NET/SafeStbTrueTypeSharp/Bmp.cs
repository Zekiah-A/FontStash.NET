using System;
using static StbTrueTypeSharp.Common;

namespace StbTrueTypeSharp
{
    public class Bitmap
    {
        public int H;
        public FakePtr<byte> Pixels;
        public int Stride;
        public int W;

        public static void stbtt__handle_clipped_edge(float[] scanline, int offset, int x, StbttActiveEdge e, float x0,
            float y0, float x1, float y1)
        {
            if (y0 == y1)
            {
                return;
            }

            if (y0 > e.Ey)
            {
                return;
            }

            if (y1 < e.Sy)
            {
                return;
            }

            if (y0 < e.Sy)
            {
                x0 += (x1 - x0) * (e.Sy - y0) / (y1 - y0);
                y0 = e.Sy;
            }

            if (y1 > e.Ey)
            {
                x1 += (x1 - x0) * (e.Ey - y1) / (y1 - y0);
                y1 = e.Ey;
            }

            if (x0 <= x && x1 <= x)
            {
                scanline[x + offset] += e.Direction * (y1 - y0);
            }
            else if (x0 >= x + 1 && x1 >= x + 1)
            {
            }
            else
            {
                scanline[x + offset] += e.Direction * (y1 - y0) * (1 - (x0 - x + (x1 - x)) / 2);
            }
        }

        public static void stbtt__fill_active_edges_new(float[] scanline, int scanlineFill, int len,
            StbttActiveEdge e, float yTop)
        {
            var yBottom = yTop + 1;
            while (e != null)
            {
                if (e.Fdx == 0)
                {
                    var x0 = e.Fx;
                    if (x0 < len)
                    {
                        if (x0 >= 0)
                        {
                            stbtt__handle_clipped_edge(scanline, 0, (int)x0, e, x0, yTop, x0, yBottom);
                            stbtt__handle_clipped_edge(scanline, scanlineFill - 1, (int)x0 + 1, e, x0, yTop, x0,
                                yBottom);
                        }
                        else
                        {
                            stbtt__handle_clipped_edge(scanline, scanlineFill - 1, 0, e, x0, yTop, x0, yBottom);
                        }
                    }
                }
                else
                {
                    var x0 = e.Fx;
                    var dx = e.Fdx;
                    var xb = x0 + dx;
                    float xTop = 0;
                    float xBottom = 0;
                    float sy0 = 0;
                    float sy1 = 0;
                    var dy = e.Fdy;
                    if (e.Sy > yTop)
                    {
                        xTop = x0 + dx * (e.Sy - yTop);
                        sy0 = e.Sy;
                    }
                    else
                    {
                        xTop = x0;
                        sy0 = yTop;
                    }

                    if (e.Ey < yBottom)
                    {
                        xBottom = x0 + dx * (e.Ey - yTop);
                        sy1 = e.Ey;
                    }
                    else
                    {
                        xBottom = xb;
                        sy1 = yBottom;
                    }

                    if (xTop >= 0 && xBottom >= 0 && xTop < len && xBottom < len)
                    {
                        if ((int)xTop == (int)xBottom)
                        {
                            float height = 0;
                            var x = (int)xTop;
                            height = sy1 - sy0;
                            scanline[x] += e.Direction * (1 - (xTop - x + (xBottom - x)) / 2) * height;
                            scanline[x + scanlineFill] += e.Direction * height;
                        }
                        else
                        {
                            var x = 0;
                            var x1 = 0;
                            var x2 = 0;
                            float yCrossing = 0;
                            float step = 0;
                            float sign = 0;
                            float area = 0;
                            if (xTop > xBottom)
                            {
                                float t = 0;
                                sy0 = yBottom - (sy0 - yTop);
                                sy1 = yBottom - (sy1 - yTop);
                                t = sy0;
                                sy0 = sy1;
                                sy1 = t;
                                t = xBottom;
                                xBottom = xTop;
                                xTop = t;
                                dx = -dx;
                                dy = -dy;
                                t = x0;
                                x0 = xb;
                                xb = t;
                            }

                            x1 = (int)xTop;
                            x2 = (int)xBottom;
                            yCrossing = (x1 + 1 - x0) * dy + yTop;
                            sign = e.Direction;
                            area = sign * (yCrossing - sy0);
                            scanline[x1] += area * (1 - (xTop - x1 + (x1 + 1 - x1)) / 2);
                            step = sign * dy;
                            for (x = x1 + 1; x < x2; ++x)
                            {
                                scanline[x] += area + step / 2;
                                area += step;
                            }

                            yCrossing += dy * (x2 - (x1 + 1));
                            scanline[x2] += area + sign * (1 - (x2 - x2 + (xBottom - x2)) / 2) * (sy1 - yCrossing);
                            scanline[x2 + scanlineFill] += sign * (sy1 - sy0);
                        }
                    }
                    else
                    {
                        var x = 0;
                        for (x = 0; x < len; ++x)
                        {
                            var y0 = yTop;
                            var x1 = (float)x;
                            var x2 = (float)(x + 1);
                            var x3 = xb;
                            var y3 = yBottom;
                            var y1 = (x - x0) / dx + yTop;
                            var y2 = (x + 1 - x0) / dx + yTop;
                            if (x0 < x1 && x3 > x2)
                            {
                                stbtt__handle_clipped_edge(scanline, 0, x, e, x0, y0, x1, y1);
                                stbtt__handle_clipped_edge(scanline, 0, x, e, x1, y1, x2, y2);
                                stbtt__handle_clipped_edge(scanline, 0, x, e, x2, y2, x3, y3);
                            }
                            else if (x3 < x1 && x0 > x2)
                            {
                                stbtt__handle_clipped_edge(scanline, 0, x, e, x0, y0, x2, y2);
                                stbtt__handle_clipped_edge(scanline, 0, x, e, x2, y2, x1, y1);
                                stbtt__handle_clipped_edge(scanline, 0, x, e, x1, y1, x3, y3);
                            }
                            else if (x0 < x1 && x3 > x1)
                            {
                                stbtt__handle_clipped_edge(scanline, 0, x, e, x0, y0, x1, y1);
                                stbtt__handle_clipped_edge(scanline, 0, x, e, x1, y1, x3, y3);
                            }
                            else if (x3 < x1 && x0 > x1)
                            {
                                stbtt__handle_clipped_edge(scanline, 0, x, e, x0, y0, x1, y1);
                                stbtt__handle_clipped_edge(scanline, 0, x, e, x1, y1, x3, y3);
                            }
                            else if (x0 < x2 && x3 > x2)
                            {
                                stbtt__handle_clipped_edge(scanline, 0, x, e, x0, y0, x2, y2);
                                stbtt__handle_clipped_edge(scanline, 0, x, e, x2, y2, x3, y3);
                            }
                            else if (x3 < x2 && x0 > x2)
                            {
                                stbtt__handle_clipped_edge(scanline, 0, x, e, x0, y0, x2, y2);
                                stbtt__handle_clipped_edge(scanline, 0, x, e, x2, y2, x3, y3);
                            }
                            else
                            {
                                stbtt__handle_clipped_edge(scanline, 0, x, e, x0, y0, x3, y3);
                            }
                        }
                    }
                }

                e = e.Next;
            }
        }

        public void stbtt__rasterize_sorted_edges(FakePtr<StbttEdge> e, int n, int vsubsample, int offX, int offY)
        {
            var active = new SimpleHolder<StbttActiveEdge>(null);
            var y = 0;
            var j = 0;
            var i = 0;
            float[] scanline;
            if (W > 64)
            {
                scanline = new float[W * 2 + 1];
            }
            else
            {
                scanline = new float[129];
            }

            var scanline2 = W;
            y = offY;
            e[n].Y0 = (float)(offY + H) + 1;
            while (j < H)
            {
                var scanYTop = y + 0.0f;
                var scanYBottom = y + 1.0f;
                IHolder<StbttActiveEdge> step = active;

                Array.Clear(scanline, 0, W);
                Array.Clear(scanline, scanline2, W + 1);

                while (step.Value != null)
                {
                    var z = step.Value;
                    if (z.Ey <= scanYTop)
                    {
                        // In original code `step` had pointer to pointer type(stbtt__active_edge **)
                        // So `step.Value = z.next`(originally `*step = z.next`) was actually setting to z.next 
                        // whatever `step` was pointing to
                        // So this whole complicated logic starting with IHolder<T> is required to reproduce that behavior
                        step.Value = z.Next;
                        z.Direction = 0;
                    }
                    else
                    {
                        step = new ActiveEdgeNext(step.Value);
                    }
                }

                while (e.Value.Y0 <= scanYBottom)
                {
                    if (e.Value.Y0 != e.Value.Y1)
                    {
                        var z = stbtt__new_active(e.Value, offX, scanYTop);
                        if (z != null)
                        {
                            if (j == 0 && offY != 0)
                            {
                                if (z.Ey < scanYTop)
                                {
                                    z.Ey = scanYTop;
                                }
                            }

                            z.Next = active.Value;
                            active.Value = z;
                        }
                    }

                    ++e;
                }

                if (active.Value != null)
                {
                    stbtt__fill_active_edges_new(scanline, scanline2 + 1, W, active.Value, scanYTop);
                }

                {
                    var sum = (float)0;
                    for (i = 0; i < W; ++i)
                    {
                        float k = 0;
                        var m = 0;
                        sum += scanline[scanline2 + i];
                        k = scanline[i] + sum;
                        k = (float)Math.Abs((double)k) * 255 + 0.5f;
                        m = (int)k;
                        if (m > 255)
                        {
                            m = 255;
                        }

                        Pixels[j * Stride + i] = (byte)m;
                    }
                }
                step = active;
                while (step.Value != null)
                {
                    var z = step.Value;
                    z.Fx += z.Fdx;
                    step = new ActiveEdgeNext(step.Value);
                }

                ++y;
                ++j;
            }
        }

        public void stbtt__rasterize(StbttPoint[] pts, int[] wcount, int windings,
            float scaleX, float scaleY, float shiftX, float shiftY, int offX, int offY, int invert)
        {
            var yScaleInv = invert != 0 ? -scaleY : scaleY;
            var n = 0;
            var i = 0;
            var j = 0;
            var k = 0;
            var m = 0;
            var vsubsample = 1;
            n = 0;
            for (i = 0; i < windings; ++i)
                n += wcount[i];
            var e = new StbttEdge[n + 1];
            for (i = 0; i < e.Length; ++i)
                e[i] = new StbttEdge();
            n = 0;
            m = 0;
            for (i = 0; i < windings; ++i)
            {
                var p = new FakePtr<StbttPoint>(pts, m);
                m += wcount[i];
                j = wcount[i] - 1;
                for (k = 0; k < wcount[i]; j = k++)
                {
                    var a = k;
                    var b = j;
                    if (p[j].y == p[k].y)
                    {
                        continue;
                    }

                    e[n].Invert = 0;
                    if ((invert != 0 && p[j].y > p[k].y) || (invert == 0 && p[j].y < p[k].y))
                    {
                        e[n].Invert = 1;
                        a = j;
                        b = k;
                    }

                    e[n].X0 = p[a].x * scaleX + shiftX;
                    e[n].Y0 = (p[a].y * yScaleInv + shiftY) * vsubsample;
                    e[n].X1 = p[b].x * scaleX + shiftX;
                    e[n].Y1 = (p[b].y * yScaleInv + shiftY) * vsubsample;
                    ++n;
                }
            }

            var ptr = new FakePtr<StbttEdge>(e);
            stbtt__sort_edges(ptr, n);
            stbtt__rasterize_sorted_edges(ptr, n, vsubsample, offX, offY);
        }

        public void stbtt_Rasterize(float flatnessInPixels, StbttVertex[] vertices,
            int numVerts, float scaleX, float scaleY, float shiftX, float shiftY, int xOff, int yOff, int invert)
        {
            var scale = scaleX > scaleY ? scaleY : scaleX;
            var windingCount = 0;
            int[] windingLengths = null;
            var windings = stbtt_FlattenCurves(vertices, numVerts, flatnessInPixels / scale, out windingLengths,
                out windingCount);
            if (windings != null)
            {
                stbtt__rasterize(windings, windingLengths, windingCount, scaleX, scaleY, shiftX, shiftY,
                    xOff, yOff, invert);
            }
        }

        private interface IHolder<T>
        {
            T Value { get; set; }
        }

        private struct SimpleHolder<T> : IHolder<T>
        {
            public T Value { get; set; }

            public SimpleHolder(T val)
            {
                Value = val;
            }
        }

        private struct ActiveEdgeNext : IHolder<StbttActiveEdge>
        {
            private readonly StbttActiveEdge p;

            public StbttActiveEdge Value
            {
                get => p.Next;

                set => p.Next = value;
            }

            public ActiveEdgeNext(StbttActiveEdge p)
            {
                this.p = p;
            }
        }
    }
}