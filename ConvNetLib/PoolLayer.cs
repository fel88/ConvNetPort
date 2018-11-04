using System;
using System.Linq;
using System.Xml.Linq;

namespace ConvNetLib
{
    public class PoolLayer : Layer
    {
        

        public override void Init()
        {

            /*
                 *   var opt = opt || {};

        // required
        this.sx = opt.sx; // filter size
        this.in_depth = opt.in_depth;
        this.in_sx = opt.in_sx;
        this.in_sy = opt.in_sy;

        // optional
        this.sy = typeof opt.sy !== 'undefined' ? opt.sy : this.sx;
        this.stride = typeof opt.stride !== 'undefined' ? opt.stride : 2;
        this.pad = typeof opt.pad !== 'undefined' ? opt.pad : 0; // amount of 0 padding to add around borders of input volume

        // computed
        this.out_depth = this.in_depth;
        this.out_sx = Math.floor((this.in_sx + this.pad * 2 - this.sx) / this.stride + 1);
        this.out_sy = Math.floor((this.in_sy + this.pad * 2 - this.sy) / this.stride + 1);
        this.layer_type = 'pool';
        // store switches for x,y coordinates for where the max comes from, for each output neuron
        this.switchx = global.zeros(this.out_sx*this.out_sy*this.out_depth);
        this.switchy = global.zeros(this.out_sx*this.out_sy*this.out_depth);
                 */

            this.out_sx = (int)Math.Floor((double)((in_sx + pad * 2 - Sx) / stride + 1));
            this.out_sy = (int)Math.Floor((double)((in_sy + pad * 2 - Sy) / stride + 1));
            // store switches for x,y coordinates for where the max comes from, for each output neuron
            this.switchx = new int[this.out_sx * this.out_sy * this.out_depth];
            this.switchy = new int[this.out_sx * this.out_sy * this.out_depth];
        }

        private int[] switchx;
        private int[] switchy;
        private int pad = 0;

        


        public int stride = 2;

        public PoolLayer(LayerDef def=null) : base(def)
        {
        }

        public override Volume Forward(Volume vin, bool training)
        {
            this.In = vin;

            var A = new Volume(this.out_sx, this.out_sy, this.out_depth, 0.0);

            var n = 0; // a counter for switches
            for (var d = 0; d < this.out_depth; d++)
            {
                var x = -this.pad;
                var y = -this.pad;
                for (var ax = 0; ax < this.out_sx; x += this.stride, ax++)
                {
                    y = -this.pad;
                    for (var ay = 0; ay < this.out_sy; y += this.stride, ay++)
                    {

                        // convolve centered at this particular location
                        double a = -99999; // hopefully small enough ;\
                        var winx = -1;
                        var winy = -1;
                        for (var fx = 0; fx < this.Sx; fx++)
                        {
                            for (var fy = 0; fy < this.Sy; fy++)
                            {
                                var oy = y + fy;
                                var ox = x + fx;
                                if (oy >= 0 && oy < vin.sy && ox >= 0 && ox < vin.sx)
                                {
                                    var v = vin.Get(ox, oy, d);
                                    // perform max pooling and store pointers to where
                                    // the max came from. This will speed up backprop 
                                    // and can help make nice visualizations in future
                                    if (v > a) { a = v; winx = ox; winy = oy; }
                                }
                            }
                        }
                        this.switchx[n] = winx;
                        this.switchy[n] = winy;
                        n++;
                        if (winy == -1)
                        {

                        }
                        A.Set(ax, ay, d, a);
                    }
                }
            }
            this.Out = A;
            return this.Out;
        }


        public override double Backward(object yy)
        {

            // pooling layers have no parameters, so simply compute 
            // gradient wrt data here
            var V = this.In;
            V.dw = new double[V.w.Length]; // zero out gradient wrt data
            var A = this.Out; // computed in forward pass 

            var n = 0;
            for (var d = 0; d < this.out_depth; d++)
            {
                var x = -this.pad;
                var y = -this.pad;
                for (var ax = 0; ax < this.out_sx; x += this.stride, ax++)
                {
                    y = -this.pad;
                    for (var ay = 0; ay < this.out_sy; y += this.stride, ay++)
                    {

                        var chain_grad = this.Out.get_grad(ax, ay, d);
                        V.add_grad(this.switchx[n], this.switchy[n], d, chain_grad);
                        n++;

                    }
                }
            }
            return 0;
        }

        public override PgListItem[] GetParamsAndGrads(int y = 0)
        {
            return new PgListItem[0];
        }

        public override string ToXml()
        {
            var xx = switchx.Aggregate("", (x, y) => x + y + ";");
            var yy = switchy.Aggregate("", (x, y) => x + y + ";");
            string str = "<layer name=\"" + Name + "\" swx=\"" + xx + "\" swy=\"" + yy + "\"/>"; ;


            return str;
        }

        public override void ParseXml(XElement elem)
        {
            var xx =
                elem.Attribute("swx")
                    .Value.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries).Select(int.Parse)
                    .ToArray();
            var yy =
              elem.Attribute("swy")
                  .Value.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries).Select(int.Parse)
                  .ToArray();
            for (int i = 0; i < switchx.Count(); i++)
            {
                switchx[i] = xx[i];
            }
            for (int i = 0; i < switchy.Count(); i++)
            {
                switchy[i] = yy[i];
            }


        }
    }
}