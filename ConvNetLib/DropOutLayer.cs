using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConvNetLib
{
    public class DropOutLayer : Layer
    {
        public DropOutLayer(LayerDef def) : base(def)
        {
        }

        public override double Backward(object y)
        {
            throw new NotImplementedException();
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
