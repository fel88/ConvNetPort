using System;
using System.Linq;
using System.Xml.Linq;

namespace ConvNetLib
{
    public class SoftmaxLayer : Layer
    {
        public int NumInputs;

        public int NumClasses;
        private double[] es;

        public override void Init()
        {
            this.NumInputs = in_sx * in_sy * in_depth;
            this.OutDepth = this.NumInputs;
            this.OutSx = 1;
            this.OutSy = 1;
        }

        public int in_sx;
        public int in_sy;
        public int in_depth;

        public override Volume Forward(Volume v, bool training)
        {
            this.In = v;

            var A = new Volume(1, 1, this.OutDepth, 0.0);

            // compute max activation
            var _as = v.W;
            var amax = v.W[0];
            for (var i = 1; i < this.OutDepth; i++)
            {
                if (_as[i] > amax) amax = _as[i];
            }

            // compute exponentials (carefully to not blow up)
            var es = new double[OutDepth];
            var esum = 0.0;
            for (var i = 0; i < this.OutDepth; i++)
            {
                var e = Math.Exp(_as[i] - amax);
                esum += e;
                es[i] = e;
            }

            // normalize and output to sum to one
            for (var i = 0; i < this.OutDepth; i++)
            {
                es[i] /= esum;
                A.W[i] = es[i];
            }

            this.es = es; // save these for backprop
            this.Out = A;
            return this.Out;
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
            var x = this.In;
            x.Dw = new double[x.W.Length]; // zero out the gradient of input Vol

            for (var i = 0; i < this.OutDepth; i++)
            {
                var indicator = (i == y) ? 1.0 : 0.0;
                var mul = -(indicator - this.es[i]);
                x.Dw[i] = mul;
            }

            // loss is the class negative log likelihood
            return -Math.Log(this.es[y]);
        }

        public override PgListItem[] GetParamsAndGrads(int y = 0)
        {
            return new PgListItem[0];
        }

        public override string GetXmlSection()
        {
            string esstr = es.Aggregate("", (x, y) => x + y + ";");
            string ret = "<layer name=\"" + Name + "\" es=\"" + esstr + "\"/>";
            return ret;
        }

        public override void ParseXmlSection(XElement elem)
        {
            var ess = elem.Attribute("es").Value.Split(new string[] {";"}, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < ess.Count(); i++)
            {
                es[i] = double.Parse(ess[i]);
            }
            
        }
    }
}