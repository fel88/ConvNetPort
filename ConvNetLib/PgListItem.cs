namespace ConvNetLib
{
    public class PgListItem
    {
        public double[] Params;
        public double[] Grads;
        public double? l2_decay_mul;
        public double? l1_decay_mul;
    }
}