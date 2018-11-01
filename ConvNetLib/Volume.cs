using System;

namespace ConvNetLib
{
    public class Volume
    {
        public double[] Dw;
        public double[] W;
        public int Sx;
        public int Sy;
        public int Depth;

        public Volume()
        {

        }

        public void addFrom(Volume v)
        {
            for (var k = 0; k < this.W.Length; k++)
            {
                this.W[k] += v.W[k];
            }
        }

        public Volume(int sx, int sy, int outDepth, double d)
        {
            Sx = sx;
            Sy = sy;
            Depth = outDepth;
            var n = sx * sy * outDepth;

            W = new double[n];
            Dw = new double[n];

            for (var i = 0; i < n; i++)
            {
                this.W[i] = d;
            }


        }
        public static Volume Augment(Volume V, int crop, int? dx = null, int? dy = null, bool? fliplr = null)
        {
            // note assumes square outputs of size crop x crop
            if (fliplr == null) fliplr = false;
            if (dx == null) dx = (int)NetStuff.randi(0, V.Sx - crop);
            if (dy == null) dy = (int)NetStuff.randi(0, V.Sy - crop);

            // randomly sample a crop in the input volume
            Volume W = null;
            if (crop != V.Sx || dx != 0 || dy != 0)
            {
                W = new Volume(crop, crop, V.Depth, 0.0);
                for (var x = 0; x < crop; x++)
                {
                    for (var y = 0; y < crop; y++)
                    {
                        if (x + dx < 0 || x + dx >= V.Sx || y + dy < 0 || y + dy >= V.Sy) continue; // oob
                        for (var d = 0; d < V.Depth; d++)
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
                for (var x = 0; x < W.Sx; x++)
                {
                    for (var y = 0; y < W.Sy; y++)
                    {
                        for (var d = 0; d < W.Depth; d++)
                        {
                            W2.Set(x, y, d, W.Get(W.Sx - x - 1, y, d)); // copy data over
                        }
                    }
                }
                W = W2; //swap
            }
            return W;
        }

        private Volume cloneAndZero()
        {
            return new Volume(this.Sx, this.Sy, this.Depth, 0.0);
        }

        public Volume(int sx, int sy, int outDepth)
        {
            Sx = sx;
            Sy = sy;
            Depth = outDepth;
            var n = sx * sy * outDepth;

            W = new double[n];
            Dw = new double[n];

            var scale = Math.Sqrt(1.0 / (sx * sy * outDepth));
            for (var i = 0; i < n; i++)
            {
                this.W[i] = NetStuff.randn(0.0, scale);
            }


        }

        public double Get(int x, int y, int d)
        {
            var ix = ((this.Sx * y) + x) * this.Depth + d;
            return this.W[ix];
        }

        public void Set(int x, int y, int d, double v)
        {
            var ix = ((this.Sx * y) + x) * this.Depth + d;
            this.W[ix] = v;
        }

        public void Add(int x, int y, int d, double v)
        {
            var ix = ((this.Sx * y) + x) * this.Depth + d;
            this.W[ix] += v;
        }
        public double get_grad(int x, int y, int d)
        {
            var ix = ((this.Sx * y) + x) * this.Depth + d;
            return this.Dw[ix];
        }

        public void set_grad(int x, int y, int d, double v)
        {
            var ix = ((this.Sx * y) + x) * this.Depth + d;
            this.Dw[ix] = v;
        }

        public void add_grad(int x, int y, int d, double v)
        {
            var ix = ((this.Sx * y) + x) * this.Depth + d;
            this.Dw[ix] += v;
        }

        public Volume Clone()
        {
            var V = new Volume(this.Sx, this.Sy, this.Depth, 0.0);
            var n = this.W.Length;
            for (var i = 0; i < n; i++) { V.W[i] = this.W[i]; }
            return V;
        }
    }
}