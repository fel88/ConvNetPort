using ConvNetLib;
using System;
using System.Collections.Generic;

namespace ConvNetTester
{
    public class Brain
    {
        private Net value_net;

        // A Brain object does all the magic.
        // over time it receives some inputs and some rewards
        // and its job is to set the outputs to maximize the expected reward
        public Brain(int num_states, int num_actions, Opt opt)
        {
            //var opt = opt || {};
            // in number of time steps, of temporal memory
            // the ACTUAL input to the net will be (x,a) temporal_window times, and followed by current x
            // so to have no information from previous time step going into value function, set to 0.
            //this.temporal_window = typeof opt.temporal_window !== 'undefined' ? opt.temporal_window : 1;
            // size of experience replay memory
            this.experience_size = opt.experience_size != null ? opt.experience_size : 30000;
            // number of examples in experience replay memory before we begin learning
            this.start_learn_threshold = opt.start_learn_threshold != null ? opt.start_learn_threshold : Math.Floor(Math.Min(this.experience_size.Value * 0.1, 1000));
            // gamma is a crucial parameter that controls how much plan-ahead the agent does. In [0,1]
            this.gamma = opt.gamma != null ? opt.gamma : 0.8;


            // number of steps we will learn for
            this.learning_steps_total = opt.learning_steps_total != null ? opt.learning_steps_total : 100000;
            // how many steps of the above to perform only random actions (in the beginning)?
            this.learning_steps_burnin = opt.learning_steps_burnin != null ? opt.learning_steps_burnin : 3000;
            // what epsilon value do we bottom out on? 0.0 => purely deterministic policy at end
            this.epsilon_min = opt.epsilon_min != null ? opt.epsilon_min : 0.05;
            // what epsilon to use at test time? (i.e. when learning is disabled)
            this.epsilon_test_time = opt.epsilon_test_time != null ? opt.epsilon_test_time : 0.01;

            // advanced feature. Sometimes a random action should be biased towards some values
            // for example in flappy bird, we may want to choose to not flap more often
            if (opt.random_action_distribution != null)
            {
                // this better sum to 1 by the way, and be of length this.num_actions
                this.random_action_distribution = opt.random_action_distribution;
                if (this.random_action_distribution.Length != num_actions)
                {
                    throw new ArgumentException("TROUBLE. random_action_distribution should be same length as num_actions.");
                    //console.log('TROUBLE. random_action_distribution should be same length as num_actions.');
                }
                var a = this.random_action_distribution;
                var s = 0.0; for (var k = 0; k < a.Length; k++) { s += a[k]; }
                if (Math.Abs(s - 1.0) > 0.0001)
                {
                    throw new ArgumentException("TROUBLE. random_action_distribution should sum to 1!");
                    //console.log('TROUBLE. random_action_distribution should sum to 1!');
                }
            }
            else
            {
                this.random_action_distribution = new int[] { };
            }

            // states that go into neural net to predict optimal action look as
            // x0,a0,x1,a1,x2,a2,...xt
            // this variable controls the size of that temporal window. Actions are
            // encoded as 1-of-k hot vectors
            this.net_inputs = num_states * this.temporal_window + num_actions * this.temporal_window + num_states;
            this.num_states = num_states;
            this.num_actions = num_actions;
            this.window_size = Math.Max(this.temporal_window, 2); // must be at least 2, but if we want more context even more
            this.state_window = new List<double[]>();

            this.action_window = new List<int>();

            this.reward_window = new List<double>();
            this.net_window = new List<double[]>();



            // create [state -> value of all possible actions] modeling net for the value function
            var layer_defs = new List<LayerDef>();
            if (opt.layer_defs != null)
            {
                // this is an advanced usage feature, because size of the input to the network, and number of
                // actions must check out. This is not very pretty Object Oriented programming but I can't see
                // a way out of it :(
                layer_defs = opt.layer_defs;
                if (layer_defs.Count < 2)
                {
                    console.log("TROUBLE! must have at least 2 layers");
                }
                if (!(layer_defs[0].type == typeof(InputLayer)))
                {
                    console.log("TROUBLE! first layer must be input layer!");
                }
                if (!(layer_defs[layer_defs.Count - 1].type == typeof(RegressionLayer)))
                {
                    console.log("TROUBLE! last layer must be input regression!");
                }
                if (layer_defs[0].out_depth * layer_defs[0].out_sx * layer_defs[0].out_sy != this.net_inputs)
                {
                    console.log("TROUBLE! Number of inputs must be num_states * temporal_window + num_actions * temporal_window + num_states!");
                }
                if (layer_defs[layer_defs.Count - 1].num_neurons != this.num_actions)
                {
                    console.log("TROUBLE! Number of regression neurons should be num_actions!");
                }
            }
            else
            {
                // create a very simple neural net by default
                layer_defs.Add(new LayerDef() { type = typeof(InputLayer), out_sx = 1, out_sy = 1, out_depth = this.net_inputs });
                if (opt.hidden_layer_sizes != null)
                {
                    // allow user to specify this via the option, for convenience
                    var hl = opt.hidden_layer_sizes;
                    for (var k = 0; k < hl.Length; k++)
                    {
                        layer_defs.Add(new LayerDef() { type = typeof(FullConnLayer), num_neurons = hl[k], activation = ActivationEnum.relu }); // relu by default
                    }
                }
                layer_defs.Add(new LayerDef() { type = typeof(RegressionLayer), num_neurons = num_actions }); // value function output
            }
            this.value_net = new Net();
            this.value_net.makeLayers(layer_defs);

            // and finally we need a Temporal Difference Learning trainer!
            var tdtrainer_options = new TdTrainerOptions { learning_rate = 0.01, momentum = 0.0, batch_size = 64, l2_decay = 0.01 };
            if (opt.tdtrainer_options != null)
            {
                tdtrainer_options = opt.tdtrainer_options; // allow user to overwrite this
            }

            this.tdtrainer = new SGDTrainer(this.value_net, tdtrainer_options);

            // experience replay
            this.experience = new List<ConvNetTester.Experience>();

            // various housekeeping variables
            this.age = 0; // incremented every backward()
            this.forward_passes = 0; // incremented every forward()
            this.epsilon = 1.0; // controls exploration exploitation tradeoff. Should be annealed over time
            this.latest_reward = 0;
            this.last_input_array = new double[] { };
            this.average_reward_window = new cnnutil.Window(1000, 10);
            this.average_loss_window = new cnnutil.Window(1000, 10);
            this.learning = true;
        }
        public int temporal_window;
        public int net_inputs;
        public int num_states;
        public int num_actions;
        public int window_size;
        public List<double[]> state_window;
        public List<int> action_window;
        public List<double> reward_window = new List<double>();
        public List<double[]> net_window = new List<double[]>();
        private double? epsilon_test_time;
        private double? epsilon_min;
        private int? learning_steps_burnin;
        private int? learning_steps_total;
        private int? experience_size;
        private double? gamma;
        public double? start_learn_threshold;
        public int[] random_action_distribution;
        public Trainer tdtrainer;
        public int age;
        public int forward_passes;
        public double? epsilon;
        public double latest_reward;
        public double[] last_input_array;
        public bool learning;
        public List<Experience> experience = new List<Experience>();
        public cnnutil.Window average_reward_window;
        public cnnutil.Window average_loss_window;
        public double[] getNetInput(double[] xt)
        {
            // return s = (x,a,x,a,x,a,xt) state vector. 
            // It's a concatenation of last window_size (x,a) pairs and current state x
            var w = new List<double>();
            w.AddRange(xt); // start with current state
                            // and now go backwards and append states and actions from history temporal_window times
            var n = this.window_size;
            for (var k = 0; k < this.temporal_window; k++)
            {
                // state
                w.AddRange(this.state_window[n - 1 - k]);
                // action, encoded as 1-of-k indicator vector. We scale it up a bit because
                // we dont want weight regularization to undervalue this information, as it only exists once
                var action1ofk = new double[] { (this.num_actions) };
                for (var q = 0; q < this.num_actions; q++) action1ofk[q] = 0.0;
                action1ofk[this.action_window[n - 1 - k]] = 1.0 * this.num_states;
                w.AddRange(action1ofk);
            }
            return w.ToArray();
        }
        PolicyReturn policy(double[] s)
        {
            // compute the value of doing any action in this state
            // and return the argmax action and its value
            var svol = new Volume(1, 1, this.net_inputs);
            svol.w = s;
            var action_values = this.value_net.Forward(svol);
            var maxk = 0;
            var maxval = action_values.w[0];
            for (var k = 1; k < this.num_actions; k++)
            {
                if (action_values.w[k] > maxval) { maxk = k; maxval = action_values.w[k]; }
            }
            return new PolicyReturn() { action = maxk, value = maxval };
        }

        public class PolicyReturn
        {
            public int action;
            public double value;
        }
        int? random_action()
        {
            // a bit of a helper function. It returns a random action
            // we are abstracting this away because in future we may want to 
            // do more sophisticated things. For example some actions could be more
            // or less likely at "rest"/default state.
            if (this.random_action_distribution.Length == 0)
            {
                return NetStuff.randi(0, this.num_actions);
            }
            else
            {
                // okay, lets do some fancier sampling:
                var p = NetStuff.randf(0, 1.0);
                var cumprob = 0.0;
                for (var k = 0; k < this.num_actions; k++)
                {
                    cumprob += this.random_action_distribution[k];
                    if (p < cumprob) { return k; }
                }
            }
            return null;
        }

        public int? forward(double[] input_array)
        {

            // compute forward (behavior) pass given the input neuron signals from body
            this.forward_passes += 1;
            this.last_input_array = input_array; // back this up

            // create network input
            int? action;
            double[] net_input = null;
            if (this.forward_passes > this.temporal_window)
            {
                // we have enough to actually do something reasonable
                net_input = this.getNetInput(input_array);
                if (this.learning)
                {
                    // compute epsilon for the epsilon-greedy policy
                    this.epsilon = Math.Min(1.0, Math.Max(
                        this.epsilon_min.Value,
                        1.0 - (this.age - this.learning_steps_burnin.Value) / (double)(this.learning_steps_total - this.learning_steps_burnin)

                        ));
                }
                else
                {
                    this.epsilon = this.epsilon_test_time; // use test-time value
                }
                var rf = NetStuff.randf(0, 1);
                if (rf < this.epsilon)
                {
                    // choose a random action with epsilon probability
                    action = this.random_action();
                }
                else
                {
                    // otherwise use our policy to make decision
                    var maxact = this.policy(net_input);
                    action = maxact.action;
                }
            }
            else
            {
                // pathological case that happens first few iterations 
                // before we accumulate window_size inputs
                net_input = new double[] { };
                action = this.random_action();
            }

            // remember the state and action we took for backward pass
            this.net_window.shift();
            this.net_window.Add(net_input);
            this.state_window.shift();
            this.state_window.Add(input_array);
            this.action_window.shift();
            this.action_window.Add(action.Value);

            return action;

        }

        public void backward(double reward)
        {
            this.latest_reward = reward;
            this.average_reward_window.add(reward);
            this.reward_window.shift();
            this.reward_window.Add(reward);

            if (!this.learning) { return; }

            // various book-keeping
            this.age += 1;

            // it is time t+1 and we have to store (s_t, a_t, r_t, s_{t+1}) as new experience
            // (given that an appropriate number of state measurements already exist, of course)
            if (this.forward_passes > this.temporal_window + 1)
            {
                //var e = new Experience();
                //var n = this.window_size;
                //e.state0 = this.net_window[n - 2];
                //e.action0 = this.action_window[n - 2];
                //e.reward0 = this.reward_window[n - 2];
                //e.state1 = this.net_window[n - 1];
                //if (this.experience.Count < this.experience_size)
                //{
                //    this.experience.Add(e);
                //}
                //else
                //{
                //    // replace. finite memory!
                //    var ri = NetStuff.randi(0, this.experience_size.Value);
                //    this.experience[ri] = e;
                //}
            }
            double avcost = 0.0;
            // learn based on experience, once we have some samples to go on
            // this is where the magic happens...
            if (this.experience.Count > this.start_learn_threshold)
            {
                avcost = 0.0;
                for (var k = 0; k < this.tdtrainer.batch_size; k++)
                {
                    var re = NetStuff.randi(0, this.experience.Count);
                    var e = this.experience[re];
                    var x = new Volume(1, 1, this.net_inputs);
                    x.w = e.state0;
                    var maxact = this.policy(e.state1);
                    var r = e.reward0 + this.gamma * maxact.value;
                    var ystruct = new UnknownClass1() { dim = e.action0, val = r.Value };
                    var loss = this.tdtrainer.train(x, ystruct);
                    avcost += loss.loss;
                }
                avcost = avcost / this.tdtrainer.batch_size;
                this.average_loss_window.add(avcost);
            }


        }
    }
}
