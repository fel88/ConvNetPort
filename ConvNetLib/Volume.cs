using System;

namespace ConvNetLib
{
    public class Volume
    {
        public double[] dw;

        public double[] w;
        public int sx;
        public int sy;
        public int depth;

        public Volume()
        {

        }

        public void addFrom(Volume v)
        {
            for (var k = 0; k < this.w.Length; k++)
            {
                this.w[k] += v.w[k];
            }
        }

        public Volume(int _sx, int _sy, int outDepth, double d)
        {
            sx = _sx;
            sy = _sy;
            depth = outDepth;
            var n = sx * sy * outDepth;

            w = new double[n];
            dw = new double[n];

            for (var i = 0; i < n; i++)
            {
                this.w[i] = d;
            }


        }
        public static Volume Augment(Volume V, int crop, int? dx = null, int? dy = null, bool? fliplr = null)
        {
            // note assumes square outputs of size crop x crop
            if (fliplr == null) fliplr = false;
            if (dx == null) dx = (int)NetStuff.randi(0, V.sx - crop);
            if (dy == null) dy = (int)NetStuff.randi(0, V.sy - crop);

            // randomly sample a crop in the input volume
            Volume W = null;
            if (crop != V.sx || dx != 0 || dy != 0)
            {
                W = new Volume(crop, crop, V.depth, 0.0);
                for (var x = 0; x < crop; x++)
                {
                    for (var y = 0; y < crop; y++)
                    {
                        if (x + dx < 0 || x + dx >= V.sx || y + dy < 0 || y + dy >= V.sy) continue; // oob
                        for (var d = 0; d < V.depth; d++)
                        {
                            W.Set(x, y, d, V.Get(x + dx.Value, y + dy.Value, d)); // copy data over
                        }
                    }
                }
            }
            else
            {
                W = V;
            }

            if (fliplr.Value)
            {
                // flip volume horziontally
                var W2 = W.cloneAndZero();
                for (var x = 0; x < W.sx; x++)
                {
                    for (var y = 0; y < W.sy; y++)
                    {
                        for (var d = 0; d < W.depth; d++)
                        {
                            W2.Set(x, y, d, W.Get(W.sx - x - 1, y, d)); // copy data over
                        }
                    }
                }
                W = W2; //swap
            }
            return W;
        }

        public Volume cloneAndZero()
        {
            return new Volume(this.sx, this.sy, this.depth, 0.0);
        }

        public Volume(int _sx, int _sy, int outDepth)
        {
            sx = _sx;
            sy = _sy;
            depth = outDepth;
            var n = sx * sy * outDepth;

            w = new double[n];
            dw = new double[n];

            var scale = Math.Sqrt(1.0 / (sx * sy * outDepth));
            for (var i = 0; i < n; i++)
            {
                this.w[i] = NetStuff.randn(0.0, scale);
            }
        }

        public double Get(int x, int y, int d)
        {
            var ix = ((this.sx * y) + x) * this.depth + d;
            return this.w[ix];
        }

        public void Set(int x, int y, int d, double v)
        {
            var ix = ((this.sx * y) + x) * this.depth + d;
            this.w[ix] = v;
        }

        public void Add(int x, int y, int d, double v)
        {
            var ix = ((this.sx * y) + x) * this.depth + d;
            this.w[ix] += v;
        }
        public double get_grad(int x, int y, int d)
        {
            var ix = ((this.sx * y) + x) * this.depth + d;
            return this.dw[ix];
        }

        public void set_grad(int x, int y, int d, double v)
        {
            var ix = ((this.sx * y) + x) * this.depth + d;
            this.dw[ix] = v;
        }

        public void add_grad(int x, int y, int d, double v)
        {
            var ix = ((this.sx * y) + x) * this.depth + d;
            this.dw[ix] += v;
        }

        public Volume Clone()
        {
            var V = new Volume(this.sx, this.sy, this.depth, 0.0);
            var n = this.w.Length;
            for (var i = 0; i < n; i++) { V.w[i] = this.w[i]; }
            return V;
        }

        public void fromJSON(dynamic json)
        {
            this.sx = json["sx"];
            this.sy = json["sy"];
            this.depth = json["depth"];

            var n = this.sx * this.sy * this.depth;
            this.w = new double[n];
            this.dw = new double[n];
            // copy over the elements.
            for (var i = 0; i < n; i++)
            {
                this.w[i] = (double)json["w"][i+""];
            }
        }
    }
}