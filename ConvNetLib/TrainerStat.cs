namespace ConvNetLib
{
    public class TrainerStat
    {
        public double loss;
        public double cost_loss;
        public double l2_decay_loss;
        public int fwd_time;
        public int bwd_time;
    }
    public class TdTrainerOptions
    {
        public TrainerMethodEnum method;
        public int batch_size;
        public double learning_rate;
        public double l2_decay;        
        public double  momentum;
    }
}