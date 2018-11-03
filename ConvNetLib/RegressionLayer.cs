using System.Xml.Linq;

namespace ConvNetLib
{
    public class RegressionLayer : Layer
    {
        public override Volume Forward(Volume V, bool isTraining)
        {
            this.In = V;
            this.Out = V;
            return V; // identity function
        }

        public override void Init()
        {
            // computed
            /*num_inputs = opt.in_sx * opt.in_sy * opt.in_depth;
            this.out_depth = this.num_inputs;
            this.out_sx = 1;
            this.out_sy = 1;
            this.layer_type = 'regression';*/
        }

        public override double Backward(object yy)
        {
            // compute and accumulate gradient wrt weights and bias of this layer
            var x = this.In;
            x.Dw = new double[x.W.Length]; // zero out the gradient of input Vol
            var loss = 0.0;
            if (yy is double[])
            {
                var y = yy as double[];
                for (var i = 0; i < this.OutDepth; i++)
                {
                    var dy = x.W[i] - y[i];
                    x.Dw[i] = dy;
                    loss += 0.5 * dy * dy;
                }
            }
            else if (yy is int)
            {
                // lets hope that only one number is being regressed
                var dy = x.W[0] - (int)yy;
                x.Dw[0] = dy;
                loss += 0.5 * dy * dy;
            }
            else
            {
                var y = (UnknownClass1)yy;
                // assume it is a struct with entries .dim and .val
                // and we pass gradient only along dimension dim to be equal to val
                var i = y.dim;
                var yi = y.val;
                var dy = x.W[i] - yi;
                x.Dw[i] = dy;
                loss += 0.5 * dy * dy;
            }
            return loss;
        }

        public override PgListItem[] GetParamsAndGrads(int y = 0)
        {
            return new PgListItem[0];
        }

      
    }

    public class UnknownClass1
    {
        public int dim;
        public double val;
    }
}