using System.Xml.Linq;

namespace ConvNetLib
{
    public class InputLayer : Layer
    {        
        public InputLayer(LayerDef def = null) : base(def)
        {
            var opt = def != null ? def : new LayerDef();

            // required: depth
            //this.out_depth = getopt(opt, ['out_depth', 'depth'], 0);
            this.out_depth = opt.out_depth;

            // optional: default these dimensions to 1
            //this.out_sx = getopt(opt, ['out_sx', 'sx', 'width'], 1);
            //this.out_sy = getopt(opt, ['out_sy', 'sy', 'height'], 1);            
            this.out_sx = opt.out_sx;
            this.out_sy = opt.out_sy;
        }

        public override Volume Forward(Volume v, bool training)
        {
            in_act = v;
            out_act = v;
            return out_act;
        }

        public override void fromJson(dynamic json)
        {
            this.out_depth = json["out_depth"];
            this.out_sx= json["out_sx"];
            this.out_sy= json["out_sy"];
        }

        public override string ToXml()
        {
            return $"<layer out_depth=\"{out_depth}\" out_sx=\"{out_sx}\" out_sy=\"{out_sy}\"/>";
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