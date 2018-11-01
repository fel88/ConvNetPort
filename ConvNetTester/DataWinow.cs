using System.Collections.Generic;
using System.Linq;

namespace ConvNetTester
{
    public class DataWinow
    {
        public DataWinow(int size = 100, int minsize = 20)
        {
            this.v = new List<double>();
            this.Size = size;
            this.MinSize = minsize;
            this.sum = 0;
        }


        private List<double> v = new List<double>();
        private double sum = 0;
        public int Size;
        public int MinSize;

        public void add(double x)
        {
            this.v.Add(x);
            this.sum += x;
            if (this.v.Count > this.Size)
            {
                var xold = this.v.First();
                v.RemoveAt(0);

                this.sum -= xold;
            }

        }

        public double get_average()
        {
            if (this.v.Count < this.MinSize) return -1;
            else return this.sum / this.v.Count;
        }

        public void reset()
        {
            this.v = new List<double>();
            this.sum = 0;
        }

    }
}