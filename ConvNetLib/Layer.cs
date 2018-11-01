using System.Xml.Linq;

namespace ConvNetLib
{
    public abstract class Layer
    {
        public string Name;
        public virtual void Init()
        {
        }

        public int OutSx;
        public int OutSy;

        public int Sx;
        public int Sy;
        public string activation;
        public int OutDepth;
        public Volume In;
        public Volume Out;
        public abstract Volume Forward(Volume v, bool isTraining);
        public abstract double Backward( object y);
        public abstract PgListItem[] GetParamsAndGrads(int y = 0);

        public virtual string GetXmlSection()
        {
            return "";
        }

        public virtual void ParseXmlSection(XElement elem)
        {
            
        }
    }
}