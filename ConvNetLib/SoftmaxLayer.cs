using System;
using System.Linq;
using System.Xml.Linq;

namespace ConvNetLib
{
    public class SoftmaxLayer : Layer
    {
        private double[] es;

        public SoftmaxLayer(LayerDef def = null) : base(def)
        {
            var opt = def != null ? def : new LayerDef();

            // computed
            this.num_inputs = opt.in_sx * opt.in_sy * opt.in_depth;
            this.out_depth = this.num_inputs;
            this.out_sx = 1;
            this.out_sy = 1;

        }

        public override void fromJson(dynamic json)
        {
            this.out_depth = json["out_depth"];
            this.out_sx = json["out_sx"];
            this.out_sy = json["out_sy"];
            
            this.num_inputs = json["num_inputs"];
        }

        /*     public override void Init()
             {
                 this.NumInputs = in_sx * in_sy * in_depth;
                 this.out_depth = this.NumInputs;
                 this.out_sx = 1;
                 this.out_sy = 1;
             }
             */


        public override Volume Forward(Volume v, bool training)
        {
            this.in_act = v;

            var A = new Volume(1, 1, this.out_depth, 0.0);

            // compute max activation
            var _as = v.w;
            var amax = v.w[0];
            for (var i = 1; i < this.out_depth; i++)
            {
                if (_as[i] > amax) amax = _as[i];
            }

            // compute exponentials (carefully to not blow up)
            var es = new double[out_depth];
            var esum = 0.0;
            for (var i = 0; i < this.out_depth; i++)
            {
                var e = Math.Exp(_as[i] - amax);
                esum += e;
                es[i] = e;
            }

            // normalize and output to sum to one
            for (var i = 0; i < this.out_depth; i++)
            {
                es[i] /= esum;
                A.w[i] = es[i];
            }

            this.es = es; // save these for backprop
            this.out_act = A;
            return this.out_act;
        }

        public override double Backward(object yy)
        {
            int y = -1;
            if (yy is int)
            {
                y = (int)yy;
            }
            if (yy is Array)
            {
                y = (int)((Array)yy).GetValue(0);
            }

            // compute and accumulate gradient wrt weights and bias of this layer
            var x = this.in_act;
            x.dw = new double[x.w.Length]; // zero out the gradient of input Vol

            for (var i = 0; i < this.out_depth; i++)
            {
                var indicator = (i == y) ? 1.0 : 0.0;
                var mul = -(indicator - this.es[i]);
                x.dw[i] = mul;
            }

            // loss is the class negative log likelihood
            return -Math.Log(this.es[y]);
        }

        public override PgListItem[] GetParamsAndGrads(int y = 0)
        {
            return new PgListItem[0];
        }

        public override string ToXml()
        {
            string esstr = es.Aggregate("", (x, y) => x + y + ";");
            string ret = "<layer name=\"" + Name + "\" es=\"" + esstr + "\"/>";
            return ret;
        }

        public override void ParseXml(XElement elem)
        {
            var ess = elem.Attribute("es").Value.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
            if (es == null)
            {
                es = new double[ess.Count()];
            }
            for (int i = 0; i < ess.Count(); i++)
            {
                es[i] = double.Parse(ess[i]);
            }

        }
    }


}