using System;
using System.Xml.Linq;

namespace ConvNetLib
{
    public class TanhLayer : Layer
    {
        public TanhLayer(LayerDef def=null) : base(def)
        {
        }

        public override Volume Forward(Volume v, bool training)
        {

            /*  this.in_act = V;
              var V2 = V.cloneAndZero();
              var N = V.w.length;
              for (var i = 0; i < N; i++)
              {
                  V2.w[i] = tanh(V.w[i]);
              }
              this.out_act = V2;
              return this.out_act;*/

            In = v;
            Volume v2 = new Volume(v.sx, v.sy, v.Depth);
            var N = v.w.Length;
            for (var i = 0; i < N; i++)
            {
                v2.w[i] = tanh(v.w[i]);
            }
            Out = v2;
            return Out;
        }

        public static double tanh(double x)
        {
            var y = Math.Exp(x * 2);
            return (double)((y - 1) / (y + 1));
        }

        public override double Backward(object y)
        {

            var V = In; // we need to set dw of this
            var V2 = Out;
            var N = V.w.Length;
            V.dw = new double[N]; // zero out gradient wrt data
            for (var i = 0; i < N; i++)
            {
                var v2wi = V2.w[i];
                V.dw[i] = (1.0 - v2wi * v2wi) * V2.dw[i];
            }
            return 0;

        }

        public override PgListItem[] GetParamsAndGrads(int y = 0)
        {
            return new PgListItem[0];
        }      
    }
}