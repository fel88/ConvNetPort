using System.Xml.Linq;

namespace ConvNetLib
{
    public class InputLayer : Layer
    {
        public InputLayer(LayerDef def=null) : base(def)
        {
        }

        public override Volume Forward(Volume v, bool training)
        {
            In = v;
            Out = v;
            return Out;
        }

        public override string ToXml()
        {
            return $"<input out_depth=\"{out_depth}\" out_sx=\"{out_sx}\" out_sy=\"{out_sy}\">";
        }
        public override void ParseXml(XElement elem)
        {
            out_depth = int.Parse(elem.Attribute("out_depth").Value);
            out_sx = int.Parse(elem.Attribute("out_sx").Value);
            out_sy = int.Parse(elem.Attribute("out_sy").Value);
        }

        public override double Backward(object y)
        {
            return 0;
        }

        public override PgListItem[] GetParamsAndGrads(int y = 0)
        {
            return new PgListItem[0];
        }
    }
}