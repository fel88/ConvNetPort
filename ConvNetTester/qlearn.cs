using ConvNetTester.Properties;
using System;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace ConvNetTester
{
    public partial class qlearn : Form
    {
        public qlearn()
        {
            InitializeComponent();
            listView1.DoubleBuffered(true);
            w = new World();
            w.Init();
            w.agents = new[] { new Agent() };
            gofast();
            RecreateGraphics();

            var asm = Assembly.GetExecutingAssembly();
            var nms = asm.GetManifestResourceNames();

            using (Stream stream = asm.GetManifestResourceStream(nms.First(z => z.Contains("qlearn.json"))))
            using (StreamReader reader = new StreamReader(stream))
            {
                string result = reader.ReadToEnd();
                richTextBox1.Text = result;
            }
        }

        public void RecreateGraphics()
        {
            bmp = new Bitmap(pictureBox1.Width, pictureBox1.Height);
            gr = Graphics.FromImage(bmp);
            gr.SmoothingMode = SmoothingMode.AntiAlias;
        }
        Bitmap bmp;
        Graphics gr;

        World w;
        int simspeed = 2;
        void goveryfast()
        {
            clearInterval(current_interval_id);
            current_interval_id = setInterval(tick, 1);
            skipdraw = true;
            simspeed = 3;
        }

        private void clearInterval(int current_interval_id)
        {

        }

        public void tick()
        {
            w.tick();
            if (!skipdraw || w.clock % 50 == 0)
            {
                draw();
                draw_stats();
                draw_net();
            }
        }
        private int setInterval(Action tick, int v)
        {
            timer1.Interval = v;
            return 0;
        }

        void gofast()
        {
            clearInterval(current_interval_id);
            current_interval_id = setInterval(tick, 1);
            skipdraw = false;
            simspeed = 2;
        }
        void gonormal()
        {
            clearInterval(current_interval_id);
            current_interval_id = setInterval(tick, 30);
            skipdraw = false;
            simspeed = 1;
        }
        void goslow()
        {
            clearInterval(current_interval_id);
            current_interval_id = setInterval(tick, 200);
            skipdraw = false;
            simspeed = 0;
        }

        void savenet()
        {
            //var j = w.agents[0].brain.value_net.toJSON();
            // var t = JSON.stringify(j);
            // document.getElementById('tt').value = t;
        }

        void loadnet()
        {
            //var t = document.getElementById('tt').value;
            // var j = JSON.parse(t);
            //  w.agents[0].brain.value_net.fromJSON(j);
            stoplearn(); // also stop learning
            gonormal();
        }

        void startlearn()
        {
            w.agents[0].brain.learning = true;
        }
        void stoplearn()
        {
            w.agents[0].brain.learning = false;
        }

        void reload()
        {
            w.agents = new Agent[] { new Agent() }; // this should simply work. I think... ;\
            reward_graph = new cnnvis.Graph(); // reinit
        }

        public void draw()
        {
            gr.Clear(Color.White);
            var agents = w.agents;

            // draw walls in environment

            for (int i = 0, n = w.walls.Count; i < n; i++)
            {
                var q = w.walls[i];
                gr.DrawLine(Pens.Black, q.p1.x, q.p1.y, q.p2.x, q.p2.y);
            }

            // draw agents
            // color agent based on reward it is experiencing at the moment
            //var r = (int)Math.Floor(agents[0].brain.latest_reward * 200);
            var r = 200;
            if (r > 255) r = 255; if (r < 0) r = 0;
            //ctx.fillStyle = "rgb(" + r + ", 150, 150)";
            // ctx.strokeStyle = "rgb(0,0,0)";
            for (int i = 0, n = agents.Length; i < n; i++)
            {
                var a = agents[i];

                // draw agents body
                gr.FillEllipse(new SolidBrush(Color.FromArgb(r, 150, 150)), a.op.x - a.rad, a.op.y - a.rad, a.rad * 2, a.rad * 2); ;
                gr.DrawEllipse(Pens.Black, a.op.x - a.rad, a.op.y - a.rad, a.rad * 2, a.rad * 2); ;
                //ctx.beginPath();
                //ctx.arc(a.op.x, a.op.y, a.rad, 0, Math.PI * 2, true);
                //ctx.fill();
                //ctx.stroke();

                // draw agents sight
                for (int ei = 0, ne = a.eyes.Count; ei < ne; ei++)
                {
                    var e = a.eyes[ei];
                    var sr = e.sensed_proximity;
                    Pen strokeStyle = null;
                    if (e.sensed_type == -1 || e.sensed_type == 0)
                    {
                        strokeStyle = Pens.Black; // wall or nothing
                    }
                    if (e.sensed_type == 1) { strokeStyle = new Pen(Color.FromArgb(255, 150, 150)); } // apples
                    if (e.sensed_type == 2) { strokeStyle = new Pen(Color.FromArgb(150, 255, 150)); } // poison
                    //ctx.beginPath();
                    gr.DrawLine(strokeStyle, a.op.x, a.op.y, a.op.x + sr * Math.Sin(a.oangle + e.angle),
                               a.op.y + sr * Math.Cos(a.oangle + e.angle));
                    //ctx.moveTo(a.op.x, a.op.y);
                    /*ctx.lineTo(a.op.x + sr * Math.Sin(a.oangle + e.angle),
                               a.op.y + sr * Math.Cos(a.oangle + e.angle));*/
                    //ctx.stroke();
                }
            }

            // draw items

            for (int i = 0, n = w.items.Count; i < n; i++)
            {
                var it = w.items[i];
                Brush br = null;
                if (it.type == 1) br = new SolidBrush(Color.FromArgb(255, 150, 150));
                if (it.type == 2) br = new SolidBrush(Color.FromArgb(150, 255, 150));


                gr.FillEllipse(br, it.p.x - it.rad, it.p.y - it.rad, it.rad * 2, it.rad * 2);
                gr.DrawEllipse(Pens.Black, it.p.x - it.rad, it.p.y - it.rad, it.rad * 2, it.rad * 2);
                //ctx.beginPath();
                //ctx.arc(it.p.x, it.p.y, it.rad, 0, Math.PI * 2, true);
                //ctx.fill();
                //ctx.stroke();
            }

            //w.agents[0].brain.visSelf(document.getElementById('brain_info_div'));
            UpdateStatus();

            pictureBox1.Image = bmp;
        }

        public void UpdateStatus()
        {
            listView1.BeginUpdate();
            listView1.Items.Clear();
            listView1.Items.Add(new ListViewItem(new string[] { "experience replay size: ", w.agents[0].brain.experience.Count + "" }));
            listView1.Items.Add(new ListViewItem(new string[] { "exploration epsilon: ", w.agents[0].brain.epsilon + "" }));
            listView1.Items.Add(new ListViewItem(new string[] { "age: ", w.agents[0].brain.age + "" }));
            listView1.Items.Add(new ListViewItem(new string[] { "average Q-learning loss: ", w.agents[0].brain.average_loss_window.get_average() + "" }));
            listView1.Items.Add(new ListViewItem(new string[] { "smooth - ish reward: ", w.agents[0].brain.average_reward_window.get_average() + "" }));
            listView1.EndUpdate();

        }


        int current_interval_id;
        bool skipdraw = false;
        private object window;
        private cnnvis.Graph reward_graph;

        public void draw_stats()
        {

        }
        public void draw_net()
        {

        }
        private void timer1_Tick(object sender, EventArgs _e)
        {
            tick();
        }

        private void pictureBox1_SizeChanged(object sender, EventArgs e)
        {
            RecreateGraphics();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            goslow();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            gofast();
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {

        }
    }
}
