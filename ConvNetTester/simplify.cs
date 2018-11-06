using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ConvNetLib;

namespace ConvNetTester
{
    public partial class simplify : Form
    {
        public simplify()
        {
            InitializeComponent();
            net = stuff.Test1();

            stuff.original_data();
            numericUpDown1.Maximum = net.layers.Count - 1;
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {

        }
        int ss = 50;

        public class MaxMinRet
        {
            public double maxi;
            public double maxv;
            public double mini;
            public double minv;
            public double dv;
        }
        public static MaxMinRet maxmin(double[] w)
        {
            if (w.Count() == 0)
            {
                return null;
            } // ... ;s

            var maxv = w[0];
            var minv = w[0];
            var maxi = 0;
            var mini = 0;
            for (var i = 1; i < w.Count(); i++)
            {
                if (w[i] > maxv)
                {
                    maxv = w[i];
                    maxi = i;
                }
                if (w[i] < minv)
                {
                    minv = w[i];
                    mini = i;
                }
            }
            return new MaxMinRet
            {
                maxi =
                maxi,
                maxv =
                maxv,
                mini =
                mini,
                minv =
                minv,
                dv =
                maxv - minv
            }
            ;
        }
        List<double> gridx = new List<double>();
        List<double> gridy = new List<double>();
        List<int> gridl = new List<int>();
        void draw()
        {
            //1.draw points
            Bitmap bmp = new Bitmap(pictureBox1.Width, pictureBox1.Height);
            Graphics gr = Graphics.FromImage(bmp);
            gr.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            gr.Clear(Color.White);

            gr.DrawLine(Pens.Black, bmp.Width / 2, 0, bmp.Width / 2, bmp.Height);
            gr.DrawLine(Pens.Black, 0, bmp.Height / 2, bmp.Width, bmp.Height / 2);

            #region draw areas

            int width = bmp.Width;
            int height = bmp.Height;

            int cx = 0, cy = 0;
            var netx = new Volume(1, 1, 2);
            gridx = new List<double>();
            gridy = new List<double>();
            gridl = new List<int>();
            for (var x = 0.0; x <= width; x += density, cx++)
            {
                cy = 0;
                for (var y = 0.0; y <= height; y += density, cy++)
                {
                    //var dec= svm.marginOne([(x-WIDTH/2)/ss, (y-HEIGHT/2)/ss]);
                    netx.w[0] = (x - width / 2.0) / ss;
                    netx.w[1] = (y - height / 2.0) / ss;

                    var a = net.Forward(netx, false);
                    //var a = netx;
                    Brush br = Brushes.Pink;
                    if (a.w[0] > a.w[1])
                    {
                        //br = 'rgb(250, 150, 150)';
                    }
                    else
                    {
                        br = Brushes.LightGreen;
                    }
                    gr.FillRectangle(br, new Rectangle((int)(x - density / 2 - 1), (int)(y - density / 2 - 1),

                        (int)(density + 2), (int)(density + 2)));

                    if (cx % gridstep == 0 && cy % gridstep == 0)
                    {
                        // record the transformation information
                        var xt = net.layers[lix].out_act.w[d0]; // in screen coords
                        var yt = net.layers[lix].out_act.w[d1]; // in screen coords
                        gridx.Add(xt);
                        gridy.Add(yt);
                        gridl.Add((a.w[0] > a.w[1]) ? 0 : 1); // remember final label as well
                    }
                }
            }

            #endregion

            for (int index = 0; index < stuff.data.Count; index++)
            {
                var doublese = stuff.data[index];
                var xx = (int)(doublese[0] * ss + bmp.Width / 2);
                var yy = (int)(doublese[1] * ss + bmp.Height / 2);

                gr.DrawEllipse(Pens.Black, xx - 5, yy - 5, 10, 10);
                gr.FillEllipse(stuff.labels[index] == 0 ? Brushes.DeepPink : Brushes.Green, xx - 5, yy - 5, 10, 10);
            }
            pictureBox1.Image = bmp;
        }
        private NetStuff stuff = new NetStuff();
        public Net net = new Net();
        
        private void timer1_Tick(object sender, EventArgs e)
        {
            //if (checkBox1.Checked)
            {
                stuff.Update(net,trainer);
            }
            draw();
            draw2();
        }
        Trainer trainer = new Trainer();
        private void button1_Click(object sender, EventArgs e)
        {
            stuff.spiral_data();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            stuff.random_data();

        }

        private void button3_Click(object sender, EventArgs e)
        {
            stuff.circle_data();

        }

        private void button4_Click(object sender, EventArgs e)
        {
            stuff.original_data();

        }

        private void pictureBox1_MouseClick(object sender, MouseEventArgs e)
        {
            var pc = pictureBox1.PointToClient(Cursor.Position);
            //var xx = (int)(doublese[0] * ss + bmp.Width / 2);
            //var yy = (int)(doublese[1] * ss + bmp.Height / 2);
            var xx = (pc.X - pictureBox1.Width / 2) / (double)ss;
            var yy = (pc.Y - pictureBox1.Height / 2) / (double)ss;

            if (e.Button == MouseButtons.Middle)
            {
                double closest = double.MaxValue;
                int ind = -1;
                for (int index = 0; index < stuff.data.Count; index++)
                {
                    var doublese = stuff.data[index];
                    var dist = Math.Pow(doublese[0] - xx, 2) + Math.Pow(doublese[1] - yy, 2);
                    if (dist < closest)
                    {
                        closest = dist;
                        ind = index;
                    }
                }
                stuff.data.RemoveAt(ind);
                stuff.labels.RemoveAt(ind);
            }
            else
            {
                stuff.data.Add(new double[] { xx, yy });
                stuff.labels.Add(e.Button == MouseButtons.Left ? 1 : 0);
            }
        }
        double density = 15.0;
        int gridstep = 2;
        private int lix = 0;
        private int d0 = 0;
        private int d1 = 1;
        public void draw2()
        {

            if (net.layers[lix] is TanhLayer || net.layers[lix] is FullConnLayer)
            {
                var netx = new Volume(1, 1, 2);

                var mmx = maxmin(gridx.ToArray());
                var mmy = maxmin(gridy.ToArray());

                Bitmap bmp = new Bitmap(pictureBox2.Width, pictureBox2.Height);
                var gr = Graphics.FromImage(bmp);
                gr.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                gr.Clear(Color.White);
                int visWIDTH = bmp.Width;
                int visHEIGHT = bmp.Height;


                #region draw grid
                var n = Math.Floor(Math.Sqrt(gridx.Count)); // size of grid. Should be fine?
                var ng = gridx.Count;
                var c = 0; // counter
                for (int x = 0; x < n; x++)
                {
                    for (int y = 0; y < n; y++)
                    {

                        // down
                        var ix1 = (int)(x * n + y);
                        var ix2 = (int)(x * n + y + 1);
                        double xraw1 = 0;
                        double yraw1 = 0;

                        if (ix1 >= 0 && ix2 >= 0 && ix1 < ng && ix2 < ng && y < n - 1)
                        { // check oob
                            var xraw = gridx[ix1];
                            xraw1 = visWIDTH * (gridx[ix1] - mmx.minv) / mmx.dv;
                            yraw1 = visHEIGHT * (gridy[ix1] - mmy.minv) / mmy.dv;
                            var xraw2 = visWIDTH * (gridx[ix2] - mmx.minv) / mmx.dv;
                            var yraw2 = visHEIGHT * (gridy[ix2] - mmy.minv) / mmy.dv;
                            gr.DrawLine(Pens.Black, (int)xraw1, (int)yraw1, (int)xraw2, (int)yraw2);

                        }

                        // and draw its color
                        Brush br = null;
                        if (gridl[ix1] == 0)
                        {
                            br = new SolidBrush(Color.FromArgb(250, 150, 150));
                            //visctx.fillStyle = 'rgb(250, 150, 150)';
                        }
                        else
                        {
                            br = new SolidBrush(Color.FromArgb(150, 250, 150));

                            //visctx.fillStyle = 'rgb(150, 250, 150)';
                        }
                        var sz = density * gridstep;
                        gr.FillRectangle(br, (int)(xraw1 - sz / 2 - 1), (int)(yraw1 - sz / 2 - 1), (int)(sz + 2), (int)(sz + 2));
                        //visctx.fillRect(xraw1 - sz / 2 - 1, yraw1 - sz / 2 - 1, sz + 2, sz + 2);

                        // right
                        ix1 = (int)((x + 1) * n + y);
                        ix2 = (int)(x * n + y);
                        if (ix1 >= 0 && ix2 >= 0 && ix1 < ng && ix2 < ng && x < n - 1)
                        { // check oob
                            var xraw = gridx[ix1];
                            xraw1 = visWIDTH * (gridx[ix1] - mmx.minv) / mmx.dv;
                            yraw1 = visHEIGHT * (gridy[ix1] - mmy.minv) / mmy.dv;
                            var xraw2 = visWIDTH * (gridx[ix2] - mmx.minv) / mmx.dv;
                            var yraw2 = visHEIGHT * (gridy[ix2] - mmy.minv) / mmy.dv;
                            gr.DrawLine(Pens.Black, (int)xraw1, (int)yraw1, (int)xraw2, (int)yraw2);


                        }

                    }
                }

                for (int x = 0; x < n; x++)
                {
                    for (int y = 0; y < n; y++)
                    {

                        // down
                        var ix1 = (int)(x * n + y);
                        var ix2 = (int)(x * n + y + 1);
                        double xraw1 = 0;
                        double yraw1 = 0;
                        if (ix1 >= 0 && ix2 >= 0 && ix1 < ng && ix2 < ng && y < n - 1)
                        { // check oob
                            var xraw = gridx[ix1];
                            xraw1 = visWIDTH * (gridx[ix1] - mmx.minv) / mmx.dv;
                            yraw1 = visHEIGHT * (gridy[ix1] - mmy.minv) / mmy.dv;
                            var xraw2 = visWIDTH * (gridx[ix2] - mmx.minv) / mmx.dv;
                            var yraw2 = visHEIGHT * (gridy[ix2] - mmy.minv) / mmy.dv;
                            gr.DrawLine(Pens.Black, (int)xraw1, (int)yraw1, (int)xraw2, (int)yraw2);

                        }

                        // and draw its color
                        Brush br = null;
                        if (gridl[ix1] == 0)
                        {
                            br = new SolidBrush(Color.FromArgb(250, 150, 150));
                            //visctx.fillStyle = 'rgb(250, 150, 150)';
                        }
                        else
                        {
                            br = new SolidBrush(Color.FromArgb(150, 250, 150));

                            //visctx.fillStyle = 'rgb(150, 250, 150)';
                        }
                        var sz = density * gridstep;
                        //   gr.FillRectangle(br, (int)(xraw1 - sz / 2 - 1), (int)(yraw1 - sz / 2 - 1), (int)(sz + 2), (int)(sz + 2));
                        //visctx.fillRect(xraw1 - sz / 2 - 1, yraw1 - sz / 2 - 1, sz + 2, sz + 2);

                        // right
                        ix1 = (int)((x + 1) * n + y);
                        ix2 = (int)(x * n + y);
                        if (ix1 >= 0 && ix2 >= 0 && ix1 < ng && ix2 < ng && x < n - 1)
                        { // check oob
                            var xraw = gridx[ix1];
                            xraw1 = visWIDTH * (gridx[ix1] - mmx.minv) / mmx.dv;
                            yraw1 = visHEIGHT * (gridy[ix1] - mmy.minv) / mmy.dv;
                            var xraw2 = visWIDTH * (gridx[ix2] - mmx.minv) / mmx.dv;
                            var yraw2 = visHEIGHT * (gridy[ix2] - mmy.minv) / mmy.dv;
                            gr.DrawLine(Pens.Black, (int)xraw1, (int)yraw1, (int)xraw2, (int)yraw2);


                        }

                    }
                }

                #endregion


                for (int i = 0; i < stuff.data.Count; i++)
                {
                    netx.w[0] = stuff.data[i][0];
                    netx.w[1] = stuff.data[i][1];
                    var a = net.Forward(netx, false);

                    var xt = visWIDTH * (net.layers[lix].out_act.w[d0] - mmx.minv) / mmx.dv; // in screen coords
                    var yt = visHEIGHT * (net.layers[lix].out_act.w[d1] - mmy.minv) / mmy.dv; // in screen coords
                    Brush br = Brushes.Green;
                    if (stuff.labels[i] == 1)
                    {
                        br = new SolidBrush(Color.FromArgb(100, 200, 100));
                    }
                    else
                    {
                        br = new SolidBrush(Color.FromArgb(200, 100, 100));
                    }
                    gr.DrawEllipse(Pens.Black, (int)xt, (int)yt, 10, 10);
                    gr.FillEllipse(br, (int)xt, (int)yt, 10, 10);
                }
                pictureBox2.Image = bmp;
            }
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            d0= 0;
            d1 = 1;
            lix = (int)numericUpDown1.Value;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            d0++;
            d1++;
            if (d0 > (net.layers[lix].out_depth - 1))
            {
                d0 = 0;
            }
            if (d1 > (net.layers[lix].out_depth - 1))
            {
                d1 = 0;
            }
        }
    }
}
