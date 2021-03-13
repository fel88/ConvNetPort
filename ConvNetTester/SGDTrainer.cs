using ConvNetLib;

namespace ConvNetTester
{
    public class SGDTrainer : Trainer
    {
        private TdTrainerOptions tdtrainer_options;

        public SGDTrainer(Net value_net, TdTrainerOptions tdtrainer_options)
        {
            this.net = value_net;
            this.tdtrainer_options = tdtrainer_options;
        }
    }
}
