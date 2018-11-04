using ConvNetLib;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;

namespace ConvNetTester
{
    public partial class fontRecognizer : Form
    {
        public fontRecognizer()
        {
            InitializeComponent();
            listView1.DoubleBuffered(true);


            for (char i = 'а'; i <= 'я'; i++)
            {
                symbols.Add(i + "");
                tags.Add(i + "");
                symbols.Add((i + "").ToUpper());
                tags.Add((i + "").ToUpper());

            }

            for (char i = 'a'; i <= 'z'; i++)
            {

                /*   if (i == 'a') continue;
                   if (i == 'c') continue;
                   if (i == 'e') continue;
                   if (i == 'p') continue;
                   if (i == 'o') continue;
                   if (i == 'x') continue;*/
                symbols.Add(i + "");
                tags.Add(i + "");
                symbols.Add((i + "").ToUpper());
                tags.Add((i + "").ToUpper());

            }

            for (char i = '0'; i <= '9'; i++)
            {
                tags.Add(i + "");
                symbols.Add(i + "");
            }
            Start = DateTime.Now;

            string[] ss = new[]
            {
                "!","@","#","$","%","^","&","*","(",")",
                "=","-","+",".",",","[","]","{","}"
            };
            foreach (var s in ss)
            {
                symbols.Add(s);
                tags.Add(s);
            }
            
            trainer = new Trainer() { method = TrainerMethodEnum.adadelta, batch_size = 20, l2_decay = 0.001 };            
            net = new Net();

            trainer.net = net;

            net.Layers.Add(new InputLayer() { out_sx = 24, out_sy = 24, out_depth = 1 });
            int fcnt = 16;
            net.Layers.Add(new ConvLayer() { Name = "conv1", in_sx = 24, in_sy = 24, sx = 5, sy = 5, in_depth = 1, filtersCnt = fcnt, stride = 1, pad = 2, activation =  ActivationEnum.relu });
            net.Layers.Add(new ReluLayer() { Name = "relu1", in_sx = 24, in_sy = 24, out_depth = fcnt });
            net.Layers.Add(new PoolLayer() { Name = "pool1", Sx = 2, Sy = 2, in_sx = 24, in_sy = 24, stride = 2, out_depth = fcnt });

            net.Layers.Add(new ConvLayer() { Name = "conv2", in_sx = 12, in_sy = 12, sx = 5, sy = 5, in_depth = 1, filtersCnt = 16, stride = 1, pad = 2, activation =  ActivationEnum.relu });
            net.Layers.Add(new ReluLayer() { Name = "relu2", in_sx = 12, in_sy = 12, out_depth = 16 });
            net.Layers.Add(new PoolLayer() { Name = "pool2", in_sx = 12, in_sy = 12, out_depth = 16, Sx = 3, Sy = 3, stride = 3 });

            /*net.Layers.Add(new ConvLayer() { Name = "conv3", in_sx = 12, in_sy = 12, sx = 5, sy = 5, in_depth = 1, filtersCnt = 16, stride = 1, pad = 2, activation = "relu" });
            net.Layers.Add(new ReluLayer() { Name = "relu3", in_sx = 12, in_sy = 12, OutDepth = 16 });
            net.Layers.Add(new PoolLayer() { Name = "pool3", in_sx = 12, in_sy = 12, OutDepth = 16, Sx = 3, Sy = 3, stride = 3 });
            */
            net.Layers.Add(new FullConnLayer() { Name = "fullConn1", out_depth = symbols.Count, NumInputs = 256 });
            net.Layers.Add(new SoftmaxLayer() { in_depth = symbols.Count, in_sx = 1, in_sy = 1, NumClasses = symbols.Count });

            net.Init();
            LoadItems();
            LoadTests();
        }

        private Trainer trainer;
        private Net net;
        private NetStuff stuff = new NetStuff();
        List<string> symbols = new List<string>();
        List<string> tags = new List<string>();

        public Bitmap Fit(Bitmap bmp, Size size)
        {
            //1.get rectangle of data
            //2. fit in new image 28x28
            ReadOnlyBitmap rom = new ReadOnlyBitmap(bmp);
            int minx = int.MaxValue;
            int miny = int.MaxValue;
            int maxy = int.MinValue;
            int maxx = int.MinValue;
            for (int i = 0; i < rom.Width; i++)
            {
                for (int j = 0; j < rom.Height; j++)
                {
                    var b = rom.GetPixel(i, j);
                    if (b < 128)
                    {
                        minx = Math.Min(minx, i);
                        miny = Math.Min(miny, j);
                        maxx = Math.Max(maxx, i);
                        maxy = Math.Max(maxy, j);
                    }
                }
            }
            var dx = (maxx - minx) + 1;
            var dy = (maxy - miny) + 1;
            Bitmap bb = new Bitmap(size.Width, size.Height);
            var gr = Graphics.FromImage(bb);
            gr.Clear(Color.White);

            float aspect = dx / (float)dy;

            float kx = 1;
            var max = Math.Max(dx, dy);
            kx = size.Width / (float)max;
            var ww = (int)(dx * kx);
            var hh = (int)(dy * kx);
            gr.Clear(Color.White);
            gr.DrawImage(bmp, new Rectangle(size.Width / 2 - ww / 2, size.Height / 2 - hh / 2, ww, hh), new Rectangle(minx, miny, dx, dy), GraphicsUnit.Pixel);
            //gr.DrawImage(bmp, new Rectangle(0,0,size.Width,size.Height), new Rectangle(minx, miny, dx, dy), GraphicsUnit.Pixel);
            //gr.DrawImage(bmp, new Rectangle(bb.Width-dx, miny, dx, dy), new Rectangle(minx, miny, dx, dy), GraphicsUnit.Pixel);
            return bb;
        }

        public void LoadItems()
        {
            FontRecognizerStuff.items.Clear();


            List<Font> fonts = new List<Font>();
            fonts.Add(new Font("Arial", 24));
            fonts.Add(new Font("Times New Roman", 24));
            fonts.Add(new Font("Times New Roman", 24, FontStyle.Italic));
            fonts.Add(new Font("Times New Roman", 24, FontStyle.Bold));

            foreach (var font in fonts)
            {
                for (int index = 0; index < symbols.Count; index++)
                {
                    Bitmap bmp2 = new Bitmap(28, 28);
                    var gr = Graphics.FromImage(bmp2);
                    var symbol = symbols[index];
                    gr.Clear(Color.White);
                    var ms = gr.MeasureString(symbol, font);
                    gr.DrawString(symbol, font, Brushes.Black, bmp2.Width / 2 - ms.Width / 2, bmp2.Height / 2 - ms.Height / 2);
                    var bmp = Fit(bmp2, new Size(28, 28));
                    ReadOnlyBitmap rom = new ReadOnlyBitmap(bmp);
                    var mi = new MnistItem() { };
                    mi.Data = new byte[rom.Width, rom.Height];
                    for (int j = 0; j < rom.Width; j++)
                    {
                        for (int k = 0; k < rom.Height; k++)
                        {
                            mi.Data[j, k] = rom.GetPixel(j, k);
                        }
                    }
                    mi.Label = tags.IndexOf(symbol.ToLower());
                    FontRecognizerStuff.items.Add(mi);

                }
            }
        }
        public void test_predict()
        {
            var num_classes = net.Layers[net.Layers.Count - 1].out_depth;
            var num_total = 0;
            var num_correct = 0;
            //document.getElementById('testset_acc').innerHTML = '';
            // grab a random test image
            for (var num = 0; num < 50; num++)
            {
                var sample = FontRecognizerStuff.sample_test_instance();
                var y = sample.label; // ground truth label

                // forward prop it through the network
                var aavg = new Volume(1, 1, num_classes, 0.0);
                // ensures we always have a list, regardless if above returns single item or list
                var xs = sample.x;

                var n = xs.Length;
                n = 1;
                for (var i = 0; i < n; i++)
                {
                    if (xs[i] is Volume)
                    {
                        var a = net.Forward(xs[i] as Volume, true);
                        aavg.addFrom(a);
                    }
                    else if (xs[i] is int)
                    {
                        var vvnew = new Volume(1, 1, num_classes, 0.0);
                        vvnew.w[0] = (int)xs[i];
                        var a = net.Forward(vvnew, true);
                        aavg.addFrom(a);
                    }
                    else if (xs[i] is bool)
                    {
                        var vvnew = new Volume(1, 1, num_classes, 0.0);
                        vvnew.w[0] = (bool)xs[i] ? 1 : 0;
                        var a = net.Forward(vvnew, true);
                        aavg.addFrom(a);
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
                var preds = new List<PredClass>();
                for (var k = 0; k < aavg.w.Length; k++)
                {
                    preds.Add(new PredClass() { k = k, p = aavg.w[k] });
                }
                preds = preds.OrderByDescending(z => z.p).ToList();

                //preds.Sort(function(a,b));{return a.p<b.p ? 1:-1;});

                var correct = preds[0].k == y;
                if (correct) num_correct++;
                num_total++;


                //var div = document.createElement('div');
                // div.className = 'testdiv';

                // draw the image into a canvas
                // draw_activations(div, xs[0], 2); // draw Vol into canv

                // add predictions
                var t = "";
                for (var k = 0; k < 3; k++)
                {
                    var col = preds[k].k == y;
                    /*var col = preds[k].k==y ?'rgb(85,187,85)' : 'rgb(187,85,85)';
      t += '<div class=\"pp\" style=\"width:' + 
          Math.floor(preds[k].p/n*100) + 
          'px; margin-left: 60px; background-color:' +
          col + ';\">' + 
          classes_txt[preds[k].k] + '</div>'*/
                }


            }

            testAccWindow.add(num_correct / (double)num_total);
            textBox2.Text = "test accuracy (200 last): " + (testAccWindow.get_average() * 100.0).ToString("F1") + "%";
            bestTestAccuracy = Math.Max(testAccWindow.get_average(), bestTestAccuracy);
        }

        public double bestTestAccuracy = 0;
        public DateTime Start;

        public void LoadTests()
        {
            FontRecognizerStuff.tests.Clear();

            List<Font> fonts = new List<Font>();
            fonts.Add(new Font("Arial", 24));
            fonts.Add(new Font("Times New Roman", 24));
            fonts.Add(new Font("Times New Roman", 24, FontStyle.Italic));
            fonts.Add(new Font("Times New Roman", 24, FontStyle.Bold));
            foreach (var font in fonts)
            {

                for (int index = 0; index < symbols.Count; index++)
                {
                    Bitmap bmp2 = new Bitmap(28, 28);
                    var gr = Graphics.FromImage(bmp2);

                    var symbol = symbols[index];
                    gr.Clear(Color.White);
                    var ms = gr.MeasureString(symbol, font);
                    gr.DrawString(symbol, font, Brushes.Black, bmp2.Width / 2 - ms.Width / 2, bmp2.Height / 2 - ms.Height / 2);
                    var bmp = Fit(bmp2, new Size(28, 28));
                    ReadOnlyBitmap rom = new ReadOnlyBitmap(bmp);
                    var mi = new MnistItem() { };
                    mi.Data = new byte[rom.Width, rom.Height];
                    for (int j = 0; j < rom.Width; j++)
                    {
                        for (int k = 0; k < rom.Height; k++)
                        {
                            mi.Data[j, k] = rom.GetPixel(j, k);
                        }
                    }
                    mi.Label = tags.IndexOf(symbol.ToLower());
                    if (mi.Id == 834)
                    {

                    }
                    if (mi.Label == 18)
                    {

                    }
                    FontRecognizerStuff.tests.Add(mi);
                }
            }

        }

        DataWinow xLossWindow = new DataWinow(100);
        DataWinow wLossWindow = new DataWinow(100);
        DataWinow trainAccWindow = new DataWinow(100);
        DataWinow valAccWindow = new DataWinow(100);
        DataWinow testAccWindow = new DataWinow(50, 1);
        public void step(MnistItemVolumePrepared sample)
        {

            int yhat;
            if (sample.isVal)
            {
                // use x to build our estimate of validation error
                net.Forward(sample.x, true);
                yhat = net.GetPrediction();
                var val_acc = yhat == sample.label ? 1.0 : 0.0;
                valAccWindow.add(val_acc);
                return; // get out
            }
            // train on it with network
            var stats = trainer.train(sample.x, sample.label);
            var lossx = stats.cost_loss;
            var lossw = stats.l2_decay_loss;

            // keep track of stats such as the average training error and loss
            yhat = net.GetPrediction();
            var train_acc = yhat == sample.label ? 1.0 : 0.0;
            xLossWindow.add(lossx);
            wLossWindow.add(lossw);
            trainAccWindow.add(train_acc);

            // visualize activations


            // log progress to graph, (full loss)
            if (FontRecognizerStuff.step_num % 200 == 0)
            {
                var xa = xLossWindow.get_average();
                var xw = wLossWindow.get_average();
                if (xa >= 0 && xw >= 0)
                { // if they are -1 it means not enough data was accumulated yet for estimates
                    //  lossGraph.add(step_num, xa + xw);
                    // lossGraph.drawSelf(document.getElementById("lossgraph"));
                }
            }

            // run prediction on test set
            if ((FontRecognizerStuff.step_num % 100 == 0 && FontRecognizerStuff.step_num > 0) || FontRecognizerStuff.step_num == 100)
            {
                test_predict();
            }

            FontRecognizerStuff.step_num++;

        }

        private bool pause = false;
        private void timer1_Tick(object sender, EventArgs e)
        {
            if (pause) return;
            if (FontRecognizerStuff.items.Any())
            {

                Stopwatch sw = new Stopwatch();
                sw.Start();
                var prep = FontRecognizerStuff.sample_training_instance();
                step(prep);
                pictureBox2.Image = (Bitmap)FontRecognizerStuff.lastitem.GetBitmap().GetBitmap();
                var p = net.GetPrediction();
                sw.Stop();
                //  if (checkBox2.Checked)
                {
                    var sub = DateTime.Now.Subtract(Start);
                    var stat = "time elapsed: " + (sub.Minutes.ToString("00") + ":" + sub.Seconds.ToString("00")) +
                               "; best: " + (bestTestAccuracy * 100.0).ToString("F1") + "%";

                    toolStripStatusLabel1.Text = stat + "; last sample: " + sw.ElapsedMilliseconds + " ms";
                }
                //  textBox6.Text = p + "";
                updateStats();


            }

        }
        public void updateStats()
        {
            listView1.Items.Clear();
            listView1.Items.Add(new ListViewItem(new string[] { "Classification loss", mnist.f2t(xLossWindow.get_average()) + "" }));
            listView1.Items.Add(new ListViewItem(new string[] { "L2 Weight decay loss", mnist.f2t(wLossWindow.get_average()) + "" }));
            listView1.Items.Add(new ListViewItem(new string[] { "Training accuracy", (100.0 * mnist.f2t(trainAccWindow.get_average())).ToString("F1") + "%" }));
            listView1.Items.Add(new ListViewItem(new string[] { "Validation accuracy", (100.0 * mnist.f2t(valAccWindow.get_average())).ToString("F1") + "%" }));
            listView1.Items.Add(new ListViewItem(new string[] { "Examples seen", FontRecognizerStuff.step_num + "" }));
        }

        private MnistSampleItem lastr = null;
        public bool Next()
        {
            //cnt++;
            var prep = FontRecognizerStuff.sample_test_instance();
            lastr = prep;
            pictureBox3.Image = prep.Item.GetBitmap().GetBitmap();
            textBox6.Text = "label: " + tags[prep.label];
            var p = TestVolume(prep.x[0] as Volume);
            /*
            net.Forward(prep.x[0] as Volume, false);

            var pps = net.GetPredictions();
            var p = pps.First().k;
            textBox1.Text = (tags[p]) + ": " + (pps.First().p * 100.0).ToString("F1") + "%";
            var pp = pps[1];
            textBox3.Text = tags[pp.k] + ": " + (pp.p * 100.0).ToString("F1") + "%";
            pp = pps[2];
            textBox4.Text = tags[pp.k] + ": " + (pp.p * 100.0).ToString("F1") + "%";*/
            if (prep.label == p)
            {
                textBox1.BackColor = Color.Green;
                textBox1.ForeColor = Color.White;
            }
            else
            {
                textBox1.BackColor = Color.Red;
                textBox1.ForeColor = Color.White;
            }
            return prep.label == p;
        }
        private void button1_Click(object sender, EventArgs e)
        {
            Next();
        }

        public int TestVolume(Volume v)
        {
            net.Forward(v, false);

            var pps = net.GetPredictions();
            var p = pps.First().k;
            textBox1.Invoke(((Action)(() =>
            {
                textBox1.Text = (tags[p]) + ": " + (pps.First().p * 100.0).ToString("F1") + "%";

            })));

            var pp = pps[1];
            textBox3.Invoke(((Action)(() =>
            {
                textBox3.Text = tags[pp.k] + ": " + (pp.p * 100.0).ToString("F1") + "%";

            })));
            pp = pps[2];
            textBox4.Invoke(((Action)(() =>
            {
                textBox4.Text = tags[pp.k] + ": " + (pp.p * 100.0).ToString("F1") + "%";

            })));

            return p;
        }

        private string lastp = "";
        private void button2_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                RecognImage(ofd.FileName);
            }

        }
        public int RecognImage(Bitmap bmp)
        {

            Bitmap zip = new Bitmap(28, 28);
            var gr = Graphics.FromImage(zip);
            float aspect = bmp.Width / (float)bmp.Height;
            float kx = 1;
            var max = Math.Max(bmp.Width, bmp.Height);
            kx = 28.0f / max;
            var ww = (int)(bmp.Width * kx);
            var hh = (int)(bmp.Height * kx);
            gr.Clear(Color.White);
            gr.DrawImage(bmp, new Rectangle((28 - ww) / 2, (28 - hh) / 2, ww, hh), new Rectangle(0, 0, bmp.Width, bmp.Height), GraphicsUnit.Pixel);
            pictureBox3.Image = zip;
            ReadOnlyBitmap rom = new ReadOnlyBitmap(zip);
            var mi = new MnistItem() { };
            mi.Data = new byte[rom.Width, rom.Height];
            for (int j = 0; j < rom.Width; j++)
            {
                for (int k = 0; k < rom.Height; k++)
                {
                    mi.Data[j, k] = rom.GetPixel(j, k);
                }
            }

            var image_dimension = 28;
            var image_channels = 1;
            // fetch the appropriate row of the training image and reshape into a Vol

            var x = new Volume(image_dimension, image_dimension, image_channels, 0.0);

            for (var dc = 0; dc < image_channels; dc++)
            {
                var i = 0;
                for (var xc = 0; xc < image_dimension; xc++)
                {
                    for (var yc = 0; yc < image_dimension; yc++)
                    {
                        // var ix = ((W * k) + i) * 4 + dc;
                        var bb = rom.GetPixel(xc, yc);
                        //x.Set(yc, xc, dc, p[ix]/255.0 - 0.5);
                        x.Set(yc, xc, dc, bb / 255.0 - 0.5);
                        i++;
                    }
                }
            }

            var p = TestVolume(x);
            return p;

        }

        public void RecognImage(string path)
        {
            var bmp = (Bitmap)Bitmap.FromFile(path);
            lastp = path;
            RecognImage(bmp);

        }

        private void button3_Click(object sender, EventArgs e)
        {
            RecognImage(lastp);
        }

        void ThreadProcessor(NetworkStream stream, object obj)
        {

            StreamReader reader = new StreamReader(stream);
            StreamWriter wrt2 = new StreamWriter(stream);

            while (true)
            {
                try
                {
                    var line = reader.ReadLine().ToUpper();

                    if (line.StartsWith("REC"))
                    {

                        var ind = line.IndexOf('=');
                        var aa = line.Substring(ind + 1).Split(new string[] { "=", ";" }, StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToArray();

                        int w = aa[0];
                        int h = aa[1];

                        Bitmap bmp = new Bitmap(w, h);
                        ReadOnlyBitmap rom = new ReadOnlyBitmap(bmp);
                        int cntr = 2;
                        for (int i = 0; i < w; i++)
                        {
                            for (int j = 0; j < h; j++)
                            {
                                rom.SetPixel3(i, j, (byte)aa[cntr]);
                                cntr++;
                            }
                        }
                        var rbmp = rom.GetBitmap();
                        var p = RecognImage(rbmp);
                        pictureBox1.Invoke((Action)(() =>
                        {
                            pictureBox1.Image = rbmp;
                        }));
                        wrt2.WriteLine(tags[p]);
                        wrt2.Flush();
                    }

                }

                catch (IOException ex)
                {
                    Console.WriteLine(ex.Message);
                    break;

                    //  TcpRoutine.ErrorSend(stream);
                }
            }
        }

        private TcpRoutine server;
        private void button4_Click(object sender, EventArgs e)
        {
            server = new TcpRoutine();
            server.InitTcp(IPAddress.Any, int.Parse(textBox5.Text), ThreadProcessor);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            pause = !pause;
        }
             

        public void AddLog(string text)
        {
            listView3.Items.Add(new ListViewItem(new string[] { DateTime.Now.ToLongTimeString(), text }));
            listView3.EnsureVisible(listView3.Items.Count - 1);
        }
        private void button6_Click(object sender, EventArgs e)
        {
            int cntr = 0;
            while (Next())
            {
                cntr++;
            }

            AddLog("error after: " + cntr + " repeats");
        }

        private void clearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            listView3.Items.Clear();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            //cnt++;

            var prep = lastr;
            pictureBox3.Image = prep.Item.GetBitmap().GetBitmap();
            textBox6.Text = "label: " + tags[prep.label];
            var p = TestVolume(prep.x[0] as Volume);

            if (prep.label == p)
            {
                textBox1.BackColor = Color.Green;
                textBox1.ForeColor = Color.White;
            }
            else
            {
                textBox1.BackColor = Color.Red;
                textBox1.ForeColor = Color.White;
            }
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            numericUpDown1.Maximum = FontRecognizerStuff.tests.Count;
            //cnt++;
            var prep = FontRecognizerStuff.GetTestSample((int)numericUpDown1.Value);
            label1.Text = "ID: " + prep.Item.Id + "; " + prep.label;

            lastr = prep;
            pictureBox3.Image = prep.Item.GetBitmap().GetBitmap();
            textBox6.Text = "label: " + tags[prep.label];
            var p = TestVolume(prep.x[0] as Volume);

            if (prep.label == p)
            {
                textBox1.BackColor = Color.Green;
                textBox1.ForeColor = Color.White;
            }
            else
            {
                textBox1.BackColor = Color.Red;
                textBox1.ForeColor = Color.White;
            }
        }

        private void clipboardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(net.GetXml());
            toolStripStatusLabel1.Text = "xml saved in clipboard";
        }

        private void fileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Xml (.xml)|*.xml";
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                var path = sfd.FileName;
                File.WriteAllText(path, net.GetXml());
                toolStripStatusLabel1.Text = "xml saved in file: " + path;
            }
        }

        private void clipboardToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            net.RestoreXml(Clipboard.GetText());
            toolStripStatusLabel1.Text = "xml parsed from clipboard";
        }

        private void fileToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Xml (.xml)|*.xml";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                net.RestoreXml(File.ReadAllText(ofd.FileName));
                toolStripStatusLabel1.Text = "xml parsed from file: " + ofd.FileName;
            }
        }
    }
}
