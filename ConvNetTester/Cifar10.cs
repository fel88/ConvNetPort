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
using System.Windows.Forms;
using static ConvNetTester.cnnutil;

namespace ConvNetTester
{
    public partial class Cifar10 : Form
    {
        public Cifar10()
        {
            InitializeComponent();
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
                break;
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
             
                Invoke(this, () => { toolStripProgressBar1.Visible = false; Enabled = true; });
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
                pictureBox1.Image = CifarStuff.random_one().Bmp;
            }
        }
        Window valAccWindow = new Window();
        void step(CifarVolumePrepared sample)
        {
            var x = sample.x;
            var y = sample.label;

            if (sample.isval)
            {
                // use x to build our estimate of validation error
                net.Forward(x);
                var yhat = net.GetPrediction();
                var val_acc = yhat == y ? 1.0 : 0.0;
                valAccWindow.add(val_acc);
                return; // get out
            }

            // train on it with network
            var stats = trainer.train(x, y);
            var lossx = stats.cost_loss;
            var lossw = stats.l2_decay_loss;
        }
        // loads a training image and trains on it with the network
        bool paused = true;
        void load_and_step()
        {
            if (paused) return;

            var sample = CifarStuff.sample_training_instance();
            step(sample); // process this image
        }
        private void timer1_Tick(object sender, EventArgs e)
        {
            load_and_step();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            paused = false;
        }
    }
}
