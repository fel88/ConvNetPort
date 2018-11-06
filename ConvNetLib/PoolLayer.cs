using System;
using System.Linq;
using System.Xml.Linq;

namespace ConvNetLib
{
    public class PoolLayer : Layer
    {




        private int[] switchx;
        private int[] switchy;
        private int? pad = 0;

        public override void fromJson(dynamic json)
        {
            this.out_depth = json["out_depth"];
            this.out_sx = json["out_sx"];
            this.out_sy = json["out_sy"];

            this.sx = json["sx"];
            this.sy = json["sy"];
            this.stride = json["stride"];
            this.in_depth = json["in_depth"];
            this.pad = json["pad"] != null ? json["pad"] : 0; // backwards compatibility
            this.switchx = new int[(this.out_sx * this.out_sy * this.out_depth)]; // need to re-init these appropriately
            this.switchy = new int[this.out_sx * this.out_sy * this.out_depth];
        }



        public int? stride = 2;


        public PoolLayer(LayerDef def = null) : base(def)
        {
            var opt = def == null ? new LayerDef() : def;

            // required
            this.sx = opt.sx; // filter size
            this.in_depth = opt.in_depth;
            this.in_sx = opt.in_sx;
            this.in_sy = opt.in_sy;

            // optional
            this.sy = opt.sy != null ? opt.sy : this.sx;
            this.stride = opt.stride != null ? opt.stride : 2;
            this.pad = opt.pad != null ? opt.pad : 0; // amount of 0 padding to add around borders of input volume

            // computed
            this.out_depth = this.in_depth;
            this.out_sx = (int)Math.Floor((double)((this.in_sx + this.pad * 2 - this.sx) / this.stride + 1));
            this.out_sy = (int)Math.Floor((double)((this.in_sy + this.pad * 2 - this.sy) / this.stride + 1));
            //this.layer_type = 'pool';
            // store switches for x,y coordinates for where the max comes from, for each output neuron
            this.switchx = new int[this.out_sx * this.out_sy * this.out_depth];
            this.switchy = new int[(this.out_sx * this.out_sy * this.out_depth)];
        }

        public override Volume Forward(Volume vin, bool training)
        {
            this.in_act = vin;

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
                        for (var fx = 0; fx < this.sx; fx++)
                        {
                            for (var fy = 0; fy < this.sy; fy++)
                            {
                                var oy = y + fy;
                                var ox = x + fx;
                                if (oy >= 0 && oy < vin.sy && ox >= 0 && ox < vin.sx)
                                {
                                    var v = vin.Get(ox.Value, oy.Value, d);
                                    // perform max pooling and store pointers to where
                                    // the max came from. This will speed up backprop 
                                    // and can help make nice visualizations in future
                                    if (v > a) { a = v; winx = ox.Value; winy = oy.Value; }
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
            this.out_act = A;
            return this.out_act;
        }


        public override double Backward(object yy)
        {

            // pooling layers have no parameters, so simply compute 
            // gradient wrt data here
            var V = this.in_act;
            V.dw = new double[V.w.Length]; // zero out gradient wrt data
            var A = this.out_act; // computed in forward pass 

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

                        var chain_grad = this.out_act.get_grad(ax, ay, d);
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