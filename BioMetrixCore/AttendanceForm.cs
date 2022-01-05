using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BioMetrixCore
{
    public partial class AttendanceForm : Form
    {
        private System.Windows.Forms.Timer tmr;

        public AttendanceForm(string staffId, string staffName, string message, string FormInterval)
        {
            Logger.LogWriter.WriteLog("Form is started loading");
            InitializeComponent();
            Logger.LogWriter.WriteLog("Form is loaded");
            this.lblStaffIdValue.Text = staffId.PadLeft(5, '0');
            this.lblTime.Text = message;
            Logger.LogWriter.WriteLog("Getting staff image");
            GetStaffImage(staffId);
            Logger.LogWriter.WriteLog("Got staff image");
            int formInterval = int.Parse(FormInterval);

            Logger.LogWriter.WriteLog("Starting timer");
            tmr = new System.Windows.Forms.Timer();
            tmr.Tick += delegate
            {
                Logger.LogWriter.WriteLog("Timer is closed");

                //this.pictureBox1.Dispose();
                this.Close();
                this.Dispose(true);
                //GC.Collect();
                //GC.WaitForPendingFinalizers();
                //GC.Collect();
                //GC.WaitForPendingFinalizers();
                tmr.Stop();
                tmr.Dispose();
            };
            tmr.Interval = (int)TimeSpan.FromSeconds(formInterval).TotalMilliseconds;
            tmr.Start();
            Logger.LogWriter.WriteLog("Timer is started");
            this.Show();
        }


        public void GetStaffImage(string staffId)
        {
            string picfilelocation = ConfigurationManager.AppSettings["picfilelocation"].ToString();
            string pngFile = picfilelocation + staffId + ".png";
            string jpgFile = picfilelocation + staffId + ".jpg";
            string jpegFile = picfilelocation + staffId + ".jpeg";
            string defaultFile = picfilelocation + "default.png";
            string FileName = "";

            if (File.Exists(pngFile))
                FileName = pngFile;
            else if (File.Exists(jpgFile))
                FileName = jpgFile;
            else if (File.Exists(jpegFile))
                FileName = jpegFile;
            else if (File.Exists(defaultFile))
                FileName = defaultFile;

            if (FileName.Length > 0)
            {
                Bitmap image = new Bitmap(FileName);
                pictureBox1.Image = image;
            }
        }

        private void AttendanceForm_Load(object sender, EventArgs e)
        {
            this.Shown += new EventHandler(Form1_Shown);
        }

        private void Form1_Shown(Object sender, EventArgs e)
        {
            Logger.LogWriter.WriteLog("Form is showing very late");
        }
    }
}
