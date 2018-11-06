using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ConvNetTester
{
    public partial class mdi : Form
    {
        public mdi()
        {
            InitializeComponent();
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            simplify f = new simplify();
            f.MdiParent = this;
            f.Show();
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            mnist f = new mnist();
            f.MdiParent = this;
            f.Show();
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            painting f = new painting();
            f.MdiParent = this;
            f.Show();
        }

        private void mdi_Load(object sender, EventArgs e)
        {

        }

        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            fontRecognizer f = new fontRecognizer();
            f.MdiParent = this;
            f.Show();
        }

        private void toolStripButton5_Click(object sender, EventArgs e)
        {
            qlearn f = new qlearn();
            f.MdiParent = this;
            f.Show();
        }

        private void toolStripButton6_Click(object sender, EventArgs e)
        {
            Cifar10 f = new Cifar10();
            f.MdiParent = this;
            f.Show();
        }
    }
}
