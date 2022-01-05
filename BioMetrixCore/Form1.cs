using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BioMetrixCore
{
    public partial class Form1 : Form
    {
        private System.Windows.Forms.Timer tmr;
        public Form1()
        {
            InitializeComponent();

            tmr = new System.Windows.Forms.Timer();
            tmr.Tick += delegate
            {
                Logger.LogWriter.WriteLog("Timer is closed");

                this.Close();
                this.Dispose(true);
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                GC.WaitForPendingFinalizers();
                tmr.Stop();
                tmr.Dispose();
            };
            tmr.Interval = (int)TimeSpan.FromSeconds(2).TotalMilliseconds;
            tmr.Start();

        }
    }
}
