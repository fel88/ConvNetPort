using System;
using System.Collections.Generic;

namespace ConvNetLib
{
    public class NetStuff
    {
        //public Trainer trainer = new Trainer();
        public List<double[]> data;
        public List<int> labels;

        static Random r = new Random();

        public static double randi(double a, double b)
        {
            lock (r)
            {
                return Math.Floor(r.NextDouble() * (b - a) + a);
            }
        }
        public static double randf(double start, double end)
        {
            lock (r)
            {
                return r.NextDouble() * (end - start) + start;
            }
        }
        public static double randn(double mu, double std)
        {
            lock (r)
            {
                return mu + gaussRandom() * std;
            }
        }
        //bool return_v = false;
        //double v_val = 0.0;
        public static double gaussRandom()
        {
            /* if (return_v)
             {
                 return_v = false;
                 return v_val;
             }*/
            double _r;
            double u;

            do
            {
                u = 2 * r.NextDouble() - 1;
                var v = 2 * r.NextDouble() - 1;
                _r = u * u + v * v;
            } while (_r == 0 || _r > 1);

            //if (_r == 0 || _r > 1) return gaussRandom();
            var c = Math.Sqrt(-2 * Math.Log(_r) / _r);
            //  v_val = v * c; // cache this
            // return_v = true;
            return u * c;
        }

        public void random_data()
        {
            data = new List<double[]>();


            labels = new List<int>();
            for (var k = 0; k < 40; k++)
            {
                data.Add(new double[] { randf(-3, 3), randf(-3, 3) });
                labels.Add((randf(0, 1) > 0.5 ? 1 : 0));
            }

        }

        public void original_data()
        {

            data = new List<double[]>();
            labels = new List<int>();
            data.Add(new double[] { -0.4326, 1.1909 });
            labels.Add(1);
            data.Add(new double[] { 3.0, 4.0 });
            labels.Add(1);
            data.Add(new double[] { 0.1253, -0.0376 });
            labels.Add(1);
            data.Add(new double[] { 0.2877, 0.3273 });
            labels.Add(1);
            data.Add(new double[] { -1.1465, 0.1746 });
            labels.Add(1);
            data.Add(new double[] { 1.8133, 1.0139 });
            labels.Add(0);
            data.Add(new double[] { 2.7258, 1.0668 });
            labels.Add(0);
            data.Add(new double[] { 1.4117, 0.5593 });
            labels.Add(0);
            data.Add(new double[] { 4.1832, 0.3044 });
            labels.Add(0);
            data.Add(new double[] { 1.8636, 0.1677 });
            labels.Add(0);
            data.Add(new double[] { 0.5, 3.2 });
            labels.Add(1);
            data.Add(new double[] { 0.8, 3.2 });
            labels.Add(1);
            data.Add(new double[] { 1.0, -2.2 });
            labels.Add(1);

        }

        public void circle_data()
        {
            data = new List<double[]>();
            for (int i = 0; i < data.Count; i++)
            {
                data[i] = new double[2];
            }

            labels = new List<int>();
            for (var i = 0; i < 50; i++)
            {
                var r = randf(0.0, 2.0);
                var t = randf(0.0, 2 * Math.PI);
                data.Add(new double[] { r * Math.Sin(t), r * Math.Cos(t) });
                labels.Add(1);
            }
            for (var i = 0; i < 50; i++)
            {
                var r = randf(3.0, 5.0);
                //var t = convnetjs.randf(0.0, 2*Math.PI);
                var t = 2 * Math.PI * i / 50.0;
                data.Add(new[] { r * Math.Sin(t), r * Math.Cos(t) });
                labels.Add(0);
            }

        }

        public void spiral_data()
        {
            data = new List<double[]>();
            labels = new List<int>();
            var n = 100.0f;
            for (var i = 0; i < n; i++)
            {
                var r = i / n * 5 + randf(-0.1, 0.1);
                var t = 1.25 * i / n * 2 * Math.PI + randf(-0.1, 0.1);
                data.Add(new double[] { r * Math.Sin(t), r * Math.Cos(t) });
                labels.Add(1);
            }
            for (var i = 0; i < n; i++)
            {
                var r = i / n * 5 + randf(-0.1, 0.1);
                var t = 1.25 * i / n * 2 * Math.PI + Math.PI + randf(-0.1, 0.1);
                data.Add(new[] { r * Math.Sin(t), r * Math.Cos(t) });
                labels.Add(0);
            }

        }

        public void Update(Net net, Trainer trainer)
        {

            trainer.net = net;
            // forward prop the data

            var start = DateTime.Now;

            var x = new Volume(1, 1, 2);
            //x.w = data[ix];
            var avloss = 0.0;
            int iters = 0;
            for (iters = 0; iters < 20; iters++)
            {
                for (var ix = 0; ix < data.Count; ix++)
                {
                    x.W = data[ix];
                    var stats = trainer.train(x, labels[ix]);
                    avloss += stats.loss;
                }
            }
            //avloss /= N * iters;

            var end = DateTime.Now;
            var time = end - start;

            //console.log('loss = ' + avloss + ', 100 cycles through data in ' + time + 'ms');

        }
        public Net Test1()
        {
            Net net1 = new Net();
            net1.Layers.Add(new InputLayer() { OutDepth = 2 });

            net1.Layers.Add(new FullConnLayer() { Name = "fullConn1", NumInputs = 2, OutDepth = 16 });
            net1.Layers.Add(new TanhLayer() { Name = "tanh1", OutDepth = 16 });
            net1.Layers.Add(new FullConnLayer() { Name = "fullConn2", NumInputs = 16, OutDepth = 2 });
            net1.Layers.Add(new TanhLayer() { Name = "tanh2", OutDepth = 2 });
            net1.Layers.Add(new FullConnLayer() { Name = "fullConn3", NumInputs = 2, OutDepth = 2 });
            net1.Layers.Add(new SoftmaxLayer() { NumInputs = 2, in_depth = 2, in_sx = 1, in_sy = 1, OutDepth = 2 });

            foreach (var layer in net1.Layers)
            {
                layer.Init();
            }
            return net1;
        }
    }
}