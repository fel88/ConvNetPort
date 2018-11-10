using System.Xml.Linq;

namespace ConvNetLib
{
    public abstract class Layer
    {
        public string Name;
        public Volume in_act;
        public Volume out_act;
        public int num_inputs;

        
        public  Layer(LayerDef def) { }
        
        public int out_sx;
        public int out_sy;

        public int? sx;
        public int? sy;

        public ActivationEnum? activation;
        public int out_depth;
        
        
        public int? num_classes;
        public int? num_neurons;
        public double? bias_pref;
        public int? group_size;
        public object drop_prob;

        public virtual void fromJson(dynamic json)
        {
        }
        
        public int in_sx;
        public int in_sy;
        
        
        public int in_depth;

        public abstract Volume Forward(Volume v, bool isTraining);
        public abstract double Backward(object y);
        public abstract PgListItem[] GetParamsAndGrads(int y = 0);

        public virtual string ToXml()
        {
            return "<layer/>";
        }

        public virtual void ParseXml(XElement elem)
        {

        }

    }
    public enum ActivationEnum
    {
        relu, sigmoid, tahn, maxout
    }
}