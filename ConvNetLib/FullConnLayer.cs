using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace ConvNetLib
{

    public class FullConnLayer : Layer
    {

        public FullConnLayer(LayerDef def = null) : base(def)
        {
            var opt = def != null ? def : new LayerDef();

            // required
            // ok fine we will allow 'filters' as the word as well
            this.out_depth =  opt.num_neurons != null ? opt.num_neurons.Value : opt.filters;

            // optional 
            this.l1_decay_mul =  opt.l1_decay_mul != null ? opt.l1_decay_mul : 0.0;
            this.l2_decay_mul =  opt.l2_decay_mul != null ? opt.l2_decay_mul : 1.0;

            // computed
            this.num_inputs = opt.in_sx * opt.in_sy * opt.in_depth;
            this.out_sx = 1;
            this.out_sy = 1;
            

            // initializations
            var bias =  opt.bias_pref != null ? opt.bias_pref : 0.0;
            this.filters = new List<ConvNetLib.Volume>();
            for (var i = 0; i < this.out_depth; i++) { this.filters.Add(new Volume(1, 1, this.num_inputs)); }
            this.biases = new Volume(1, 1, this.out_depth, bias.Value);

        }

        /*public override void Init()
        {
            if (num_neurons != null)
            {
                this.out_depth = num_neurons.Value;
            }
            
            var bias = 0.0;
            for (var i = 0; i < this.out_depth; i++) { this.filters.Add(new Volume(1, 1, this.NumInputs)); }
            this.biases = new Volume(1, 1, this.out_depth, bias);

        }*/


        

        public List<Volume> filters = new List<Volume>();
        public Volume biases = new Volume();
        private double? l1_decay_mul;
        private double? l2_decay_mul;

        public override Volume Forward(Volume v, bool tra)
        {
            this.In = v;
            var A = new Volume(1, 1, this.out_depth, 0.0);
            var Vw = v.w;

            for (var i = 0; i < this.out_depth; i++)
            {
                var a = 0.0;
                var wi = this.filters[i].w;
                for (var d = 0; d < this.num_inputs; d++)
                {
                    a += Vw[d] * wi[d]; // for efficiency use Vols directly for now
                }
                a += this.biases.w[i];
                A.w[i] = a;
            }
            this.Out = A;
            return this.Out;
        }

        public override double Backward(object y)
        {
            var V = this.In;
            V.dw = new double[V.w.Length]; // zero out the gradient in input Vol

            // compute gradient wrt weights and data
            for (var i = 0; i < this.out_depth; i++)
            {
                var tfi = this.filters[i];
                var chain_grad = this.Out.dw[i];
                for (var d = 0; d < this.num_inputs; d++)
                {
                    V.dw[d] += tfi.w[d] * chain_grad; // grad wrt input data
                    tfi.dw[d] += V.w[d] * chain_grad; // grad wrt params
                }
                this.biases.dw[i] += chain_grad;
            }
            return 0;
        }

        public override PgListItem[] GetParamsAndGrads(int y = 0)
        {
            List<PgListItem> response = new List<PgListItem>();
            for (var i = 0; i < this.out_depth; i++)
            {
                response.Add(new PgListItem()
                {
                    Params = this.filters[i].w,
                    Grads = this.filters[i].dw,
                    l1_decay_mul = l1_decay_mul,
                    l2_decay_mul = l2_decay_mul,
                });
                ;
            }
            response.Add(new PgListItem()
            {
                Params = this.biases.w,
                Grads = this.biases.dw,
                l1_decay_mul = 0.0,
                l2_decay_mul = 0.0,
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