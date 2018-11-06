using System.Xml.Linq;

namespace ConvNetLib
{
    public class RegressionLayer : Layer
    {
        // implements an L2 regression cost layer,
        // so penalizes \sum_i(||x_i - y_i||^2), where x is its input
        // and y is the user-provided array of "correct" values.
        public RegressionLayer(LayerDef def = null) : base(def)
        {
            var opt = def != null ? def : new LayerDef();

            // computed
            this.num_inputs = opt.in_sx * opt.in_sy * opt.in_depth;
            this.out_depth = this.num_inputs;
            this.out_sx = 1;
            this.out_sy = 1;

        }

        public override Volume Forward(Volume V, bool isTraining)
        {
            this.in_act = V;
            this.out_act = V;
            return V; // identity function
        }


        public override double Backward(object yy)
        {
            // compute and accumulate gradient wrt weights and bias of this layer
            var x = this.in_act;
            x.dw = new double[x.w.Length]; // zero out the gradient of input Vol
            var loss = 0.0;
            if (yy is double[])
            {
                var y = yy as double[];
                for (var i = 0; i < this.out_depth; i++)
                {
                    var dy = x.w[i] - y[i];
                    x.dw[i] = dy;
                    loss += 0.5 * dy * dy;
                }
            }
            else if (yy is int)
            {
                // lets hope that only one number is being regressed
                var dy = x.w[0] - (int)yy;
                x.dw[0] = dy;
                loss += 0.5 * dy * dy;
            }
            else
            {
                var y = (UnknownClass1)yy;
                // assume it is a struct with entries .dim and .val
                // and we pass gradient only along dimension dim to be equal to val
                var i = y.dim;
                var yi = y.val;
                var dy = x.w[i] - yi;
                x.dw[i] = dy;
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