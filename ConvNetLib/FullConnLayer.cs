using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace ConvNetLib
{

    public class FullConnLayer : Layer
    {
        public FullConnLayer(int? out_sx = null, int? out_sy = null)
        {
            l2_decay_mul = 1;
            l1_decay_mul = 0;
        }

        public override void Init()
        {
            if (num_neurons != null)
            {
                this.OutDepth = num_neurons.Value;
            }
            
            var bias = 0.0;
            for (var i = 0; i < this.OutDepth; i++) { this.filters.Add(new Volume(1, 1, this.NumInputs)); }
            this.biases = new Volume(1, 1, this.OutDepth, bias);

        }

     
        public int NumInputs;
        public int? num_neurons;
        public List<Volume> filters = new List<Volume>();
        public Volume biases = new Volume();
        private double? l1_decay_mul;
        private double? l2_decay_mul;

        public override Volume Forward(Volume v, bool tra)
        {
            this.In = v;
            var A = new Volume(1, 1, this.OutDepth, 0.0);
            var Vw = v.W;

            for (var i = 0; i < this.OutDepth; i++)
            {
                var a = 0.0;
                var wi = this.filters[i].W;
                for (var d = 0; d < this.NumInputs; d++)
                {
                    a += Vw[d] * wi[d]; // for efficiency use Vols directly for now
                }
                a += this.biases.W[i];
                A.W[i] = a;
            }
            this.Out = A;
            return this.Out;
        }

        public override double Backward(object y)
        {
            var V = this.In;
            V.Dw = new double[V.W.Length]; // zero out the gradient in input Vol

            // compute gradient wrt weights and data
            for (var i = 0; i < this.OutDepth; i++)
            {
                var tfi = this.filters[i];
                var chain_grad = this.Out.Dw[i];
                for (var d = 0; d < this.NumInputs; d++)
                {
                    V.Dw[d] += tfi.W[d] * chain_grad; // grad wrt input data
                    tfi.Dw[d] += V.W[d] * chain_grad; // grad wrt params
                }
                this.biases.Dw[i] += chain_grad;
            }
            return 0;
        }

        public override PgListItem[] GetParamsAndGrads(int y = 0)
        {
            List<PgListItem> response = new List<PgListItem>();
            for (var i = 0; i < this.OutDepth; i++)
            {
                response.Add(new PgListItem()
                {
                    Params = this.filters[i].W,
                    Grads = this.filters[i].Dw,
                    l1_decay_mul = l1_decay_mul,
                    l2_decay_mul = l2_decay_mul,
                });
                ;
            }
            response.Add(new PgListItem()
            {
                Params = this.biases.W,
                Grads = this.biases.Dw,
                l1_decay_mul = 0.0,
                l2_decay_mul = 0.0,
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