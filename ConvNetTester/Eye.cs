namespace ConvNetTester
{
    public class Eye
    {

        public double angle;
        public int max_range;
        public double sensed_proximity;
        public int? sensed_type;
        private double v;

        public Eye(double _angle)
        {
            angle = _angle;
            this.max_range = 85;
            this.sensed_proximity = 85; // what the eye is seeing. will be set in world.tick()
            this.sensed_type = -1; // what does the eye see?
        }
    }
}
