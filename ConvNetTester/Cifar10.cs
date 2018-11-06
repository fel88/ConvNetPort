using ConvNetLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using static ConvNetTester.cnnutil;

namespace ConvNetTester
{
    public partial class Cifar10 : Form
    {
        public Cifar10()
        {
            InitializeComponent();
            listView1.DoubleBuffered(true);
            Init();


        }
        Net net;

        Trainer trainer;
        void Init()
        {
            List<LayerDef> layer_defs = new List<LayerDef>();

            layer_defs.Add(new LayerDef() { type = typeof(InputLayer), out_sx = 32, out_sy = 32, out_depth = 3 });
            layer_defs.Add(new LayerDef() { type = typeof(ConvLayer), sx = 5, filters = 16, stride = 1, pad = 2, activation = ActivationEnum.relu });
            layer_defs.Add(new LayerDef() { type = typeof(PoolLayer), sx = 2, stride = 2 });
            layer_defs.Add(new LayerDef() { type = typeof(ConvLayer), sx = 5, filters = 20, stride = 1, pad = 2, activation = ActivationEnum.relu });
            layer_defs.Add(new LayerDef() { type = typeof(PoolLayer), sx = 2, stride = 2 });
            layer_defs.Add(new LayerDef() { type = typeof(ConvLayer), sx = 5, filters = 20, stride = 1, pad = 2, activation = ActivationEnum.relu });
            layer_defs.Add(new LayerDef() { type = typeof(PoolLayer), sx = 2, stride = 2 });
            layer_defs.Add(new LayerDef() { type = typeof(SoftmaxLayer), num_classes = 10 });

            net = new Net();
            net.makeLayers(layer_defs);
            //trainer = new convnetjs.SGDTrainer(net, {method:'adadelta', batch_size:4, l2_decay:0.0001});\n\
            trainer = new SGDTrainer(net, new TdTrainerOptions() { batch_size = 4, l2_decay = 0.0001, method = TrainerMethodEnum.adadelta });
        }

        public void progressReport(float t)
        {
            statusStrip1.Invoke((Action)(() =>
            {
                toolStripProgressBar1.Value = (int)Math.Round(t * 100);
            }));
        }


        public void SetStatusInfo(string text)
        {
            statusStrip1.Invoke((Action)(() =>
            {
                toolStripStatusLabel1.Text = text;
            }));

        }

        public void Invoke(Control c, Action act)
        {
            c.Invoke((Action)(() => { act(); }));
        }
        private void button1_Click(object sender, EventArgs e)
        {
            if (!Directory.Exists(textBox1.Text))
            {
                MessageBox.Show("Directory: " + textBox1.Text + " not exist", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            var dir = new DirectoryInfo(textBox1.Text);
            List<string> names = new List<string>();
            for (int i = 0; i < 5; i++)
            {
                names.Add($"data_batch_{ i + 1}.bin");
            }
            foreach (var item in names)
            {
                var path = Path.Combine(textBox1.Text, item);
                if (!File.Exists(path))
                {
                    MessageBox.Show("File: " + path + " not exist", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }


            toolStripProgressBar1.Visible = true;

            Thread th = new Thread(() =>
            {
                Stopwatch sw = new Stopwatch();
                Invoke(this, () => { Enabled = false; });

                sw.Start();
                SetStatusInfo("Tests loading..");
                var tests = Stuff.LoadCifarImages(Path.Combine(textBox1.Text, "test_batch.bin"), progressReport).ToList();

                CifarStuff.items.Clear();
                CifarStuff.tests.Clear();
                foreach (var item in names)
                {
                    SetStatusInfo($"Data file: {item} loading..");
                    var items1 = Stuff.LoadCifarImages(Path.Combine(textBox1.Text, names[0]), progressReport).ToList();
                    CifarStuff.items.AddRange(items1);
                }

                sw.Stop();
                CifarStuff.tests.AddRange(tests);
                SetStatusInfo("Loaded: " + (CifarStuff.items.Count + CifarStuff.tests.Count) + " images; " + sw.ElapsedMilliseconds + " ms");

                Invoke(this, () =>
                {
                    toolStripProgressBar1.Visible = false; Enabled = true;
                    button4.Enabled = true;
                });
            });
            th.IsBackground = true;
            th.Start();


            var lbls = File.ReadAllLines(Path.Combine(textBox1.Text, "batches.meta.txt"));
            CifarStuff.labels = lbls.ToArray();


            Start = DateTime.Now;
        }
        DateTime Start;

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (CifarStuff.items.Any())
            {

                var t = CifarStuff.sample_test_instance();
                pictureBox1.Image = t.Bmp;
                net.Forward(t.x);
                label1.Text = CifarStuff.labels[t.label];

                var pps = net.GetPredictions();
                var p = pps.First().k;
                textBox5.Invoke(((Action)(() =>
                {
                    textBox5.Text = (CifarStuff.labels[p]) + ": " + (pps.First().p * 100.0).ToString("F1") + "%";

                })));

                var pp = pps[1];
                textBox3.Invoke(((Action)(() =>
                {
                    textBox3.Text = CifarStuff.labels[pp.k] + ": " + (pp.p * 100.0).ToString("F1") + "%";

                })));
                pp = pps[2];
                textBox4.Invoke(((Action)(() =>
                {
                    textBox4.Text = CifarStuff.labels[pp.k] + ": " + (pp.p * 100.0).ToString("F1") + "%";

                })));




                if (t.label == p)
                {
                    textBox5.BackColor = Color.Green;
                    textBox5.ForeColor = Color.White;
                }
                else
                {
                    textBox5.BackColor = Color.Red;
                    textBox5.ForeColor = Color.White;
                }
            }
        }

        cnnvis.Graph lossGraph = new cnnvis.Graph();
        Window xLossWindow = new cnnutil.Window(100);
        Window wLossWindow = new cnnutil.Window(100);
        Window trainAccWindow = new cnnutil.Window(100);
        Window valAccWindow = new cnnutil.Window(100);
        Window testAccWindow = new cnnutil.Window(50, 1);
        int step_num = 0;

        public void UpdateStats(TrainerStat stats)
        {
            listView1.Items.Clear();
            // visualize training status
            listView1.Items.Add(new ListViewItem(new string[] { "Forward time per example: ", stats.fwd_time + "ms" }) { });
            listView1.Items.Add(new ListViewItem(new string[] { "Backprop time per example: ", stats.bwd_time + "ms" }) { });
            listView1.Items.Add(new ListViewItem(new string[] { "Classification loss: ", cnnutil.f2t(xLossWindow.get_average()) + "" }) { });
            listView1.Items.Add(new ListViewItem(new string[] { "L2 Weight decay loss: ", cnnutil.f2t(wLossWindow.get_average()) + "" }) { });
            listView1.Items.Add(new ListViewItem(new string[] { "Training accuracy: ", (100.0 * cnnutil.f2t(trainAccWindow.get_average())).ToString("F1") + "%" }) { });
            listView1.Items.Add(new ListViewItem(new string[] { "Validation accuracy: ", (100.0 * cnnutil.f2t(valAccWindow.get_average())).ToString("F1") + "%" }) { });
            listView1.Items.Add(new ListViewItem(new string[] { "Examples seen: ", step_num + "" }) { });
        }

        void step(CifarVolumePrepared sample)
        {
            var x = sample.x;
            var y = sample.label;


            int yhat;
            if (sample.isval)
            {
                // use x to build our estimate of validation error
                net.Forward(x);
                yhat = net.getPrediction();
                var val_acc = yhat == y ? 1.0 : 0.0;
                valAccWindow.add(val_acc);
                return; // get out
            }

            // train on it with network
            var stats = trainer.train(x, y);
            var lossx = stats.cost_loss;
            var lossw = stats.l2_decay_loss;

            // keep track of stats such as the average training error and loss
            yhat = net.getPrediction();
            var train_acc = yhat == y ? 1.0 : 0.0;
            xLossWindow.add(lossx);
            wLossWindow.add(lossw);
            trainAccWindow.add(train_acc);

            UpdateStats(stats);

            step_num++;
        }

        // loads a training image and trains on it with the network
        bool paused = true;
        void load_and_step()
        {
            if (paused) return;

            var sample = CifarStuff.sample_training_instance();
            pictureBox2.Image = sample.Bmp;
            step(sample); // process this image


            //  testAccWindow.add(num_correct / (double)num_total);
            // textBox2.Text = "test accuracy (200 last): " + (testAccWindow.get_average() * 100.0).ToString("F1") + "%";
            //bestTestAccuracy = Math.Max(testAccWindow.get_average(), bestTestAccuracy);
        }
        private void timer1_Tick(object sender, EventArgs e)
        {
            load_and_step();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            paused = false;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            paused = !paused;
        }

        void reset_all()
        {

            // reinit trainer
            trainer = new SGDTrainer(net, new TdTrainerOptions() { learning_rate = trainer.learning_rate, momentum = trainer.momentum, batch_size = trainer.batch_size, l2_decay = trainer.l2_decay });
            //update_net_param_display();

            // reinit windows that keep track of val/train accuracies
            xLossWindow.reset();
            wLossWindow.reset();
            trainAccWindow.reset();
            valAccWindow.reset();
            testAccWindow.reset();
            lossGraph = new cnnvis.Graph(); // reinit graph too
            step_num = 0;

        }
        private void button5_Click(object sender, EventArgs e)
        {

            trainer.learning_rate = 0.0001;
            trainer.momentum = 0.9;
            trainer.batch_size = 2;
            trainer.l2_decay = 0.00001;
            reset_all();
            var txt = File.ReadAllText("cifar10_snapshot.json");

            var ser = new JavaScriptSerializer();
            dynamic rets = ser.DeserializeObject(txt);
            int cnt = 0;
            net = new Net();
            foreach (var item in rets["layers"])
            {
                Layer l = null;
                switch ((string)item["layer_type"])
                {
                    case "input":
                        {
                            l = new InputLayer();

                        }
                        break;
                    case "pool":
                        {
                            l = new PoolLayer();

                        }
                        break;
                    case "conv":
                        {
                            l = new ConvLayer();

                        }
                        break;
                    case "fc":
                        {
                            l = new FullConnLayer();

                        }
                        break;
                    case "softmax":
                        {
                            l = new SoftmaxLayer();

                        }
                        break;
                    case "relu":
                        {
                            l = new ReluLayer();

                        }
                        break;
                }
                l.fromJson(item);
                            net.layers.Add(l);
                //net.layers[cnt].fromJson(item);
                //cnt++;
            }


            //var jobj = JsonObject.Parse(new JsonParseContext() { Text = txt });
        }
    }
}
