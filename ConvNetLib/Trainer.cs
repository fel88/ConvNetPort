using System;
using System.Collections.Generic;

namespace ConvNetLib
{
    public class Trainer
    {
        private int k;
        public int batch_size = 10;
        public Net net;
        public double momentum = 0.1;
        private List<double[]> gsum = new List<double[]>();
        private List<double[]> xsum = new List<double[]>();
        public string method = "sgd";
        public double l2_decay = 0.001;
        public double l1_decay = 0;
        public double learning_rate = 0.01;
        private double eps = 10e-6;
        private double ro = 0.95;

        public TrainerStat train(Volume x, object y)
        {
            TrainerStat stat = new TrainerStat();
            
            var start = DateTime.Now;

            this.net.Forward(x, true); // also set the flag that lets the net know we're just training
            var end = DateTime.Now;
            var fwd_time = end - start;

            start = DateTime.Now;
            var cost_loss = this.net.Backward(y);
            var l2_decay_loss = 0.0;
            var l1_decay_loss = 0.0;
            end = DateTime.Now;
            var bwd_time = end - start;

            this.k++;
            if (this.k % this.batch_size == 0)
            {

                var pglist = this.net.getParamsAndGrads();

                // initialize lists for accumulators. Will only be done once on first iteration
                if (this.gsum.Count == 0 && (this.method != "sgd" || this.momentum > 0.0))
                {
                    // only vanilla sgd doesnt need either lists
                    // momentum needs gsum
                    // adagrad needs gsum
                    // adadelta needs gsum and xsum
                    for (var i = 0; i < pglist.Length; i++)
                    {
                        this.gsum.Add(new double[pglist[i].Params.Length]);
                        if (this.method == "adadelta")
                        {
                            this.xsum.Add(new double[pglist[i].Params.Length]);
                        }
                        else
                        {
                            this.xsum.Add(new double[] { }); // conserve memory
                        }
                    }
                }

                // perform an update for all sets of weights
                for (var i = 0; i < pglist.Length; i++)
                {
                    var pg = pglist[i]; // param, gradient, other options in future (custom learning rate etc)
                    var p = pg.Params;
                    var g = pg.Grads;

                    // learning rate for some parameters.
                    var l2_decay_mul = pg.l2_decay_mul == null ? 1.0 : pg.l2_decay_mul.Value;
                    var l1_decay_mul = pg.l1_decay_mul == null ? 1.0 : pg.l1_decay_mul.Value;
                    var l2_decay = this.l2_decay * l2_decay_mul;
                    var l1_decay = this.l1_decay * l1_decay_mul;

                    var plen = p.Length;
                    for (var j = 0; j < plen; j++)
                    {
                        l2_decay_loss += l2_decay * p[j] * p[j] / 2; // accumulate weight decay loss
                        l1_decay_loss += l1_decay * Math.Abs(p[j]);
                        var l1grad = l1_decay * (p[j] > 0 ? 1 : -1);
                        var l2grad = l2_decay * (p[j]);

                        var gij = (l2grad + l1grad + g[j]) / this.batch_size; // raw batch gradient

                        var gsumi = this.gsum[i];
                        var xsumi = this.xsum[i];
                        if (this.method == "adadelta")
                        {
                            // assume adadelta if not sgd or adagrad
                            gsumi[j] = this.ro * gsumi[j] + (1 - this.ro) * gij * gij;
                            var dx = -Math.Sqrt((xsumi[j] + this.eps) / (gsumi[j] + this.eps)) * gij;
                            xsumi[j] = this.ro * xsumi[j] + (1 - this.ro) * dx * dx; // yes, xsum lags behind gsum by 1.
                            p[j] += dx;
                        }
                        else
                        {
                            // assume SGD
                            if (this.momentum > 0.0)
                            {
                                // momentum update
                                var dx = this.momentum * gsumi[j] - this.learning_rate * gij; // step
                                gsumi[j] = dx; // back this up for next iteration of momentum
                                p[j] += dx; // apply corrected gradient
                            }
                            else
                            {
                                // vanilla sgd
                                p[j] += -this.learning_rate * gij;
                            }
                        }
                        g[j] = 0.0; // zero out gradient so that we can begin accumulating anew
                    }
                }
            }

            stat.loss = cost_loss + l1_decay_loss + l2_decay_loss;
            stat.cost_loss = cost_loss ;
            stat.l2_decay_loss = l2_decay_loss;
            stat.fwd_time =(int) fwd_time.TotalMilliseconds;
            stat.bwd_time =(int) bwd_time.TotalMilliseconds;
            
            // appending softmax_loss for backwards compatibility, but from now on we will always use cost_loss
            // in future, TODO: have to completely redo the way loss is done around the network as currently 
            // loss is a bit of a hack. Ideally, user should specify arbitrary number of loss functions on any layer
            // and it should all be computed correctly and automatically. 
            /*   return {fwd_time: fwd_time, bwd_time: bwd_time, 
                       l2_decay_loss: l2_decay_loss, l1_decay_loss: l1_decay_loss,
                       cost_loss: cost_loss, softmax_loss: cost_loss, 
                       loss: cost_loss + l1_decay_loss + l2_decay_loss}*/

            return stat;
        }
    }
}