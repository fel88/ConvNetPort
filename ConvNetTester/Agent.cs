using ConvNetLib;
using System;
using System.Collections.Generic;

namespace ConvNetTester
{
    public class Agent
    {
        // A single agent
        public Agent()
        {

            // positional information
            this.p = new Vec(50, 50);
            this.op = this.p; // old position
            this.angle = 0; // direction facing

            this.actions = new List<double[]>();
            this.actions.Add(new double[] { 1, 1 });
            this.actions.Add(new[] { 0.8, 1 });
            this.actions.Add(new double[] { 1, 0.8 });
            this.actions.Add(new double[] { 0.5, 0 });
            this.actions.Add(new double[] { 0, 0.5 });

            // properties
            this.rad = 10;
            this.eyes = new List<ConvNetTester.Eye>();
            for (var k = 0; k < 9; k++) { this.eyes.Add(new Eye((k - 3) * 0.25)); }

            // braaain
            //this.brain = new deepqlearn.Brain(this.eyes.length * 3, this.actions.length);
            //var spec = document.getElementById('qspec').value;
            // eval(spec)           ;
            eval();
            //this.brain = brain;

            this.reward_bonus = 0.0;
            this.digestion_signal = 0.0;

            // outputs on world
            this.rot1 = 0.0; // rotation speed of 1st wheel
            this.rot2 = 0.0; // rotation speed of 2nd wheel

            this.prevactionix = -1;
        }

        public void forward()
        {
            // in forward pass the agent simply behaves in the environment
            // create input to brain
            var num_eyes = this.eyes.Count;
            var input_array = new double[num_eyes * 3];
            for (var i = 0; i < num_eyes; i++)
            {
                var e = this.eyes[i];
                input_array[i * 3] = 1.0;
                input_array[i * 3 + 1] = 1.0;
                input_array[i * 3 + 2] = 1.0;
                if (e.sensed_type != -1)
                {
                    // sensed_type is 0 for wall, 1 for food and 2 for poison.
                    // lets do a 1-of-k encoding into the input array
                    input_array[i * 3 + e.sensed_type.Value] = e.sensed_proximity / e.max_range; // normalize to [0,1]
                }
            }

            // get action from brain
            var actionix = this.brain.forward(input_array);
            var action = this.actions[actionix.Value];
            this.actionix = actionix; //back this up

            // demultiplex into behavior variables
            this.rot1 = action[0] * 1;
            this.rot2 = action[1] * 1;

            //this.rot1 = 0;
            //this.rot2 = 0;
        }

        public void eval()
        {
            var num_inputs = 27; // 9 eyes, each sees 3 numbers (wall, green, red thing proximity)
            var num_actions = 5; // 5 possible angles agent can turn
            var temporal_window = 1; // amount of temporal memory. 0 = agent lives in-the-moment :)
            var network_size = num_inputs * temporal_window + num_actions * temporal_window + num_inputs;

            // the value function network computes a value of taking any of the possible actions
            // given an input state. Here we specify one explicitly the hard way
            // but user could also equivalently instead use opt.hidden_layer_sizes = [20,20]
            // to just insert simple relu hidden layers.
            var layer_defs = new List<LayerDef>();
            layer_defs.Add(new LayerDef() { type = typeof(InputLayer), out_sx = 1, out_sy = 1, out_depth = network_size });
            layer_defs.Add(new LayerDef() { type = typeof(FullConnLayer), num_neurons = 50, activation = ActivationEnum.relu });
            layer_defs.Add(new LayerDef() { type = typeof(FullConnLayer), num_neurons = 50, activation = ActivationEnum.relu });
            layer_defs.Add(new LayerDef() { type = typeof(RegressionLayer), num_neurons = num_actions });

            //            // options for the Temporal Difference learner that trains the above net
            //            // by backpropping the temporal difference learning rule.
            var tdtrainer_options = new TdTrainerOptions() { learning_rate = 0.001, momentum = 0.0, batch_size = 64, l2_decay = 0.01 };

            var opt = new Opt();
            opt.temporal_window = temporal_window;
            opt.experience_size = 30000;
            opt.start_learn_threshold = 1000;
            opt.gamma = 0.7;
            opt.learning_steps_total = 200000;
            opt.learning_steps_burnin = 3000;
            opt.epsilon_min = 0.05;
            opt.epsilon_test_time = 0.05;
            opt.layer_defs = layer_defs;
            opt.tdtrainer_options = tdtrainer_options;

            brain = new Brain(num_inputs, num_actions, opt); // woohoo

        }
        public int rad;
        public double rot1;
        public double rot2;
        public double reward_bonus;
        public double digestion_signal;
        public int prevactionix;

        public List<double[]> actions = new List<double[]>();
        public List<Eye> eyes = new List<Eye>();
        public double angle;
        public Vec p;
        public Brain brain;
        public Vec op;
        public double oangle;
        private int? actionix;

        internal void backward()
        {

            // in backward pass agent learns.
            // compute reward 
            var proximity_reward = 0.0;
            var num_eyes = this.eyes.Count;
            for (var i = 0; i < num_eyes; i++)
            {
                var e = this.eyes[i];
                // agents dont like to see walls, especially up close
                proximity_reward += e.sensed_type == 0 ? e.sensed_proximity / e.max_range : 1.0;
            }
            proximity_reward = proximity_reward / num_eyes;
            proximity_reward = Math.Min(1.0, proximity_reward * 2);

            // agents like to go straight forward
            var forward_reward = 0.0;
            if (this.actionix == 0 && proximity_reward > 0.75) forward_reward = 0.1 * proximity_reward;

            // agents like to eat good things
            var digestion_reward = this.digestion_signal;
            this.digestion_signal = 0.0;

            var reward = proximity_reward + forward_reward + digestion_reward;

            // pass to brain for learning
            this.brain.backward(reward);

        }
    }
}
