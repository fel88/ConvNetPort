using System;

namespace ConvNetTester
{
    public class Vec
    {
        public Vec() { }
        public Vec(double _x, double _y) { x = _x; y = _y; }
        public double x;
        public double y;

        public double length()
        {
            return Math.Sqrt(x * x + y * y);
        }
        public Vec rotate(double a)
        {  // CLOCKWISE
            return new Vec(this.x * Math.Cos(a) + this.y * Math.Sin(a),
                           -this.x * Math.Sin(a) + this.y * Math.Cos(a));
        }
        internal void normalize()
        {
            var l = length();
            x /= l;
            y /= l;
        }

        internal void scale(double d)
        {
            x *= d;
            y *= d;
        }
        public Vec sub(Vec v) { return new Vec(this.x - v.x, this.y - v.y); }
        internal Vec add(Vec v)
        {
            return new Vec(x + v.x, y + v.y);
        }

        internal float dist_from(Vec v)
        {
            return (float)Math.Sqrt(Math.Pow(this.x - v.x, 2) + Math.Pow(this.y - v.y, 2));
        }
    }
}
