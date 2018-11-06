using System;
using System.Linq;
using System.Xml.Linq;

namespace ConvNetLib
{
    public class ReluLayer : Layer
    {
        public ReluLayer(LayerDef def = null) : base(def)
        {
            var opt = def != null ? def : new LayerDef();

            // computed
            this.out_sx = opt.in_sx;
            this.out_sy = opt.in_sy;
            this.out_depth = opt.in_depth;

        }

        public override void fromJson(dynamic json)
        {
            this.out_depth = json["out_depth"];
            this.out_sx = json["out_sx"];
            this.out_sy = json["out_sy"];            
        }

        /*     public override void Init()
             {
                 // computed
                 this.out_sx = in_sx;
                 this.out_sy = in_sy;

             }
             */


        public override Volume Forward(Volume V, bool isTraining)
        {
            this.in_act = V;

            var V2 = V.Clone();
            var N = V.w.Length;
            var V2w = V2.w;
            for (var i = 0; i < N; i++)
            {
                if (V2w[i] < 0) V2w[i] = 0; // threshold at 0
            }
            if (V2.w.Any(z => double.IsNaN(z)))
            {

            }
            this.out_act = V2;
            return this.out_act;
        }

        public override double Backward(object y)
        {
            var V = this.in_act; // we need to set dw of this
            var V2 = this.out_act;
            var N = V.w.Length;
            V.dw = new double[N]; // zero out gradient wrt data
            for (var i = 0; i < N; i++)
            {
                if (V2.w[i] <= 0) V.dw[i] = 0; // threshold
                else V.dw[i] = V2.dw[i];
            }
            return 0;
        }

        public override PgListItem[] GetParamsAndGrads(int y = 0)
        {
            return new PgListItem[0];
        }
    }
}