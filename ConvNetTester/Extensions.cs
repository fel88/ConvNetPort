using System.Collections;
using System.Collections.Generic;
using System.Drawing;

namespace ConvNetTester
{
    public static class Extensions
    {

        public static void shift(this IList l)
        {
            if (l.Count > 0)
            {
                l.RemoveAt(0);
            }
        }
        public static T shift<T>(this IList l)
        {
            if (l.Count > 0)
            {
                var temp = l[0];
                l.RemoveAt(0);
                return (T)temp;
            }
            return default(T);
        }
        public static void pop(this List<Wall> w)
        {
            w.RemoveAt(w.Count - 1);
        }
        public static void DrawLine(this Graphics gr, Pen p, double x1, double y1, double x2, double y2)
        {
            gr.DrawLine(p, (float)x1, (float)y1, (float)x2, (float)y2);
        }
        public static void DrawEllipse(this Graphics gr, Pen p, double x1, double y1, double x2, double y2)
        {
            gr.DrawEllipse(p, (float)x1, (float)y1, (float)x2, (float)y2);
        }
        public static void FillEllipse(this Graphics gr, Brush p, double x1, double y1, double x2, double y2)
        {
            gr.FillEllipse(p, (float)x1, (float)y1, (float)x2, (float)y2);
        }
    }
}
