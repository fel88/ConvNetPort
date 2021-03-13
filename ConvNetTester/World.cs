using ConvNetLib;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ConvNetTester
{
    public class World
    {

        public List<Wall> walls = new List<Wall>();
        public List<Item> items = new List<Item>();
        public float randf(float s, float e)
        {
            return (float)(s + ((e - s) * r.NextDouble()));
        }
        Random r = new Random();
        int W = 700;
        int H = 500;
        // World object contains many agents and walls and food and stuff
        void util_add_box(List<Wall> lst, float x, float y, float w, float h)
        {
            lst.Add(new Wall(new Vec(x, y), new Vec(x + w, y)));
            lst.Add(new Wall(new Vec(x + w, y), new Vec(x + w, y + h)));
            lst.Add(new Wall(new Vec(x + w, y + h), new Vec(x, y + h)));
            lst.Add(new Wall(new Vec(x, y + h), new Vec(x, y)));
        }
        public void Init()
        {
            this.agents = new Agent[1];


            this.clock = 0;

            // set up walls in the world
            this.walls = new List<Wall>();
            var pad = 10;
            util_add_box(this.walls, pad, pad, this.W - pad * 2, this.H - pad * 2);
            util_add_box(this.walls, 100, 100, 200, 300); // inner walls
            this.walls.pop();
            util_add_box(this.walls, 400, 100, 200, 300);
            this.walls.pop();

            // set up food and poison
            this.items = new List<Item>();

            for (var k = 0; k < 30; k++)
            {
                var x = randf(20, this.W - 20);
                var y = randf(20, this.H - 20);
                var t = r.Next(1, 3); // food or poison (1 and 2)
                var it = new Item(x, y, t);
                this.items.Add(it);
            }
        }


        // line intersection helper function: does line segment (p1,p2) intersect segment (p3,p4) ?
        intersectResult line_intersect(Vec p1, Vec p2, Vec p3, Vec p4)
        {
            var denom = (p4.y - p3.y) * (p2.x - p1.x) - (p4.x - p3.x) * (p2.y - p1.y);
            if (denom == 0.0) { return null; } // parallel lines
            var ua = ((p4.x - p3.x) * (p1.y - p3.y) - (p4.y - p3.y) * (p1.x - p3.x)) / denom;
            var ub = ((p2.x - p1.x) * (p1.y - p3.y) - (p2.y - p1.y) * (p1.x - p3.x)) / denom;
            if (ua > 0.0 && ua < 1.0 && ub > 0.0 && ub < 1.0)
            {
                var up = new Vec(p1.x + ua * (p2.x - p1.x), p1.y + ua * (p2.y - p1.y));
                return new intersectResult()
                {
                    ua = ua,
                    ub = ub,
                    up = up,
                    result = true
                }; // up is intersection point
            }
            return null;
        }

        public class intersectResult
        {
            public int? type;
            public bool result;
            public double ua;
            public double ub;
            public Vec up;
        }
        intersectResult line_point_intersect(Vec p1, Vec p2, Vec p0, float rad)
        {
            var v = new Vec(p2.y - p1.y, -(p2.x - p1.x)); // perpendicular vector
            var d = Math.Abs((p2.x - p1.x) * (p1.y - p0.y) - (p1.x - p0.x) * (p2.y - p1.y));
            d = d / v.length();
            if (d > rad) { return null; }

            v.normalize();
            v.scale(d);
            var up = p0.add(v);
            double? ua = null;
            if (Math.Abs(p2.x - p1.x) > Math.Abs(p2.y - p1.y))
            {
                ua = (up.x - p1.x) / (p2.x - p1.x);
            }
            else
            {
                ua = (up.y - p1.y) / (p2.y - p1.y);
            }
            if (ua > 0.0 && ua < 1.0)
            {
                return new intersectResult() { ua = ua.Value, up = up };
            }
            return null;
        }

        // helper function to get closest colliding walls/items
        public intersectResult stuff_collide_(Vec p1, Vec p2, bool check_walls, bool check_items)
        {
            intersectResult minres = null;

            // collide with walls
            if (check_walls)
            {
                for (int i = 0, n = this.walls.Count; i < n; i++)
                {
                    var wall = this.walls[i];
                    var res = line_intersect(p1, p2, wall.p1, wall.p2);
                    if (res != null)
                    {
                        res.type = 0; // 0 is wall
                        if (minres == null) { minres = res; }
                        else
                        {
                            // check if its closer
                            if (res.ua < minres.ua)
                            {
                                // if yes replace it
                                minres = res;
                            }
                        }
                    }
                }
            }
            // collide with items
            if (check_items)
            {
                for (int i = 0, n = this.items.Count; i < n; i++)
                {
                    var it = this.items[i];
                    var res = line_point_intersect(p1, p2, it.p, it.rad);
                    if (res != null)
                    {
                        res.type = it.type; // store type of item
                        if (minres == null) { minres = res; }
                        else
                        {
                            if (res.ua < minres.ua) { minres = res; }
                        }
                    }
                }
            }

            return minres;
        }

        internal void tick()
        {
            // tick the environment
            this.clock++;

            // fix input to all agents based on environment
            // process eyes
            //this.collpoints = [];
            for (int i = 0, n = this.agents.Length; i < n; i++)
            {
                var a = this.agents[i];
                for (int ei = 0, ne = a.eyes.Count; ei < ne; ei++)
                {
                    var e = a.eyes[ei];
                    // we have a line from p to p->eyep
                    var eyep = new Vec(a.p.x + e.max_range * (float)Math.Sin(a.angle + e.angle),
                                       a.p.y + e.max_range * (float)Math.Cos(a.angle + e.angle));
                    var res = this.stuff_collide_(a.p, eyep, true, true);
                    if (res != null)
                    {
                        // eye collided with wall
                        e.sensed_proximity = res.up.dist_from(a.p);
                        e.sensed_type = res.type;
                    }
                    else
                    {
                        e.sensed_proximity = e.max_range;
                        e.sensed_type = -1;
                    }
                }
            }
            // let the agents behave in the world based on their input
            for (int i = 0, n = this.agents.Length; i < n; i++)
            {
                this.agents[i].forward();
            }
            // apply outputs of agents on evironment
            for (int i = 0, n = this.agents.Length; i < n; i++)
            {
                var a = this.agents[i];
                a.op = a.p; // back up old position
                a.oangle = a.angle; // and angle

                // steer the agent according to outputs of wheel velocities
                var v = new Vec(0, a.rad / 2.0);
                v = v.rotate(a.angle + Math.PI / 2);
                var w1p = a.p.add(v); // positions of wheel 1 and 2
                var w2p = a.p.sub(v);
                var vv = a.p.sub(w2p);
                vv = vv.rotate(-a.rot1);
                var vv2 = a.p.sub(w1p);
                vv2 = vv2.rotate(a.rot2);
                var np = w2p.add(vv);
                np.scale(0.5);
                var np2 = w1p.add(vv2);
                np2.scale(0.5);
                a.p = np.add(np2);

                a.angle -= a.rot1;
                if (a.angle < 0) a.angle += 2 * Math.PI;
                a.angle += a.rot2;
                if (a.angle > 2 * Math.PI) a.angle -= 2 * Math.PI;

                // agent is trying to move from p to op. Check walls
                var res = this.stuff_collide_(a.op, a.p, true, false);
                if (res != null)
                {
                    // wall collision! reset position
                    a.p = a.op;
                }

                // handle boundary conditions
                if (a.p.x < 0) a.p.x = 0;
                if (a.p.x > this.W) a.p.x = this.W;
                if (a.p.y < 0) a.p.y = 0;
                if (a.p.y > this.H) a.p.y = this.H;
            }
            // tick all items
            var update_items = false;
            for (int i = 0, n = this.items.Count; i < n; i++)
            {
                var it = this.items[i];
                it.age += 1;

                // see if some agent gets lunch
                for (int j = 0, m = this.agents.Length; j < m; j++)
                {
                    var a = this.agents[j];
                    var d = a.p.dist_from(it.p);
                    if (d < it.rad + a.rad)
                    {

                        // wait lets just make sure that this isn't through a wall
                        var rescheck = this.stuff_collide_(a.p, it.p, true, false);
                        if (rescheck == null)
                        {
                            // ding! nom nom nom
                            if (it.type == 1) a.digestion_signal += 5.0; // mmm delicious apple
                            if (it.type == 2) a.digestion_signal += -6.0; // ewww poison
                            it.cleanup = true;
                            update_items = true;
                            break; // break out of loop, item was consumed
                        }
                    }

                }

                if (it.age > 5000 && this.clock % 100 == 0 && NetStuff.randf(0, 1) < 0.1)
                {
                    it.cleanup = true; // replace this one, has been around too long
                    update_items = true;
                }
            }
            if (update_items)
            {
                var nt = new List<Item>();
                for (int i = 0, n = this.items.Count; i < n; i++)
                {
                    var it = this.items[i];
                    if (!it.cleanup) nt.Add(it);
                }
                this.items = nt; // swap
            }
            if (this.items.Count < 30 && this.clock % 10 == 0 && NetStuff.randf(0, 1) < 0.25)
            {
                var newitx = NetStuff.randf(20, this.W - 20);
                var newity = NetStuff.randf(20, this.H - 20);
                var newitt = NetStuff.randi(1, 3); // food or poison (1 and 2)
                var newit = new Item(newitx, newity, newitt);
                this.items.Add(newit);
            }

            // agents are given the opportunity to learn based on feedback of their action on environment
            for (int i = 0, n = this.agents.Length; i < n; i++)
            {
                this.agents[i].backward();
            }
        }

        public Agent[] agents = new Agent[1] { new Agent() };
        public int clock = 0;
    }
}
