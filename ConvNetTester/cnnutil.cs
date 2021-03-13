using System;
using System.Collections.Generic;

namespace ConvNetTester
{
    public static class cnnutil
    {
        public static double f2t(double x, int? d = null)
        {
            if (d == null) { d = 5; }
            var dd = 1.0 * Math.Pow(10, d.Value);
            return Math.Floor(x * dd) / dd;
        }
        // a window stores _size_ number of values
        // and returns averages. Useful for keeping running
        // track of validation or training accuracy during SGD
        public class Window
        {
            int? minsize;
            List<double> v = new List<double>();
            int? size;
            double sum;

            public Window() { }
            public Window(int? size, int? minsize = null)
            {
                this.v = new List<double>();
                this.size = size == null ? 100 : size;
                this.minsize = minsize == null ? 20 : minsize;
                this.sum = 0;
            }
            public void add(double x)
            {
                this.v.Add(x);
                this.sum += x;
                if (this.v.Count > this.size)
                {
                    var xold = this.v.shift<double>();
                    this.sum -= xold;
                }
            }
            public void reset()
            {
                this.v = new List<double>();
                this.sum = 0;
            }
            public double get_average()
            {
                if (v.Count < minsize) return -1;
                else return sum / v.Count;
            }
        }
    }
}
