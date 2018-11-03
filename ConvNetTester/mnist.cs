using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ConvNetLib;

namespace ConvNetTester
{
    public partial class mnist : Form
    {
        public mnist()
        {
            InitializeComponent();
            listView1.DoubleBuffered(true);
            trainer = new Trainer() { method = "adadelta", batch_size = 20, l2_decay = 0.001 };

            net = new Net();

            trainer.net = net;

            net.Layers.Add(new InputLayer() { OutSx = 24, OutSy = 24, OutDepth = 1 });

            net.Layers.Add(new ConvLayer() { Name = "conv1", in_sx = 24, in_sy = 24, sx = 5, sy = 5, in_depth = 1, filtersCnt = 8, stride = 1, pad = 2, activation = ActivationEnum.relu });
            net.Layers.Add(new ReluLayer() { Name = "relu1", in_sx = 24, in_sy = 24, OutDepth = 8 });
            net.Layers.Add(new PoolLayer() { Name = "pool1", Sx = 2, Sy = 2, in_sx = 24, in_sy = 24, stride = 2, OutDepth = 8 });

            net.Layers.Add(new ConvLayer() { Name = "conv2", in_sx = 12, in_sy = 12, sx = 5, sy = 5, in_depth = 1, filtersCnt = 16, stride = 1, pad = 2, activation =  ActivationEnum.relu });
            net.Layers.Add(new ReluLayer() { Name = "relu2", in_sx = 12, in_sy = 12, OutDepth = 16 });

            net.Layers.Add(new PoolLayer() { Name = "pool2", in_sx = 12, in_sy = 12, OutDepth = 16, Sx = 3, Sy = 3, stride = 3 });

            net.Layers.Add(new FullConnLayer() { Name = "fullConn1", OutDepth = 10, NumInputs = 256 });
            net.Layers.Add(new SoftmaxLayer() { in_depth = 10, in_sx = 1, in_sy = 1, NumClasses = 10 });

            net.Init();

        }

        private Trainer trainer;
        private Net net;
        private NetStuff stuff = new NetStuff();


        //List<MnistItem> tests = new List<MnistItem>();


        public static double f2t(double x, int? d = null)
        {
            if (d == null) { d = 5; }
            var dd = 1.0 * Math.Pow(10, d.Value);
            return Math.Floor(x * dd) / dd;
        }
        public static MnistItem[] LoadImages(string imgPath, string labelsPath, bool withBitmap = false)
        {
            List<MnistItem> bmps = new List<MnistItem>();

            Stopwatch sw = new Stopwatch();
            sw.Start();
            var bytes = File.ReadAllBytes(imgPath);
            int imagesCnt = ReadInt(bytes, 4);
            int indexer = 8;
            var w = ReadInt(bytes, indexer);
            indexer += 4;
            var h = ReadInt(bytes, indexer);
            indexer += 4;
            for (int i = 0; i < imagesCnt; i++)
            {
                bmps.Add(ReadImage(bytes, indexer, w, h));
                indexer += w * h;

            }
            sw.Stop();
            var ms = sw.ElapsedMilliseconds;
            sw.Stop(); sw.Reset();
            sw.Start();
            if (withBitmap)
            {
                foreach (var mnistItem in bmps)
                {
                    mnistItem.GetBitmap();
                }
            }
            sw.Stop();
            var ms2 = sw.ElapsedMilliseconds;
            sw.Stop(); sw.Reset();
            sw.Start();

            #region load lables
            bytes = File.ReadAllBytes(labelsPath);
            imagesCnt = ReadInt(bytes, 4);
            indexer = 8;
            for (int i = 0; i < imagesCnt; i++)
            {
                bmps[i].Label = bytes[indexer];
                indexer++;
            }
            sw.Stop();
            var ms3 = sw.ElapsedMilliseconds;


            #endregion

            return bmps.ToArray();

        }

        string[] names = new string[]
        {
                "t10k-images.idx3-ubyte",
                "t10k-labels.idx1-ubyte",
                "train-images.idx3-ubyte",
                "train-labels.idx1-ubyte"
        };

        private void button1_Click(object sender, EventArgs e)
        {
            if (!Directory.Exists(textBox1.Text))
            {
                MessageBox.Show("Directory: " + textBox1.Text + " not exist", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
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
            Stopwatch sw = new Stopwatch();
            sw.Start();
            var items1 = LoadImages(Path.Combine(textBox1.Text, names[0]), Path.Combine(textBox1.Text, names[1])).ToList();
            var tests = LoadImages(Path.Combine(textBox1.Text, names[2]), Path.Combine(textBox1.Text, names[3])).ToList();
            MnistStuff.items.Clear();
            MnistStuff.items.AddRange(items1);
            MnistStuff.tests.AddRange(tests);

            sw.Stop();
            toolStripStatusLabel1.Text = "Loaded: " + (items1.Count + tests.Count) + " images; " + sw.ElapsedMilliseconds + " ms";
            Start = DateTime.Now;

        }

        public static int ReadInt(byte[] bytes, int start)
        {
            int res = 0;
            for (int i = 0; i < 4; i++)
            {
                res ^= (bytes[start + (3 - i)] << (i * 8));
            }
            return res;

        }

        public static MnistItem ReadImage(byte[] bb, int start, int w, int h)
        {
            byte[,] data = new byte[w, h];
            int cntr = 0;
            for (int j = 0; j < h; j++)
            {
                for (int i = 0; i < w; i++)
                {
                    data[i, j] = bb[start + cntr];
                    cntr++;
                }
            }
            return new MnistItem() { Data = data };
        }

        private int cnt = 0;

        private void button2_Click(object sender, EventArgs e)
        {
            //cnt++;
            var prep = MnistStuff.sample_test_instance();

            pictureBox1.Image = prep.Item.GetBitmap().GetBitmap();
            //textBox3.Text = "label: " + prep.label;

            net.Forward(prep.x[0] as Volume, false);

            var p = net.GetPrediction();
            textBox8.Text = "predict: " + p;
            if (prep.label == p)
            {
                textBox8.BackColor = Color.Green;
                textBox8.ForeColor = Color.White;
            }
            else
            {
                textBox8.BackColor = Color.Red;
                textBox8.ForeColor = Color.White;
            }
        }

        public static Volume SampleToVolume(MnistItem item)
        {
            var x = new Volume(28, 28, 1, 0.0);
            int cntr = 0;
            for (int i = 0; i < item.Data.GetLength(0); i++)
            {
                for (int j = 0; j < item.Data.GetLength(1); j++)
                {
                    x.W[cntr] = item.Data[i, j] / 255.0;
                    cntr++;
                }
            }
            x = Volume.Augment(x, 24);
            return x;
        }
        // evaluate current network on test set


        public int testImage(MnistItem item)
        {
            var x = SampleToVolume(item);
            //var x = convnetjs.img_to_vol(img);
            var out_p = net.Forward(x, true);


            // var vis_elt = document.getElementById("visnet");
            //visualize_activations(net, vis_elt);

            var preds = new List<PredClass>();
            for (var k = 0; k < out_p.W.Length; k++)
            {
                preds.Add(new PredClass() { k = k, p = out_p.W[k] });
            }
            preds = preds.OrderByDescending(z => z.p).ToList();
            //preds.sort(function(a, b){return a.p < b.p ? 1 : -1;});

            // add predictions
            //var div = document.createElement('div');
            // div.className = 'testdiv';

            // draw the image into a canvas
            // draw_activations_COLOR(div, x, 2);

            //  var probsdiv = document.createElement('div');


            return preds[0].k;
            var t = "";
            for (var k = 0; k < 3; k++)
            {
                var col = k == 0;
                /*  var cc=? 'rgb(85,187,85)' : 'rgb(187,85,85)';
                  t += '<div class=\"pp\" style=\"width:' + Math.floor(preds[k].p/1*100) + 'px; background-color:' + col +
                       ';\">' + classes_txt[preds[k].k] + '</div>'*/
            }

            //   probsdiv.innerHTML = t;
            //    probsdiv.className = 'probsdiv';
            //     div.appendChild(probsdiv);

            // add it into DOM
            /*   $
               (div).prependTo($("#testset_vis")).
               hide().fadeIn('slow').slideDown('slow');
               if ($
               (".probsdiv").length > 200)
               {
               $
                   ("#testset_vis > .probsdiv").last().remove(); // pop to keep upper bound of shown items
               }*/
        }

        public void test_predict()
        {
            var num_classes = net.Layers[net.Layers.Count - 1].OutDepth;
            var num_total = 0;
            var num_correct = 0;
            //document.getElementById('testset_acc').innerHTML = '';
            // grab a random test image
            for (var num = 0; num < 50; num++)
            {
                var sample = MnistStuff.sample_test_instance();
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
                        vvnew.W[0] = (int)xs[i];
                        var a = net.Forward(vvnew, true);
                        aavg.addFrom(a);
                    }
                    else if (xs[i] is bool)
                    {
                        var vvnew = new Volume(1, 1, num_classes, 0.0);
                        vvnew.W[0] = (bool)xs[i] ? 1 : 0;
                        var a = net.Forward(vvnew, true);
                        aavg.addFrom(a);
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
                var preds = new List<PredClass>();
                for (var k = 0; k < aavg.W.Length; k++)
                {
                    preds.Add(new PredClass() { k = k, p = aavg.W[k] });
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
            textBox7.Text = "test accuracy (200 last): " + (testAccWindow.get_average() * 100.0).ToString("F1") + "%";
            bestTestAccuracy = Math.Max(testAccWindow.get_average(), bestTestAccuracy);

        }
        public double bestTestAccuracy = 0;
        public DateTime Start;
        public void AppendLayerVisualization(Layer layer)
        {
            return;
            Panel panel = new Panel() { Width = 300 };
            panel.BorderStyle = BorderStyle.FixedSingle;
            var pb = new PictureBox();
            //   panel.Controls.Add(pb);
            flowLayoutPanel1.Controls.Add(panel);

            var mma = simplify.maxmin(layer.Out.W);
            var t = "max activation: " + f2t(mma.maxv) + ", min: " + f2t(mma.minv);
            TextBox tb = new TextBox();
            tb.Text = t;
            tb.Width = 300;
            tb.ReadOnly = true;
            panel.Controls.Add(tb);
        }

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
            if (MnistStuff.step_num % 200 == 0)
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
            if ((MnistStuff.step_num % 100 == 0 && MnistStuff.step_num > 0) || MnistStuff.step_num == 100)
            {
                test_predict();
            }

            MnistStuff.step_num++;
            /*
             * 
  var x = sample.x;
  var y = sample.label;

  if(sample.isval) {
    // use x to build our estimate of validation error
    net.forward(x);
    var yhat = net.getPrediction();
    var val_acc = yhat === y ? 1.0 : 0.0;
    valAccWindow.add(val_acc);
    return; // get out
  }

  // train on it with network
  var stats = trainer.train(x, y);
  var lossx = stats.cost_loss;
  var lossw = stats.l2_decay_loss;

  // keep track of stats such as the average training error and loss
  var yhat = net.getPrediction();
  var train_acc = yhat === y ? 1.0 : 0.0;
  xLossWindow.add(lossx);
  wLossWindow.add(lossw);
  trainAccWindow.add(train_acc);

  // visualize training status
  var train_elt = document.getElementById("trainstats");
  train_elt.innerHTML = '';
  var t = 'Forward time per example: ' + stats.fwd_time + 'ms';
  train_elt.appendChild(document.createTextNode(t));
  train_elt.appendChild(document.createElement('br'));
  var t = 'Backprop time per example: ' + stats.bwd_time + 'ms';
  train_elt.appendChild(document.createTextNode(t));
  train_elt.appendChild(document.createElement('br'));
  var t = 'Classification loss: ' + f2t(xLossWindow.get_average());
  train_elt.appendChild(document.createTextNode(t));
  train_elt.appendChild(document.createElement('br'));
  var t = 'L2 Weight decay loss: ' + f2t(wLossWindow.get_average());
  train_elt.appendChild(document.createTextNode(t));
  train_elt.appendChild(document.createElement('br'));
  var t = 'Training accuracy: ' + f2t(trainAccWindow.get_average());
  train_elt.appendChild(document.createTextNode(t));
  train_elt.appendChild(document.createElement('br'));
  var t = 'Validation accuracy: ' + f2t(valAccWindow.get_average());
  train_elt.appendChild(document.createTextNode(t));
  train_elt.appendChild(document.createElement('br'));
  var t = 'Examples seen: ' + step_num;
  train_elt.appendChild(document.createTextNode(t));
  train_elt.appendChild(document.createElement('br'));

  // visualize activations
  if(step_num % 100 === 0) {
    var vis_elt = document.getElementById("visnet");
    visualize_activations(net, vis_elt);
  }

  // log progress to graph, (full loss)
  if(step_num % 200 === 0) {
    var xa = xLossWindow.get_average();
    var xw = wLossWindow.get_average();
    if(xa >= 0 && xw >= 0) { // if they are -1 it means not enough data was accumulated yet for estimates
      lossGraph.add(step_num, xa + xw);
      lossGraph.drawSelf(document.getElementById("lossgraph"));
    }
  }

  // run prediction on test set
  if((step_num % 100 === 0 && step_num > 0) || step_num===100) {
    test_predict();
  }
  step_num++;
             */
        }

        private bool pause = false;
        private void timer1_Tick(object sender, EventArgs e)
        {
            if (pause) return;
            if (MnistStuff.items.Any())
            {
                //if (checkBox1.Checked)
                {
                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    var prep = MnistStuff.sample_training_instance();
                    step(prep);
                    pictureBox2.Image = (Bitmap)MnistStuff.lastitem.GetBitmap().GetBitmap();
                    var p = net.GetPrediction();
                    sw.Stop();
                    //if (checkBox2.Checked)
                    {
                        var sub = DateTime.Now.Subtract(Start);
                        var stat = "time elapsed: " + (sub.Minutes.ToString("00") + ":" + sub.Seconds.ToString("00")) +
                                   "; best: " + (bestTestAccuracy * 100.0).ToString("F1") + "%";

                        toolStripStatusLabel1.Text = stat + "; last sample: " + sw.ElapsedMilliseconds + " ms";

                        //toolStripStatusLabel1.Text = sw.ElapsedMilliseconds + " ms";
                    }
                    textBox6.Text = p + "";
                    //stuff.trainer.train(SampleToVolume(ss), new double[] { bmps[0].Label });
                    //var pred = net.GetPrediction();
                    updateStats();
                    flowLayoutPanel1.Controls.Clear();
                    for (int i = 0; i < net.Layers.Count; i++)
                    {
                        if (net.Layers[i] is SoftmaxLayer)
                        {

                            AppendLayerVisualization(net.Layers[i]);
                        }

                    }
                }

            }

        }

        public void updateStats()
        {

            listView1.Items.Clear();
            listView1.Items.Add(new ListViewItem(new string[] { "Classification loss", f2t(xLossWindow.get_average()) + "" }));
            listView1.Items.Add(new ListViewItem(new string[] { "L2 Weight decay loss", f2t(wLossWindow.get_average()) + "" }));
            listView1.Items.Add(new ListViewItem(new string[] { "Training accuracy", (100.0 * mnist.f2t(trainAccWindow.get_average())).ToString("F1") + "%" }));
            listView1.Items.Add(new ListViewItem(new string[] { "Validation accuracy", (100.0 * mnist.f2t(valAccWindow.get_average())).ToString("F1") + "%" }));
            listView1.Items.Add(new ListViewItem(new string[] { "Examples seen", MnistStuff.step_num + "" }));



        }

        DataWinow xLossWindow = new DataWinow(100);
        DataWinow wLossWindow = new DataWinow(100);
        DataWinow trainAccWindow = new DataWinow(100);
        DataWinow valAccWindow = new DataWinow(100);
        DataWinow testAccWindow = new DataWinow(50, 1);

        private void button3_Click(object sender, EventArgs e)
        {
            pause = !pause;
        }

    }

}
