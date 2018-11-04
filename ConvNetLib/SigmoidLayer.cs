using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConvNetLib
{
    // Implements Sigmoid nnonlinearity elementwise
    // x -> 1/(1+e^(-x))
    // so the output is between 0 and 1.
    public class SigmoidLayer : Layer
    {

        public SigmoidLayer(LayerDef _opt=null):base(_opt)
        {
            var opt = _opt == null ? new LayerDef() : _opt;

            // computed
            this.out_sx = opt.in_sx;
            this.out_sy = opt.in_sy;
            this.out_depth = opt.in_depth;            
        }
        public override double Backward(object y)
        {
            var V = this.in_act; // we need to set dw of this
            var V2 = this.out_act;
            var N = V.w.Length;
            V.dw = new double[N]; // zero out gradient wrt data
            for (var i = 0; i < N; i++)
            {
                var v2wi = V2.w[i];
                V.dw[i] = v2wi * (1.0 - v2wi) * V2.dw[i];
            }
            return 0;
        }

        public override Volume Forward(Volume V, bool isTraining)
        {
            this.in_act = V;
            var V2 = V.cloneAndZero();
            var N = V.w.Length;
            var V2w = V2.w;
            var Vw = V.w;
            for (var i = 0; i < N; i++)
            {
                V2w[i] = 1.0 / (1.0 + Math.Exp(-Vw[i]));
            }
            this.out_act = V2;
            return this.out_act;
        }

        public override PgListItem[] GetParamsAndGrads(int y = 0)
        {
            return new PgListItem[0];
        }
    }

}
