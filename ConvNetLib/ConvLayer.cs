using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace ConvNetLib
{
    public class ConvLayer : Layer
    {
        public ConvLayer()
        {

        }

        public override void Init()
        {
            OutDepth = filtersCnt;
            this.OutSx = (int)Math.Floor((double)((this.in_sx + this.pad * 2 - this.sx) / this.stride + 1));
            this.OutSy = (int)Math.Floor((double)((this.in_sy + this.pad * 2 - this.sy) / this.stride + 1));

            var bias = bias_pref != null ? bias_pref.Value : 0.0;
            this.filters = new List<Volume>();
            for (var i = 0; i < this.OutDepth; i++) { this.filters.Add(new Volume(this.sx, this.sy, this.in_depth)); }
            this.biases = new Volume(1, 1, this.OutDepth, bias);
        }

        public int in_depth;
        public double? bias_pref;
        public int sy;
        public int sx;
        public int in_sx;
        public int in_sy;
        public int stride;

        public int out_sx
        {
            get
            {
                return OutSx;
            }
        }

        public int out_sy
        {
            get
            {
                return OutSy;
            }
        }
        public int pad;
        public List<Volume> filters = new List<Volume>();
        public int filtersCnt = 8;
        public Volume biases = new Volume();

        public override Volume Forward(Volume V, bool isTraining)
        {
            // optimized code by @mdda that achieves 2x speedup over previous version

            this.In = V;
            var A = new Volume(this.out_sx, this.out_sy, this.OutDepth, 0.0);

            var V_sx = V.Sx;
            var V_sy = V.Sy;
            var xy_stride = this.stride;

            for (var d = 0; d < this.OutDepth; d++)
            {
                var f = this.filters[d];
                var x = -this.pad;
                var y = -this.pad;
                for (var ay = 0; ay < this.out_sy; y += xy_stride, ay++)
                {  // xy_stride
                    x = -this.pad;
                    for (var ax = 0; ax < this.out_sx; x += xy_stride, ax++)
                    {  // xy_stride

                        // convolve centered at this particular location
                        var a = 0.0;
                        for (var fy = 0; fy < f.Sy; fy++)
                        {
                            var oy = y + fy; // coordinates in the original input array coordinates
                            for (var fx = 0; fx < f.Sx; fx++)
                            {
                                var ox = x + fx;
                                if (oy >= 0 && oy < V_sy && ox >= 0 && ox < V_sx)
                                {
                                    for (var fd = 0; fd < f.Depth; fd++)
                                    {
                                        // avoid function call overhead (x2) for efficiency, compromise modularity :(
                                        a += f.W[((f.Sx * fy) + fx) * f.Depth + fd] * V.W[((V_sx * oy) + ox) * V.Depth + fd];
                                    }
                                }
                            }
                        }
                        a += this.biases.W[d];
                        A.Set(ax, ay, d, a);
                    }
                }
            }
            if (A.W.Any(double.IsNaN))
            {

            }
            this.Out = A;
            return this.Out;
        }

        public override double Backward(object yy)
        {

            var V = this.In;
            V.Dw = new double[V.W.Length]; // zero out gradient wrt bottom data, we're about to fill it

            var V_sx = V.Sx;
            var V_sy = V.Sy;
            var xy_stride = this.stride;

            for (var d = 0; d < this.OutDepth; d++)
            {
                var f = this.filters[d];
                var x = -this.pad;
                var y = -this.pad;
                for (var ay = 0; ay < this.out_sy; y += xy_stride, ay++)
                {  // xy_stride
                    x = -this.pad;
                    for (var ax = 0; ax < this.out_sx; x += xy_stride, ax++)
                    {  // xy_stride

                        // convolve centered at this particular location
                        var chain_grad = this.Out.get_grad(ax, ay, d); // gradient from above, from chain rule
                        for (var fy = 0; fy < f.Sy; fy++)
                        {
                            var oy = y + fy; // coordinates in the original input array coordinates
                            for (var fx = 0; fx < f.Sx; fx++)
                            {
                                var ox = x + fx;
                                if (oy >= 0 && oy < V_sy && ox >= 0 && ox < V_sx)
                                {
                                    for (var fd = 0; fd < f.Depth; fd++)
                                    {
                                        // avoid function call overhead (x2) for efficiency, compromise modularity :(
                                        var ix1 = ((V_sx * oy) + ox) * V.Depth + fd;
                                        var ix2 = ((f.Sx * fy) + fx) * f.Depth + fd;
                                        f.Dw[ix2] += V.W[ix1] * chain_grad;
                                        V.Dw[ix1] += f.W[ix2] * chain_grad;
                                    }
                                }
                            }
                        }
                        this.biases.Dw[d] += chain_grad;
                    }
                }
            }
            return 0;
        }

        public double l1_decay_mul = 0;
        public double l2_decay_mul = 0;

        public override PgListItem[] GetParamsAndGrads(int y = 0)
        {
            List<PgListItem> response = new List<PgListItem>();
            for (var i = 0; i < this.OutDepth; i++)
            {
                response.Add(
                    new PgListItem()
                    {
                        Params = this.filters[i].W,
                        Grads = this.filters[i].Dw,
                        l2_decay_mul = l2_decay_mul,
                        l1_decay_mul = l1_decay_mul,
                    });

            }
            response.Add(
                new PgListItem()
                {
                    Params = this.biases.W,
                    Grads = this.biases.Dw,
                    l2_decay_mul = 0,
                    l1_decay_mul = 0,
                });


            return response.ToArray();
        }

        public override string GetXmlSection()
        {
            var biasesstr = biases.W.Aggregate("", (x, y) => x + y + ";");
            string str = "<layer name=\"" + Name + "\" biases=\"" + biasesstr + "\">";
            foreach (var filter in filters)
            {
                var fstr = filter.W.Aggregate("", (x, y) => x + y + ";");
                str += "<filter val=\"" + fstr + "\"/>";
            }
            str += "</layer>";
            return str;
        }

        public override void ParseXmlSection(XElement elem)
        {
            var bss =
                elem.Attribute("biases")
                    .Value.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries).Select(double.Parse)
                    .ToArray();
            for (int i = 0; i < biases.W.Count(); i++)
            {
                biases.W[i] = bss[i];
            }
            var array = elem.Descendants("filter").ToArray();
            for (int k = 0; k < array.Length; k++)
            {
                bss =
                   array[k].Attribute("val")
                       .Value.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries).Select(double.Parse)
                       .ToArray();
                for (int i = 0; i < filters[k].W.Count(); i++)
                {
                    filters[k].W[i] = bss[i];
                }
            }

        }
    }
}