using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Xml.Linq;

namespace ConvNetLib
{

    public class console
    {
        public static void log(string txt)
        {
            //throw new ArgumentException(txt);
        }
    }
    public class Net
    {
        public List<Layer> Layers = new List<Layer>();
        public string GetXml()
        {
            List<string> xml = new List<string>();
            xml.Add("<?xml version=\"1.0\"?>");
            xml.Add("<root>");
            foreach (var layer in Layers)
            {
                xml.Add(layer.GetXmlSection());
            }
            xml.Add("</root>");
            var str = xml.Aggregate("", (x, y) => x + y + Environment.NewLine);
            return str;
        }

        // forward prop the network. 
        // The trainer class passes is_training = true, but when this function is
        // called from outside (not from the trainer), it defaults to prediction mode
        public Volume Forward(Volume v, bool isTraining=false)
        {

            var act = this.Layers[0].Forward(v, isTraining);
            for (var i = 1; i < this.Layers.Count; i++)
            {
                act = this.Layers[i].Forward(act, isTraining);
            }
            return act;
        }
        public PredClass[] GetPredictions()
        {
            var S = this.Layers[this.Layers.Count - 1];

            var p = S.Out.W;
            List<PredClass> ppc = new List<PredClass>();
            for (var i = 0; i < p.Length; i++)
            {
                ppc.Add(new PredClass() { k = i, p = p[i] });

            }
            return ppc.OrderByDescending(z => z.p).ToArray(); // return index of the class with highest class probability
        }

        public int GetPrediction()
        {
            // this is a convenience function for returning the argmax
            // prediction, assuming the last layer of the net is a softmax
            var S = this.Layers[this.Layers.Count - 1];

            var p = S.Out.W;
            var maxv = p[0];
            var maxi = 0;
            for (var i = 1; i < p.Length; i++)
            {
                if (p[i] > maxv)
                {
                    maxv = p[i];
                    maxi = i;
                }
            }
            return maxi; // return index of the class with highest class probability
        }


        public void Init()
        {
            foreach (var l in Layers)
            {
                l.Init();
            }
        }

        public double Backward(object y)
        {
            var N = this.Layers.Count;
            var loss = this.Layers[N - 1].Backward(y); // last layer assumed to be loss layer
            for (var i = N - 2; i >= 0; i--)
            { // first layer assumed input
                this.Layers[i].Backward(null);
            }
            return loss;
        }

        public PgListItem[] getParamsAndGrads()
        {
            // accumulate parameters and gradients for the entire network
            List<PgListItem> response = new List<PgListItem>();
            for (var i = 0; i < this.Layers.Count; i++)
            {
                var layer_reponse = this.Layers[i].GetParamsAndGrads();
                for (var j = 0; j < layer_reponse.Count(); j++)
                {
                    response.Add(layer_reponse[j]);
                }
            }
            return response.ToArray();
        }

        public void RestoreXml(string txt)
        {
            var doc = XDocument.Parse(txt);
            foreach (var descendant in doc.Descendants("layer"))
            {
                var nm = descendant.Attribute("name").Value;
                var ll = Layers.FirstOrDefault(z => z.Name == nm);
                if (ll != null)
                {
                    ll.ParseXmlSection(descendant);
                }
            }
        }


        public void assert(bool b, string txt)
        {
            if (!b)
            {
                throw new ArgumentException(txt);
            }
        }
        // desugar layer_defs for adding activation, dropout layers etc
        Layer[] desugar(List<Layer> defs)
        {
            var new_defs = new List<Layer>();
            for (var i = 0; i < defs.Count; i++)
            {
                var def = defs[i];

                if (def is SoftmaxLayer || def is SvmLayer)
                {
                    // add an fc layer here, there is no reason the user should
                    // have to worry about this and we almost always want to
                    new_defs.Add(new FullConnLayer() { num_neurons = def.num_classes });
                }
                if (def is RegressionLayer)
                {
                    // add an fc layer here, there is no reason the user should
                    // have to worry about this and we almost always want to
                    new_defs.Add(new FullConnLayer() { num_neurons = def.num_neurons });
                }

                if ((def is FullConnLayer || def is ConvLayer)
                    && def.bias_pref == null)
                {
                    def.bias_pref = 0.0;
                    if (def.activation != null && def.activation == ActivationEnum.relu)
                    {
                        def.bias_pref = 0.1; // relus like a bit of positive bias to get gradients early
                                             // otherwise it's technically possible that a relu unit will never turn on (by chance)
                                             // and will never get any gradient and never contribute any computation. Dead relu.
                    }
                }

                new_defs.Add(def);

                if (def.activation != null)
                {
                    if (def.activation == ActivationEnum.relu)
                    {
                        new_defs.Add(new ReluLayer());
                    }
                    else if (def.activation == ActivationEnum.sigmoid) { new_defs.Add(new SigmoidLayer()); }
                    else if (def.activation == ActivationEnum.tahn)
                    {
                        new_defs.Add(new TanhLayer());
                    }
                    else if (def.activation == ActivationEnum.maxout)
                    {
                        // create maxout activation, and pass along group size, if provided
                        var gs = def.group_size != null ? def.group_size : 2;
                        new_defs.Add(new MaxoutLayer()
                        {
                            group_size = gs
                        });
                    }
                    else
                    {

                        console.log("ERROR unsupported activation " + def.activation);
                    }
                }
                if (def.drop_prob != null && def is DropOutLayer)
                {
                    new_defs.Add(new DropOutLayer() { drop_prob = def.drop_prob });
                }

            }
            return new_defs.ToArray();
        }

        // takes a list of layer definitions and creates the network layer objects
        public void makeLayers(List<Layer> defs)
        {


            // few checks
            assert(defs.Count >= 2, "Error! At least one input layer and one loss layer are required.");
            assert(defs[0] is InputLayer, "Error! First layer must be the input layer, to declare size of inputs");



            defs = desugar(defs).ToList();

            // create the layers
            this.Layers = new List<Layer>();
            for (var i = 0; i < defs.Count; i++)
            {
                var def = defs[i];
                if (i > 0)
                {
                    var prev = this.Layers[i - 1];
                    def.in_sx = prev.out_sx;
                    def.in_sy = prev.out_sy;
                    def.in_depth = prev.out_depth;
                }

                Layers.Add(def);
                /*  switch (def.type)
                  {
                      case 'fc': this.layers.push(new global.FullyConnLayer(def)); break;
                      case 'lrn': this.layers.push(new global.LocalResponseNormalizationLayer(def)); break;
                      case 'dropout': this.layers.push(new global.DropoutLayer(def)); break;
                      case 'input': this.layers.push(new global.InputLayer(def)); break;
                      case 'softmax': this.layers.push(new global.SoftmaxLayer(def)); break;
                      case 'regression': this.layers.push(new global.RegressionLayer(def)); break;
                      case 'conv': this.layers.push(new global.ConvLayer(def)); break;
                      case 'pool': this.layers.push(new global.PoolLayer(def)); break;
                      case 'relu': this.layers.push(new global.ReluLayer(def)); break;
                      case 'sigmoid': this.layers.push(new global.SigmoidLayer(def)); break;
                      case 'tanh': this.layers.push(new global.TanhLayer(def)); break;
                      case 'maxout': this.layers.push(new global.MaxoutLayer(def)); break;
                      case 'svm': this.layers.push(new global.SVMLayer(def)); break;
                      default: console.log('ERROR: UNRECOGNIZED LAYER TYPE: ' + def.type);
                  }*/
            }

        }
    }

    public class PredClass
    {
        public int k;
        public double p;
    }

}
