namespace ConvNetLib
{
    public class InputLayer : Layer
    {       
        public override Volume Forward(Volume v, bool training)
        {
            In = v;
            Out = v;
            return Out;
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