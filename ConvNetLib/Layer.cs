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

        public ActivationEnum? activation;
        public int OutDepth;
        public Volume In;
        public Volume Out;
        public int? num_classes;
        public int? num_neurons;
        public double? bias_pref;
        public int? group_size;
        public object drop_prob;
        public object out_sx;
        public object in_sx;
        public object in_sy;
        public object out_sy;
        public object out_depth;
        public object in_depth;

        public abstract Volume Forward(Volume v, bool isTraining);
        public abstract double Backward(object y);
        public abstract PgListItem[] GetParamsAndGrads(int y = 0);

        public virtual string GetXmlSection()
        {
            return "";
        }

        public virtual void ParseXmlSection(XElement elem)
        {

        }

    }
    public enum ActivationEnum
    {
        relu, sigmoid, tahn, maxout
    }
}