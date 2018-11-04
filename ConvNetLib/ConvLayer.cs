using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace ConvNetLib
{
    public class ConvLayer : Layer
    {


        public override void Init()
        {
            out_depth = filtersCnt;
            this.out_sx = (int)Math.Floor((double)((this.in_sx + this.pad * 2 - this.sx) / this.stride + 1));
            this.out_sy = (int)Math.Floor((double)((this.in_sy + this.pad * 2 - this.sy) / this.stride + 1));

            var bias = bias_pref != null ? bias_pref.Value : 0.0;
            this.filters = new List<Volume>();
            for (var i = 0; i < this.out_depth; i++) { this.filters.Add(new Volume(this.sx, this.sy, this.in_depth)); }
            this.biases = new Volume(1, 1, this.out_depth, bias);
        }



        public int sy;
        public int sx;

        public int? stride;


        public int pad;
        public List<Volume> filters = new List<Volume>();
        public int filtersCnt = 8;
        public Volume biases = new Volume();

        public override Volume Forward(Volume V, bool isTraining)
        {
            // optimized code by @mdda that achieves 2x speedup over previous version

            this.In = V;
            var A = new Volume(this.out_sx, this.out_sy, this.out_depth, 0.0);

            var V_sx = V.sx;
            var V_sy = V.sy;
            var xy_stride = this.stride.Value;

            for (var d = 0; d < this.out_depth; d++)
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
                        for (var fy = 0; fy < f.sy; fy++)
                        {
                            var oy = y + fy; // coordinates in the original input array coordinates
                            for (var fx = 0; fx < f.sx; fx++)
                            {
                                var ox = x + fx;
                                if (oy >= 0 && oy < V_sy && ox >= 0 && ox < V_sx)
                                {
                                    for (var fd = 0; fd < f.Depth; fd++)
                                    {
                                        // avoid function call overhead (x2) for efficiency, compromise modularity :(
                                        a += f.w[((f.sx * fy) + fx) * f.Depth + fd] * V.w[((V_sx * oy) + ox) * V.Depth + fd];
                                    }
                                }
                            }
                        }
                        a += this.biases.w[d];
                        A.Set(ax, ay, d, a);
                    }
                }
            }
            if (A.w.Any(double.IsNaN))
            {

            }
            this.Out = A;
            return this.Out;
        }

        public override double Backward(object yy)
        {

            var V = this.In;
            V.dw = new double[V.w.Length]; // zero out gradient wrt bottom data, we're about to fill it

            var V_sx = V.sx;
            var V_sy = V.sy;
            var xy_stride = this.stride.Value;

            for (var d = 0; d < this.out_depth; d++)
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
                        for (var fy = 0; fy < f.sy; fy++)
                        {
                            var oy = y + fy; // coordinates in the original input array coordinates
                            for (var fx = 0; fx < f.sx; fx++)
                            {
                                var ox = x + fx;
                                if (oy >= 0 && oy < V_sy && ox >= 0 && ox < V_sx)
                                {
                                    for (var fd = 0; fd < f.Depth; fd++)
                                    {
                                        // avoid function call overhead (x2) for efficiency, compromise modularity :(
                                        var ix1 = ((V_sx * oy) + ox) * V.Depth + fd;
                                        var ix2 = ((f.sx * fy) + fx) * f.Depth + fd;
                                        f.dw[ix2] += V.w[ix1] * chain_grad;
                                        V.dw[ix1] += f.w[ix2] * chain_grad;
                                    }
                                }
                            }
                        }
                        this.biases.dw[d] += chain_grad;
                    }
                }
            }
            return 0;
        }

        public double l1_decay_mul = 0;
        public double l2_decay_mul = 0;

        public ConvLayer(LayerDef _opt = null) : base(_opt)
        {
            var opt = _opt == null ? new LayerDef() : _opt;

            // required
            this.out_depth = opt.filters;
            this.sx = opt.sx; // filter size. Should be odd if possible, it's cleaner.
            this.in_depth = opt.in_depth;
            this.in_sx = opt.in_sx;
            this.in_sy = opt.in_sy;

            // optional
            this.sy = opt.sy != null ? opt.sy.Value : this.sx;
            this.stride = opt.stride != null ? opt.stride : 1; // stride at which we apply filters to input volume
            this.pad = opt.pad != null ? opt.pad.Value : 0; // amount of 0 padding to add around borders of input volume
            this.l1_decay_mul = opt.l1_decay_mul != null ? opt.l1_decay_mul.Value : 0.0;
            this.l2_decay_mul = opt.l2_decay_mul != null ? opt.l2_decay_mul.Value : 1.0;

            // computed
            // note we are doing floor, so if the strided convolution of the filter doesnt fit into the input
            // volume exactly, the output volume will be trimmed and not contain the (incomplete) computed
            // final application.
            this.out_sx = (int)Math.Floor((double)((this.in_sx + this.pad * 2 - this.sx) / this.stride.Value + 1));
            this.out_sy = (int)Math.Floor((double)((this.in_sy + this.pad * 2 - this.sy) / this.stride.Value + 1));

            // initializations
            var bias = opt.bias_pref != null ? opt.bias_pref : 0.0;
            this.filters = new List<ConvNetLib.Volume>();
            for (var i = 0; i < this.out_depth; i++) { this.filters.Add(new Volume(this.sx, this.sy, this.in_depth)); }
            this.biases = new Volume(1, 1, this.out_depth, bias.Value);
        }

        public override PgListItem[] GetParamsAndGrads(int y = 0)
        {
            List<PgListItem> response = new List<PgListItem>();
            for (var i = 0; i < this.out_depth; i++)
            {
                response.Add(
                    new PgListItem()
                    {
                        Params = this.filters[i].w,
                        Grads = this.filters[i].dw,
                        l2_decay_mul = l2_decay_mul,
                        l1_decay_mul = l1_decay_mul,
                    });

            }
            response.Add(
                new PgListItem()
                {
                    Params = this.biases.w,
                    Grads = this.biases.dw,
                    l2_decay_mul = 0,
                    l1_decay_mul = 0,
                });


            return response.ToArray();
        }

        public override string ToXml()
        {
            var biasesstr = biases.w.Aggregate("", (x, y) => x + y + ";");
            string str = "<layer name=\"" + Name + "\" biases=\"" + biasesstr + "\">";
            foreach (var filter in filters)
            {
                var fstr = filter.w.Aggregate("", (x, y) => x + y + ";");
                str += "<filter val=\"" + fstr + "\"/>";
            }
            str += "</layer>";
            return str;
        }

        public override void ParseXml(XElement elem)
        {
            var bss =
                elem.Attribute("biases")
                    .Value.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries).Select(double.Parse)
                    .ToArray();
            for (int i = 0; i < biases.w.Count(); i++)
            {
                biases.w[i] = bss[i];
            }
            var array = elem.Descendants("filter").ToArray();
            for (int k = 0; k < array.Length; k++)
            {
                bss =
                   array[k].Attribute("val")
                       .Value.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries).Select(double.Parse)
                       .ToArray();
                for (int i = 0; i < filters[k].w.Count(); i++)
                {
                    filters[k].w[i] = bss[i];
                }
            }

        }
    }
}