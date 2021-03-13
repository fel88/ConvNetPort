namespace ConvNetTester
{
    public class Item
    {
        public Item(double x, double y, int t)
        {
            type = t;
            p = new Vec(x, y);
        }
        public int age;
        public bool cleanup;
        public Vec p;
        public int rad = 10;
        public int type;
    }
}
