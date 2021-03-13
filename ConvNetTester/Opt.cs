using ConvNetLib;
using System.Collections.Generic;

namespace ConvNetTester
{
    public class Opt
    {
        public int temporal_window;
        public int? experience_size = 30000;
        public double? start_learn_threshold = 1000;
        public double? gamma = 0.7;
        public int? learning_steps_total = 200000;
        public int? learning_steps_burnin = 3000;
        public double? epsilon_min = 0.05;
        public double? epsilon_test_time = 0.05;
        public List<LayerDef> layer_defs;
        public TdTrainerOptions tdtrainer_options;
        internal int[] random_action_distribution;
        internal int[] hidden_layer_sizes;
    }
}
