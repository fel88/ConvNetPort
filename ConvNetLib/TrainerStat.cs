﻿namespace ConvNetLib
{
    public class TrainerStat
    {
        public double loss;
        public double cost_loss;
        public double l2_decay_loss;
        public int fwd_time;
        public int bwd_time;
    }
}