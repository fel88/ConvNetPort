using ConvNetLib;
using ConvNetTester.Properties;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace ConvNetTester
{
    public partial class qlearn : Form
    {
        public qlearn()
        {
            InitializeComponent();
            listView1.DoubleBuffered(true);
            w = new World();
            w.Init();
            w.agents = new[] { new Agent() };
            gofast();
            RecreateGraphics();

            var asm = Assembly.GetExecutingAssembly();
            var nms = asm.GetManifestResourceNames();

            using (Stream stream = asm.GetManifestResourceStream(nms.First(z => z.Contains("qlearn.json"))))
            using (StreamReader reader = new StreamReader(stream))
            {
                string result = reader.ReadToEnd();
                richTextBox1.Text = result;
            }
        }

        public void RecreateGraphics()
        {
            bmp = new Bitmap(pictureBox1.Width, pictureBox1.Height);
            gr = Graphics.FromImage(bmp);
            gr.SmoothingMode = SmoothingMode.AntiAlias;
        }
        Bitmap bmp;
        Graphics gr;

        World w;
        int simspeed = 2;
        void goveryfast()
        {
            clearInterval(current_interval_id);
            current_interval_id = setInterval(tick, 1);
            skipdraw = true;
            simspeed = 3;
        }

        private void clearInterval(int current_interval_id)
        {

        }

        public void tick()
        {
            w.tick();
            if (!skipdraw || w.clock % 50 == 0)
            {
                draw();
                draw_stats();
                draw_net();
            }
        }
        private int setInterval(Action tick, int v)
        {
            timer1.Interval = v;
            return 0;
        }

        void gofast()
        {
            clearInterval(current_interval_id);
            current_interval_id = setInterval(tick, 1);
            skipdraw = false;
            simspeed = 2;
        }
        void gonormal()
        {
            clearInterval(current_interval_id);
            current_interval_id = setInterval(tick, 30);
            skipdraw = false;
            simspeed = 1;
        }
        void goslow()
        {
            clearInterval(current_interval_id);
            current_interval_id = setInterval(tick, 200);
            skipdraw = false;
            simspeed = 0;
        }

        void savenet()
        {
            //var j = w.agents[0].brain.value_net.toJSON();
            // var t = JSON.stringify(j);
            // document.getElementById('tt').value = t;
        }

        void loadnet()
        {
            //var t = document.getElementById('tt').value;
            // var j = JSON.parse(t);
            //  w.agents[0].brain.value_net.fromJSON(j);
            stoplearn(); // also stop learning
            gonormal();
        }

        void startlearn()
        {
            w.agents[0].brain.learning = true;
        }
        void stoplearn()
        {
            w.agents[0].brain.learning = false;
        }

        void reload()
        {
            w.agents = new Agent[] { new Agent() }; // this should simply work. I think... ;\
            reward_graph = new cnnvis.Graph(); // reinit
        }

        public void draw()
        {
            gr.Clear(Color.White);
            var agents = w.agents;

            // draw walls in environment

            for (int i = 0, n = w.walls.Count; i < n; i++)
            {
                var q = w.walls[i];
                gr.DrawLine(Pens.Black, q.p1.x, q.p1.y, q.p2.x, q.p2.y);
            }

            // draw agents
            // color agent based on reward it is experiencing at the moment
            //var r = (int)Math.Floor(agents[0].brain.latest_reward * 200);
            var r = 200;
            if (r > 255) r = 255; if (r < 0) r = 0;
            //ctx.fillStyle = "rgb(" + r + ", 150, 150)";
            // ctx.strokeStyle = "rgb(0,0,0)";
            for (int i = 0, n = agents.Length; i < n; i++)
            {
                var a = agents[i];

                // draw agents body
                gr.FillEllipse(new SolidBrush(Color.FromArgb(r, 150, 150)), a.op.x - a.rad, a.op.y - a.rad, a.rad * 2, a.rad * 2); ;
                gr.DrawEllipse(Pens.Black, a.op.x - a.rad, a.op.y - a.rad, a.rad * 2, a.rad * 2); ;
                //ctx.beginPath();
                //ctx.arc(a.op.x, a.op.y, a.rad, 0, Math.PI * 2, true);
                //ctx.fill();
                //ctx.stroke();

                // draw agents sight
                for (int ei = 0, ne = a.eyes.Count; ei < ne; ei++)
                {
                    var e = a.eyes[ei];
                    var sr = e.sensed_proximity;
                    Pen strokeStyle = null;
                    if (e.sensed_type == -1 || e.sensed_type == 0)
                    {
                        strokeStyle = Pens.Black; // wall or nothing
                    }
                    if (e.sensed_type == 1) { strokeStyle = new Pen(Color.FromArgb(255, 150, 150)); } // apples
                    if (e.sensed_type == 2) { strokeStyle = new Pen(Color.FromArgb(150, 255, 150)); } // poison
                    //ctx.beginPath();
                    gr.DrawLine(strokeStyle, a.op.x, a.op.y, a.op.x + sr * Math.Sin(a.oangle + e.angle),
                               a.op.y + sr * Math.Cos(a.oangle + e.angle));
                    //ctx.moveTo(a.op.x, a.op.y);
                    /*ctx.lineTo(a.op.x + sr * Math.Sin(a.oangle + e.angle),
                               a.op.y + sr * Math.Cos(a.oangle + e.angle));*/
                    //ctx.stroke();
                }
            }

            // draw items

            for (int i = 0, n = w.items.Count; i < n; i++)
            {
                var it = w.items[i];
                Brush br = null;
                if (it.type == 1) br = new SolidBrush(Color.FromArgb(255, 150, 150));
                if (it.type == 2) br = new SolidBrush(Color.FromArgb(150, 255, 150));


                gr.FillEllipse(br, it.p.x - it.rad, it.p.y - it.rad, it.rad * 2, it.rad * 2);
                gr.DrawEllipse(Pens.Black, it.p.x - it.rad, it.p.y - it.rad, it.rad * 2, it.rad * 2);
                //ctx.beginPath();
                //ctx.arc(it.p.x, it.p.y, it.rad, 0, Math.PI * 2, true);
                //ctx.fill();
                //ctx.stroke();
            }

            //w.agents[0].brain.visSelf(document.getElementById('brain_info_div'));
            UpdateStatus();

            pictureBox1.Image = bmp;
        }

        public void UpdateStatus()
        {
            listView1.BeginUpdate();
            listView1.Items.Clear();
            listView1.Items.Add(new ListViewItem(new string[] { "experience replay size: ", w.agents[0].brain.experience.Count + "" }));
            listView1.Items.Add(new ListViewItem(new string[] { "exploration epsilon: ", w.agents[0].brain.epsilon + "" }));
            listView1.Items.Add(new ListViewItem(new string[] { "age: ", w.agents[0].brain.age + "" }));
            listView1.Items.Add(new ListViewItem(new string[] { "average Q-learning loss: ", w.agents[0].brain.average_loss_window.get_average() + "" }));
            listView1.Items.Add(new ListViewItem(new string[] { "smooth - ish reward: ", w.agents[0].brain.average_reward_window.get_average() + "" }));
            listView1.EndUpdate();

        }


        int current_interval_id;
        bool skipdraw = false;
        private object window;
        private cnnvis.Graph reward_graph;

        public void draw_stats()
        {

        }
        public void draw_net()
        {

        }
        private void timer1_Tick(object sender, EventArgs _e)
        {
            tick();
        }

        private void pictureBox1_SizeChanged(object sender, EventArgs e)
        {
            RecreateGraphics();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            goslow();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            gofast();
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {

        }
    }

    public class Eye
    {

        public double angle;
        public int max_range;
        public double sensed_proximity;
        public int? sensed_type;
        private double v;

        public Eye(double _angle)
        {
            angle = _angle;
            this.max_range = 85;
            this.sensed_proximity = 85; // what the eye is seeing. will be set in world.tick()
            this.sensed_type = -1; // what does the eye see?
        }
    }

    public class Item
    {
        public Item(double x, double y, int t)
        {
            type = t;
            p = new Vec(x, y);
        }
        public int age;
        public bool cleanup;
        public Vec p;
        public int rad = 10;
        public int type;
    }
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
    public class Vec
    {
        public Vec() { }
        public Vec(double _x, double _y) { x = _x; y = _y; }
        public double x;
        public double y;

        public double length()
        {
            return Math.Sqrt(x * x + y * y);
        }
        public Vec rotate(double a)
        {  // CLOCKWISE
            return new Vec(this.x * Math.Cos(a) + this.y * Math.Sin(a),
                           -this.x * Math.Sin(a) + this.y * Math.Cos(a));
        }
        internal void normalize()
        {
            var l = length();
            x /= l;
            y /= l;
        }

        internal void scale(double d)
        {
            x *= d;
            y *= d;
        }
        public Vec sub(Vec v) { return new Vec(this.x - v.x, this.y - v.y); }
        internal Vec add(Vec v)
        {
            return new Vec(x + v.x, y + v.y);
        }

        internal float dist_from(Vec v)
        {
            return (float)Math.Sqrt(Math.Pow(this.x - v.x, 2) + Math.Pow(this.y - v.y, 2));
        }
    }
    public class Wall
    {
        public Wall(Vec v1, Vec v2)
        {
            p1 = v1;
            p2 = v2;
        }
        public Vec p1;
        public Vec p2;
    }

    public class World
    {

        public List<Wall> walls = new List<Wall>();
        public List<Item> items = new List<Item>();
        public float randf(float s, float e)
        {
            return (float)(s + ((e - s) * r.NextDouble()));
        }
        Random r = new Random();
        int W = 700;
        int H = 500;
        // World object contains many agents and walls and food and stuff
        void util_add_box(List<Wall> lst, float x, float y, float w, float h)
        {
            lst.Add(new Wall(new Vec(x, y), new Vec(x + w, y)));
            lst.Add(new Wall(new Vec(x + w, y), new Vec(x + w, y + h)));
            lst.Add(new Wall(new Vec(x + w, y + h), new Vec(x, y + h)));
            lst.Add(new Wall(new Vec(x, y + h), new Vec(x, y)));
        }
        public void Init()
        {
            this.agents = new Agent[1];


            this.clock = 0;

            // set up walls in the world
            this.walls = new List<Wall>();
            var pad = 10;
            util_add_box(this.walls, pad, pad, this.W - pad * 2, this.H - pad * 2);
            util_add_box(this.walls, 100, 100, 200, 300); // inner walls
            this.walls.pop();
            util_add_box(this.walls, 400, 100, 200, 300);
            this.walls.pop();

            // set up food and poison
            this.items = new List<Item>();

            for (var k = 0; k < 30; k++)
            {
                var x = randf(20, this.W - 20);
                var y = randf(20, this.H - 20);
                var t = r.Next(1, 3); // food or poison (1 and 2)
                var it = new Item(x, y, t);
                this.items.Add(it);
            }
        }


        // line intersection helper function: does line segment (p1,p2) intersect segment (p3,p4) ?
        intersectResult line_intersect(Vec p1, Vec p2, Vec p3, Vec p4)
        {
            var denom = (p4.y - p3.y) * (p2.x - p1.x) - (p4.x - p3.x) * (p2.y - p1.y);
            if (denom == 0.0) { return null; } // parallel lines
            var ua = ((p4.x - p3.x) * (p1.y - p3.y) - (p4.y - p3.y) * (p1.x - p3.x)) / denom;
            var ub = ((p2.x - p1.x) * (p1.y - p3.y) - (p2.y - p1.y) * (p1.x - p3.x)) / denom;
            if (ua > 0.0 && ua < 1.0 && ub > 0.0 && ub < 1.0)
            {
                var up = new Vec(p1.x + ua * (p2.x - p1.x), p1.y + ua * (p2.y - p1.y));
                return new intersectResult()
                {
                    ua = ua,
                    ub = ub,
                    up = up,
                    result = true
                }; // up is intersection point
            }
            return null;
        }

        public class intersectResult
        {
            public int? type;
            public bool result;
            public double ua;
            public double ub;
            public Vec up;
        }
        intersectResult line_point_intersect(Vec p1, Vec p2, Vec p0, float rad)
        {
            var v = new Vec(p2.y - p1.y, -(p2.x - p1.x)); // perpendicular vector
            var d = Math.Abs((p2.x - p1.x) * (p1.y - p0.y) - (p1.x - p0.x) * (p2.y - p1.y));
            d = d / v.length();
            if (d > rad) { return null; }

            v.normalize();
            v.scale(d);
            var up = p0.add(v);
            double? ua = null;
            if (Math.Abs(p2.x - p1.x) > Math.Abs(p2.y - p1.y))
            {
                ua = (up.x - p1.x) / (p2.x - p1.x);
            }
            else
            {
                ua = (up.y - p1.y) / (p2.y - p1.y);
            }
            if (ua > 0.0 && ua < 1.0)
            {
                return new intersectResult() { ua = ua.Value, up = up };
            }
            return null;
        }

        // helper function to get closest colliding walls/items
        public intersectResult stuff_collide_(Vec p1, Vec p2, bool check_walls, bool check_items)
        {
            intersectResult minres = null;

            // collide with walls
            if (check_walls)
            {
                for (int i = 0, n = this.walls.Count; i < n; i++)
                {
                    var wall = this.walls[i];
                    var res = line_intersect(p1, p2, wall.p1, wall.p2);
                    if (res != null)
                    {
                        res.type = 0; // 0 is wall
                        if (minres == null) { minres = res; }
                        else
                        {
                            // check if its closer
                            if (res.ua < minres.ua)
                            {
                                // if yes replace it
                                minres = res;
                            }
                        }
                    }
                }
            }
            // collide with items
            if (check_items)
            {
                for (int i = 0, n = this.items.Count; i < n; i++)
                {
                    var it = this.items[i];
                    var res = line_point_intersect(p1, p2, it.p, it.rad);
                    if (res != null)
                    {
                        res.type = it.type; // store type of item
                        if (minres == null) { minres = res; }
                        else
                        {
                            if (res.ua < minres.ua) { minres = res; }
                        }
                    }
                }
            }

            return minres;
        }

        internal void tick()
        {
            // tick the environment
            this.clock++;

            // fix input to all agents based on environment
            // process eyes
            //this.collpoints = [];
            for (int i = 0, n = this.agents.Length; i < n; i++)
            {
                var a = this.agents[i];
                for (int ei = 0, ne = a.eyes.Count; ei < ne; ei++)
                {
                    var e = a.eyes[ei];
                    // we have a line from p to p->eyep
                    var eyep = new Vec(a.p.x + e.max_range * (float)Math.Sin(a.angle + e.angle),
                                       a.p.y + e.max_range * (float)Math.Cos(a.angle + e.angle));
                    var res = this.stuff_collide_(a.p, eyep, true, true);
                    if (res != null)
                    {
                        // eye collided with wall
                        e.sensed_proximity = res.up.dist_from(a.p);
                        e.sensed_type = res.type;
                    }
                    else
                    {
                        e.sensed_proximity = e.max_range;
                        e.sensed_type = -1;
                    }
                }
            }
            // let the agents behave in the world based on their input
            for (int i = 0, n = this.agents.Length; i < n; i++)
            {
                this.agents[i].forward();
            }
            // apply outputs of agents on evironment
            for (int i = 0, n = this.agents.Length; i < n; i++)
            {
                var a = this.agents[i];
                a.op = a.p; // back up old position
                a.oangle = a.angle; // and angle

                // steer the agent according to outputs of wheel velocities
                var v = new Vec(0, a.rad / 2.0);
                v = v.rotate(a.angle + Math.PI / 2);
                var w1p = a.p.add(v); // positions of wheel 1 and 2
                var w2p = a.p.sub(v);
                var vv = a.p.sub(w2p);
                vv = vv.rotate(-a.rot1);
                var vv2 = a.p.sub(w1p);
                vv2 = vv2.rotate(a.rot2);
                var np = w2p.add(vv);
                np.scale(0.5);
                var np2 = w1p.add(vv2);
                np2.scale(0.5);
                a.p = np.add(np2);

                a.angle -= a.rot1;
                if (a.angle < 0) a.angle += 2 * Math.PI;
                a.angle += a.rot2;
                if (a.angle > 2 * Math.PI) a.angle -= 2 * Math.PI;

                // agent is trying to move from p to op. Check walls
                var res = this.stuff_collide_(a.op, a.p, true, false);
                if (res != null)
                {
                    // wall collision! reset position
                    a.p = a.op;
                }

                // handle boundary conditions
                if (a.p.x < 0) a.p.x = 0;
                if (a.p.x > this.W) a.p.x = this.W;
                if (a.p.y < 0) a.p.y = 0;
                if (a.p.y > this.H) a.p.y = this.H;
            }
            // tick all items
            var update_items = false;
            for (int i = 0, n = this.items.Count; i < n; i++)
            {
                var it = this.items[i];
                it.age += 1;

                // see if some agent gets lunch
                for (int j = 0, m = this.agents.Length; j < m; j++)
                {
                    var a = this.agents[j];
                    var d = a.p.dist_from(it.p);
                    if (d < it.rad + a.rad)
                    {

                        // wait lets just make sure that this isn't through a wall
                        var rescheck = this.stuff_collide_(a.p, it.p, true, false);
                        if (rescheck == null)
                        {
                            // ding! nom nom nom
                            if (it.type == 1) a.digestion_signal += 5.0; // mmm delicious apple
                            if (it.type == 2) a.digestion_signal += -6.0; // ewww poison
                            it.cleanup = true;
                            update_items = true;
                            break; // break out of loop, item was consumed
                        }
                    }

                }

                if (it.age > 5000 && this.clock % 100 == 0 && NetStuff.randf(0, 1) < 0.1)
                {
                    it.cleanup = true; // replace this one, has been around too long
                    update_items = true;
                }
            }
            if (update_items)
            {
                var nt = new List<Item>();
                for (int i = 0, n = this.items.Count; i < n; i++)
                {
                    var it = this.items[i];
                    if (!it.cleanup) nt.Add(it);
                }
                this.items = nt; // swap
            }
            if (this.items.Count < 30 && this.clock % 10 == 0 && NetStuff.randf(0, 1) < 0.25)
            {
                var newitx = NetStuff.randf(20, this.W - 20);
                var newity = NetStuff.randf(20, this.H - 20);
                var newitt = NetStuff.randi(1, 3); // food or poison (1 and 2)
                var newit = new Item(newitx, newity, newitt);
                this.items.Add(newit);
            }

            // agents are given the opportunity to learn based on feedback of their action on environment
            for (int i = 0, n = this.agents.Length; i < n; i++)
            {
                this.agents[i].backward();
            }
        }

        public Agent[] agents = new Agent[1] { new Agent() };
        public int clock = 0;
    }
    public static class Extensions
    {

        public static void shift(this IList l)
        {
            if (l.Count > 0)
            {
                l.RemoveAt(0);
            }
        }
        public static T shift<T>(this IList l)
        {
            if (l.Count > 0)
            {
                var temp = l[0];
                l.RemoveAt(0);
                return (T)temp;
            }
            return default(T);
        }
        public static void pop(this List<Wall> w)
        {
            w.RemoveAt(w.Count - 1);
        }
        public static void DrawLine(this Graphics gr, Pen p, double x1, double y1, double x2, double y2)
        {
            gr.DrawLine(p, (float)x1, (float)y1, (float)x2, (float)y2);
        }
        public static void DrawEllipse(this Graphics gr, Pen p, double x1, double y1, double x2, double y2)
        {
            gr.DrawEllipse(p, (float)x1, (float)y1, (float)x2, (float)y2);
        }
        public static void FillEllipse(this Graphics gr, Brush p, double x1, double y1, double x2, double y2)
        {
            gr.FillEllipse(p, (float)x1, (float)y1, (float)x2, (float)y2);
        }
    }

    public class SGDTrainer : Trainer
    {
        private TdTrainerOptions tdtrainer_options;

        public SGDTrainer(Net value_net, TdTrainerOptions tdtrainer_options)
        {
            this.net = value_net;
            this.tdtrainer_options = tdtrainer_options;
        }
    }

    public static class cnnutil
    {
        public static double f2t(double x, int? d = null)
        {
            if (d == null) { d = 5; }
            var dd = 1.0 * Math.Pow(10, d.Value);
            return Math.Floor(x * dd) / dd;
        }
        // a window stores _size_ number of values
        // and returns averages. Useful for keeping running
        // track of validation or training accuracy during SGD
        public class Window
        {
            int? minsize;
            List<double> v = new List<double>();
            int? size;
            double sum;

            public Window() { }
            public Window(int? size, int? minsize = null)
            {
                this.v = new List<double>();
                this.size = size == null ? 100 : size;
                this.minsize = minsize == null ? 20 : minsize;
                this.sum = 0;
            }
            public void add(double x)
            {
                this.v.Add(x);
                this.sum += x;
                if (this.v.Count > this.size)
                {
                    var xold = this.v.shift<double>();
                    this.sum -= xold;
                }
            }
            public void reset()
            {
                this.v = new List<double>();
                this.sum = 0;
            }
            public double get_average()
            {
                if (v.Count < minsize) return -1;
                else return sum / v.Count;
            }
        }
    }
    public class cnnvis
    {
        internal class Graph
        {
            public Graph()
            {
            }
        }
    }

    // An agent is in state0 and does action0
    // environment then assigns reward0 and provides new state, state1
    // Experience nodes store all this information, which is used in the
    // Q-learning update step
    public class Experience
    {
        public Experience() { }
        public Experience(double[] state0, int action0, double reward0, double[] state1)
        {
            this.state0 = state0;
            this.action0 = action0;
            this.reward0 = reward0;
            this.state1 = state1;
        }
        public double[] state0;
        public int action0;
        public double reward0;
        public double[] state1;
    }
}
