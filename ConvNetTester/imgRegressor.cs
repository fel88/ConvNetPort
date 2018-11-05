using System;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ConvNetLib;
using System.Collections.Generic;

namespace ConvNetTester
{
    public partial class imgRegressor : Form
    {
        public imgRegressor()
        {
            InitializeComponent();
            numericUpDown1.Value = mod_skip_draw;
            create();
        }

        public void create(int cnt = 6, int neurons = 20)
        {
            /*
           layer_defs = [];
layer_defs.push({type:'input', out_sx:1, out_sy:1, out_depth:2}); // 2 inputs: x, y 
layer_defs.push({type:'fc', num_neurons:20, activation:'relu'});
layer_defs.push({type:'fc', num_neurons:20, activation:'relu'});
layer_defs.push({type:'fc', num_neurons:20, activation:'relu'});
layer_defs.push({type:'fc', num_neurons:20, activation:'relu'});
layer_defs.push({type:'fc', num_neurons:20, activation:'relu'});
layer_defs.push({type:'fc', num_neurons:20, activation:'relu'});
layer_defs.push({type:'fc', num_neurons:20, activation:'relu'});
layer_defs.push({type:'regression', num_neurons:3}); // 3 outputs: r,g,b 

net = new convnetjs.Net();
net.makeLayers(layer_defs);

trainer = new convnetjs.SGDTrainer(net, {learning_rate:0.01, momentum:0.9, batch_size:5, l2_decay:0.0});
           */
            List<LayerDef> defs = new List<LayerDef>();
            net = new Net();
            defs.Add(new LayerDef() { type = typeof(InputLayer), out_sx = 1, out_sy = 1, out_depth = 2 });
            defs.Add(new LayerDef() { type = typeof(FullConnLayer), num_neurons = 20, activation = ActivationEnum.relu });
            defs.Add(new LayerDef() { type = typeof(FullConnLayer), num_neurons = 20, activation = ActivationEnum.relu });
            defs.Add(new LayerDef() { type = typeof(FullConnLayer), num_neurons = 20, activation = ActivationEnum.relu });
            defs.Add(new LayerDef() { type = typeof(FullConnLayer), num_neurons = 20, activation = ActivationEnum.relu });
            defs.Add(new LayerDef() { type = typeof(FullConnLayer), num_neurons = 20, activation = ActivationEnum.relu });
            defs.Add(new LayerDef() { type = typeof(FullConnLayer), num_neurons = 20, activation = ActivationEnum.relu });
            defs.Add(new LayerDef() { type = typeof(FullConnLayer), num_neurons = 20, activation = ActivationEnum.relu });
            defs.Add(new LayerDef() { type = typeof(RegressionLayer), num_neurons = 3 });

            net.makeLayers(defs);
            trainer = new Trainer();
            trainer.batch_size = 5;
            trainer.net = net;

            trainer.momentum = 0.9;
            trainer.learning_rate = 0.01;
            trainer.l2_decay = 0;

        }

        private Net net;
        private NetStuff stuff = new NetStuff();
        private void button1_Click(object sender, EventArgs e)
        {
            UpdateFiles(textBox1.Text);
        }

        public void UpdateFiles(string path)
        {
            var d = new DirectoryInfo(path);
            listView1.Items.Clear();
            foreach (var file in d.GetFiles())
            {
                listView1.Items.Add(new ListViewItem(file.Name) { Tag = file });
            }
        }

        private Trainer trainer;

        private ReadOnlyBitmap bmp;
        private ReadOnlyBitmap outbmp;
        private int batches_per_iteration = 100;
        private double smooth_loss;
        public void update()
        {
            if (bmp == null) return;
            // forward prop the data
            var W = bmp.Width;
            var H = bmp.Height;



            var v = new Volume(1, 1, 2);
            double loss = 0;
            double lossi = 0;
            var N = batches_per_iteration;

            Stopwatch sw = new Stopwatch();
            sw.Start();

            for (var iters = 0; iters < trainer.batch_size; iters++)
            {
                for (var i = 0; i < N; i++)
                {
                    // sample a coordinate
                    var x = (int)NetStuff.randi(0, W);
                    var y = (int)NetStuff.randi(0, H);
                    var ix = ((W * y) + x) * 4;
                    var arr = bmp.GetPixel3(x, y).Select(z => z / 255.0).ToArray();
                    v.w[0] = (x - W / 2) / (double)W;
                    v.w[1] = (y - H / 2) / (double)H;
                    var stats = trainer.train(v, arr);
                    loss += stats.loss;
                    lossi += 1;
                }
            }
            loss /= lossi;

            if (counter == 0) smooth_loss = loss;
            else smooth_loss = 0.99 * smooth_loss + 0.01 * loss;
            var t = ""; t += "loss: " + smooth_loss + "; iteration: " + counter;
            textBox2.Invoke((Action)(() =>
            {
                textBox2.Text = t;

            }));


            sw.Stop();
            var ms = sw.ElapsedMilliseconds;

        }

        private int counter = 0;
        public int mod_skip_draw = 150;
        public void draw()
        {


            // iterate over all pixels in the target array, evaluate them
            // and draw
            var W = bmp.Width;
            var H = bmp.Height;

            var v = new Volume(1, 1, 2);
            for (var x = 0; x < W; x++)
            {
                v.w[0] = (x - W / 2) / (double)W;
                for (var y = 0; y < H; y++)
                {
                    v.w[1] = (y - H / 2) / (double)H;

                    var r = net.Forward(v, false);
                    outbmp.SetPixel(x, y,
                        new byte[] { (byte)(255 * r.w[0]), (byte)(255 * r.w[1]), (byte)(255 * r.w[2]), 255 });

                }
            }
            pictureBox2.Image = outbmp.GetBitmap();
        }

        private bool flag = false;

        private long drawms;
        private bool pause = false;
        private void timer1_Tick(object sender, EventArgs e)
        {
            if (pause) return;
            if (bmp == null) return;

            if (flag) return;
            flag = true;

            // while (true)
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                update();

                sw.Stop();
                var ms = sw.ElapsedMilliseconds;

                if (outbmp != null)
                {

                    if (!(counter % mod_skip_draw != 0))
                    {
                        sw = new Stopwatch();
                        sw.Start();
                        draw();
                        sw.Stop();
                        drawms = sw.ElapsedMilliseconds;
                    }
                }
                counter++;

                toolStripStatusLabel1.Text = ms + " ms; Last draw ms: " + drawms + " ms";
                flag = false;
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            var vv = Enum.GetValues(typeof(PictureBoxSizeMode)).Length;
            if (((int)pictureBox1.SizeMode) == (vv - 1))
            {
                pictureBox1.SizeMode = 0;

            }
            else
            {
                pictureBox1.SizeMode++;
            }

        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            var vv = Enum.GetValues(typeof(PictureBoxSizeMode)).Length;

            if (((int)pictureBox2.SizeMode) == (vv - 1))
            {
                pictureBox2.SizeMode = 0;

            }
            else
            {
                pictureBox2.SizeMode++;
            }



        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            mod_skip_draw = (int)numericUpDown1.Value;
        }


        private void listView1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            bool temp = pause;

            if (listView1.SelectedItems.Count > 0)
            {
                pause = true;

                var t = listView1.SelectedItems[0].Tag as FileInfo;
                var ld = (Bitmap)Bitmap.FromFile(t.FullName);
                bmp = new ReadOnlyBitmap(ld);
                Bitmap bmpo = new Bitmap(bmp.Width, bmp.Height);
                outbmp = new ReadOnlyBitmap(bmpo);
                pictureBox1.Image = ld;
                pause = temp;
            }


        }

        private void button2_Click(object sender, EventArgs e)
        {
            create();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            pause = !checkBox1.Checked;
        }
    }
}
