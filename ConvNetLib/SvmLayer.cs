using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace ConvNetLib
{
    public class SvmLayer : Layer
    {
        public SvmLayer(LayerDef def=null) : base(def)
        {
        }

        public override double Backward(object y)
        {
            // compute and accumulate gradient wrt weights and bias of this layer
            var x = this.in_act;
            //x.dw = global.zeros(x.w.length); // zero out the gradient of input Vol
            x.dw = new double[x.w.Length];
            // we're using structured loss here, which means that the score
            // of the ground truth should be higher than the score of any other 
            // class, by a margin
            var yscore = x.w[(int)y]; // score of ground truth
            var margin = 1.0;
            var loss = 0.0;
            for (var i = 0; i < this.out_depth; i++)
            {
                if ((int)y == i) { continue; }
                var ydiff = -yscore + x.w[i] + margin;
                if (ydiff > 0)
                {
                    // violating dimension, apply loss
                    x.dw[i] += 1;
                    x.dw[(int)y] -= 1;
                    loss += ydiff;
                }
            }

            return loss;
        }

        public override PgListItem[] GetParamsAndGrads(int y = 0)
        {
            return new PgListItem[] { };
        }

        public override Volume Forward(Volume V, bool isTraining)
        {
            this.in_act = V;
            this.out_act = V; // nothing to do, output raw scores
            return V;
        }

        public override string ToXml()
        {
            return $"<svm out_depth=\"{out_depth}\" out_sx=\"{out_sx}\" out_sy=\"{out_sy}\" num_inputs=\"{num_inputs}\"/>";
        }

        public override void ParseXml(XElement elem)
        {
            out_depth = int.Parse(elem.Attribute("out_depth").Value);
            out_sx = int.Parse(elem.Attribute("out_sx").Value);
            out_sy = int.Parse(elem.Attribute("out_sy").Value);
            num_inputs = int.Parse(elem.Attribute("num_inputs").Value);
        }

    }
}
