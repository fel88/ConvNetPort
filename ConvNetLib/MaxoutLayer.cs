using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConvNetLib
{
    // Implements Maxout nnonlinearity that computes
    // x -> max(x)
    // where x is a vector of size group_size. Ideally of course,
    // the input size should be exactly divisible by group_size
    public class MaxoutLayer : Layer
    {

        public MaxoutLayer(LayerDef _opt=null):base(_opt)
        {

            var opt = _opt == null ? new LayerDef() : _opt;

            // required
            this.group_size = opt.group_size != null ? opt.group_size : 2;

            // computed
            this.out_sx = opt.in_sx;
            this.out_sy = opt.in_sy;
            this.out_depth = (int)Math.Floor((double)opt.in_depth / this.group_size.Value);

            this.switches = new int[this.out_sx * this.out_sy * this.out_depth]; // useful for backprop
        }
        int[] switches;
        public override double Backward(object _y)
        {
            var V = this.in_act; // we need to set dw of this
            var V2 = this.out_act;
            var N = this.out_depth;
            V.dw = new double[V.w.Length]; // zero out gradient wrt data

            // pass the gradient through the appropriate switch
            if (this.out_sx == 1 && this.out_sy == 1)
            {
                for (var i = 0; i < N; i++)
                {
                    var chain_grad = V2.dw[i];
                    V.dw[this.switches[i]] = chain_grad;
                }
            }
            else
            {
                // bleh okay, lets do this the hard way
                var n = 0; // counter for switches
                for (var x = 0; x < V2.sx; x++)
                {
                    for (var y = 0; y < V2.sy; y++)
                    {
                        for (var i = 0; i < N; i++)
                        {
                            var chain_grad = V2.get_grad(x, y, i);
                            V.set_grad(x, y, this.switches[n], chain_grad);
                            n++;
                        }
                    }
                }
            }
            return 0;
        }

        public override Volume Forward(Volume v, bool isTraining)
        {
            throw new NotImplementedException();
        }

        public override PgListItem[] GetParamsAndGrads(int y = 0)
        {
            throw new NotImplementedException();
        }
    }


    
}
