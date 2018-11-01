using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Xml.Linq;

namespace ConvNetLib
{
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
        public Volume Forward(Volume v, bool isTraining)
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
    }
    public class PredClass
    {
        public int k;
        public double p;
    }

}
