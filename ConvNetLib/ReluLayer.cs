using System.Linq;
using System.Xml.Linq;

namespace ConvNetLib
{
    public class ReluLayer : Layer
    {
        public override void Init()
        {
            // computed
            this.OutSx = in_sx;
            this.OutSy = in_sy;

        }

        public int in_sx;
        public int in_sy;

        public override Volume Forward(Volume V, bool isTraining)
        {
            this.In = V;
           
            var V2 = V.Clone();
            var N = V.W.Length;
            var V2w = V2.W;
            for (var i = 0; i < N; i++)
            {
                if (V2w[i] < 0) V2w[i] = 0; // threshold at 0
            }
            if (V2.W.Any(z => double.IsNaN(z)))
            {
                
            }
            this.Out = V2;
            return this.Out;
        }

        public override double Backward(object y)
        {
            var V = this.In; // we need to set dw of this
            var V2 = this.Out;
            var N = V.W.Length;
            V.Dw = new double[N]; // zero out gradient wrt data
            for (var i = 0; i < N; i++)
            {
                if (V2.W[i] <= 0) V.Dw[i] = 0; // threshold
                else V.Dw[i] = V2.Dw[i];
            }
            return 0;
        }

        public override PgListItem[] GetParamsAndGrads(int y = 0)
        {
            return new PgListItem[0];
        }

    }
}